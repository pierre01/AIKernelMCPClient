using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;

// ====== Configuration (simple, env-variable friendly) ======
string RestBaseUrl = Environment.GetEnvironmentVariable("REST_BASE_URL") ?? "https://localhost:7196";
bool UseBearer = (Environment.GetEnvironmentVariable("REST_USE_BEARER") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
string BearerToken = Environment.GetEnvironmentVariable("REST_BEARER_TOKEN") ?? "";
int TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("REST_TIMEOUT_SECONDS"), out var t) ? t : 60;
string OpenApiPath = Environment.GetEnvironmentVariable("OPENAPI_PATH") ?? "APIDesc.json";

// ====== Host/DI bootstrap ======
var builder = Host.CreateApplicationBuilder(args);

// Console logging to stderr (recommended for MCP)
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Information);

// HttpClient to reach Lights.RestApi
builder.Services.AddHttpClient("lights", client =>
{
    client.BaseAddress = new Uri(RestBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);
    if (UseBearer && !string.IsNullOrWhiteSpace(BearerToken))
    {
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken);
    }
});

// ====== Load & parse OpenAPI ======
OpenApiDocument openApiDoc;
await using (var stream = File.OpenRead(OpenApiPath))
{
    var reader = new OpenApiStreamReader();
    openApiDoc = reader.Read(stream, out var diag) ?? throw new InvalidOperationException("Failed to read OpenAPI document.");
    if (diag?.Errors?.Count > 0)
    {
        Console.Error.WriteLine("[OpenAPI] Diagnostics:");
        foreach (var e in diag.Errors) Console.Error.WriteLine(" - " + e);
    }
}

// Build tool registry from OpenAPI
var toolRegistry = OpenApiToolMapper.BuildTools(openApiDoc);

// ====== MCP Server wiring ======
IServiceProvider? rootProvider = null; // will be set after host.Build()

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation { Name = "Lights.McpServer", Version = "0.1.0" };
        options.Capabilities = new ServerCapabilities
        {
            Tools = new ToolsCapability
            {
                // Expose the tool list
                ListToolsHandler = (request, ct) =>
                {
                    var list = toolRegistry.Tools.Select(t => new Tool
                    {
                        Name = t.Name,
                        Description = t.Description,
                        InputSchema = JsonSerializer.Deserialize<JsonElement>(t.InputSchema.ToString())
                    }).ToList();

                    return ValueTask.FromResult(new ListToolsResult { Tools = list });
                },

                // Route tool calls to Lights.RestApi
                CallToolHandler = async (request, ct) =>
                {
                    if (request?.Params is null)
                        throw new McpException("Missing params.");

                    var toolName = request.Params.Name ?? throw new McpException("Missing tool name.");
                    var mapped = toolRegistry.Tools.FirstOrDefault(x => x.Name == toolName)
                                 ?? throw new McpException($"Unknown tool '{toolName}'.");

                    // Convert arguments (dictionary / JsonElement) → JObject safely
                    JObject input = new();
                    if (request.Params.Arguments is not null)
                    {
                        // Serialize back to JSON then parse to JObject for consistent handling
                        var json = JsonSerializer.Serialize(request.Params.Arguments);
                        input = JObject.Parse(json);
                    }

                    var http = rootProvider!.GetRequiredService<IHttpClientFactory>().CreateClient("lights");

                    // Build HTTP request
                    var (url, bodyContent) = OpenApiToolMapper.BindHttpRequest(
                        mapped.Route, mapped.Method, mapped, input);

                    using var msg = new HttpRequestMessage(new HttpMethod(mapped.Method), url);
                    if (bodyContent is not null)
                        msg.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");

                    var res = await http.SendAsync(msg, ct);
                    var text = await res.Content.ReadAsStringAsync(ct);

                    if (!res.IsSuccessStatusCode)
                    {
                        // Throwing McpException returns a proper MCP error to the client
                        throw new McpException($"REST call failed ({(int)res.StatusCode} {res.ReasonPhrase}): {text}");
                    }

                    // Try to pass JSON back as text block (client can parse if needed).
                    // You can also construct a JSON content block if your host expects it.
                    return new CallToolResult
                    {
                        Content = [new TextContentBlock { Type = "text", Text = text }]
                    };
                }
            }
        };
    })
    .WithStdioServerTransport(); // simplest transport to start

var host = builder.Build();
rootProvider = host.Services;

Console.Error.WriteLine("Lights.McpServer (STDIO) is ready. Waiting for a host to connect...");
await host.RunAsync();

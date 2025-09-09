using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

// ====== Configuration (simple, env-variable friendly) ======
string RestBaseUrl = Environment.GetEnvironmentVariable("REST_BASE_URL") ?? "http://localhost:5042";
bool UseBearer = (Environment.GetEnvironmentVariable("REST_USE_BEARER") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
string BearerToken = Environment.GetEnvironmentVariable("REST_BEARER_TOKEN") ?? "";
int TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("REST_TIMEOUT_SECONDS"), out var t) ? t : 60;
string OpenApiPath = Environment.GetEnvironmentVariable("OPENAPI_PATH") ?? "APIDesc.json";

if (!Debugger.IsAttached)
{
    Debugger.Launch();
}
Debug.WriteLine(">> Starting Lights.McpServer");

static JObject ToJObject(IReadOnlyDictionary<string, JsonElement> dict)
{
    var obj = new JObject();
    foreach (var kv in dict)
    {
        var el = kv.Value;
        obj[kv.Key] = el.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? JValue.CreateNull()
            : JToken.Parse(el.GetRawText());
    }
    return obj;
}



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
                // === List tools
                ListToolsHandler = (request, ct) =>
                {
                    var tools = toolRegistry.Tools.Select(t => new Tool
                    {
                        Name = t.Name,
                        Description = t.Description,
                        InputSchema = JsonDocument.Parse(t.InputSchema.ToString()).RootElement.Clone()
                    }).ToList();

                    return ValueTask.FromResult(new ListToolsResult { Tools = tools });
                },


                // === Call tool
                CallToolHandler = async (request, ct) =>
                {
                    if (request?.Params is null) throw new McpException("Missing params.");
                    var toolName = request.Params.Name ?? throw new McpException("Missing tool name.");

                    var mapped = toolRegistry.Tools.FirstOrDefault(x => x.Name == toolName)
                                 ?? throw new McpException($"Unknown tool '{toolName}'.");

                    // request.Params.Arguments is IReadOnlyDictionary<string, JsonElement> in your SDK
                    var argsDict = request.Params.Arguments
                                   ?? throw new McpException("Missing tool arguments.");

                    // Convert to JObject (preserve exact JSON per key)
                    var input = ToJObject(argsDict);

                    var http = rootProvider!.GetRequiredService<IHttpClientFactory>().CreateClient("lights");
                    var (url, bodyContent) = OpenApiToolMapper.BindHttpRequest(mapped.Route, mapped.Method, mapped, input);

                    // Compose the absolute URI for debugging
                    Uri? absolute;
                    Uri.TryCreate(http.BaseAddress, url, out absolute);
                    var finalUri = absolute?.ToString() ?? (http.BaseAddress?.ToString() ?? "") + url;

                    // Build the request
                    using var msg = new HttpRequestMessage(new HttpMethod(mapped.Method), url);
                    if (bodyContent is not null)
                        msg.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");

                    // DEBUG LOGS
                    Console.Error.WriteLine($"[Lights.McpServer] {mapped.Method} {finalUri}");
                    if (bodyContent is not null) Console.Error.WriteLine($"[Lights.McpServer] Body: {bodyContent}");

                    HttpResponseMessage res;
                    try
                    {
                        res = await http.SendAsync(msg, ct);
                    }
                    catch (HttpRequestException ex)
                    {
                        // Network / TLS / DNS issues show up here
                        throw new McpException($"Network error calling {finalUri}: {ex.Message}");
                    }

                    var text = await res.Content.ReadAsStringAsync(ct);

                    if (!res.IsSuccessStatusCode)
                    {
                        throw new McpException(
                            $"REST call failed {finalUri} -> {(int)res.StatusCode} {res.ReasonPhrase}\n{text}");
                    }
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

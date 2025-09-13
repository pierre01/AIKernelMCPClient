using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

// ====== Configuration (simple, env-variable friendly) ======
string RestBaseUrl = Environment.GetEnvironmentVariable("REST_BASE_URL") ?? "https://localhost:5042";
bool UseBearer = (Environment.GetEnvironmentVariable("REST_USE_BEARER") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
string BearerToken = Environment.GetEnvironmentVariable("REST_BEARER_TOKEN") ?? "";
int TimeoutSeconds = int.TryParse(Environment.GetEnvironmentVariable("REST_TIMEOUT_SECONDS"), out var t) ? t : 60;
string OpenApiPath = Environment.GetEnvironmentVariable("OPENAPI_PATH") ?? "APIDesc.json";

if (!Debugger.IsAttached)
{
    Debugger.Launch();
}
Debug.WriteLine(">> Starting Lights.McpServer");

// Single shared HttpClient with optional dev-cert bypass in DEBUG
HttpClient Http = CreateHttpClient();

HttpClient CreateHttpClient()
{
#if DEBUG
    var handler = new HttpClientHandler
    {
        // Dev only: accept self-signed certs so HTTPS localhost works
        ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true
    };
#else
    var handler = new HttpClientHandler();
#endif

    var client = new HttpClient(handler)
    {
        BaseAddress = new Uri(RestBaseUrl),
        Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
    };

    if (UseBearer && !string.IsNullOrWhiteSpace(BearerToken))
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);

    return client;
}

static JObject ToJObject(IReadOnlyDictionary<string, JsonElement> dict)
{
    var obj = new JObject();
    foreach (var (key, el) in dict)
    {
        obj[key] = el.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
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


                CallToolHandler = async (request, ct) =>
                {
                    if (request?.Params is null) throw new McpException("Missing params.");
                    var toolName = request.Params.Name ?? throw new McpException("Missing tool name.");

                    var mapped = toolRegistry.Tools.FirstOrDefault(x => x.Name == toolName)
                                 ?? throw new McpException($"Unknown tool '{toolName}'.");

                    // In your SDK, Arguments is IReadOnlyDictionary<string, JsonElement>
                    var argsDict = request.Params.Arguments
                                  ?? throw new McpException("Missing tool arguments.");

                    // Convert args → JObject for the OpenAPI mapper
                    var input = ToJObject(argsDict);

                    // Build relative URL + optional JSON body
                    var (relativeUrl, bodyJson) = OpenApiToolMapper.BindHttpRequest(mapped.Route, mapped.Method, mapped, input);

                    // Compose absolute URL
                    var finalUri = new Uri(Http.BaseAddress!, relativeUrl);

                    using var msg = new HttpRequestMessage(new HttpMethod(mapped.Method), finalUri);
                    if (bodyJson is not null &&
                        (mapped.Method == "POST" || mapped.Method == "PUT" || mapped.Method == "PATCH"))
                    {
                        msg.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    }

                    // Debug tracing
                    Console.Error.WriteLine($"[MCP→REST] {mapped.Method} {finalUri}");
                    if (bodyJson is not null) Console.Error.WriteLine($"[MCP→REST] Body: {bodyJson}");

                    HttpResponseMessage res;
                    try
                    {
                        switch(mapped.Method)
                        {
                            case "PATCH":
                                res = await Http.PatchAsync(finalUri, msg.Content);
                                break;
                            case "GET":
                                default:
                                res = await Http.GetAsync(finalUri);
                                break;
                        }
                        
                    }
                    catch (HttpRequestException ex)
                    {
                        throw new McpException($"Network error calling {finalUri}: {ex.Message}");
                    }

                    var text = await res.Content.ReadAsStringAsync(ct);
                    if (!res.IsSuccessStatusCode)
                    {
                        throw new McpException($"REST call failed {finalUri} -> {(int)res.StatusCode} {res.ReasonPhrase}\n{text}");
                    }

                    // Return raw text (JSON or not). You can switch to JSON content blocks if you prefer.
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

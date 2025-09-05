// File: LightsMcpServer/Program.cs
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Net.Http;

class Program
{
    // Quick-n-dirty JSON-RPC over stdio for MCP
    static async Task Main(string[] args)
    {
        // Args: --spec APIDesc.json --base https://localhost:5042
        var specPath = GetArg(args, "--spec") ?? "APIDesc.json";
        var baseUrl = GetArg(args, "--base") ?? "https://localhost:5042";

        var openApi = OpenApiToMcp.Load(specPath, baseUrl);
        var toolIndex = openApi.Tools.ToDictionary(t => t.Name, t => t);
        var http = new HttpClient { BaseAddress = new Uri(openApi.ServerUrl ?? baseUrl) };

        // Advertise capabilities once (MCP handshake-lite)
        WriteJson(new
        {
            jsonrpc = "2.0",
            method = "notifications/ready",
            @params = new { serverInfo = new { name = "lights-mcp", version = openApi.Version } }
        });

        // Simple read loop
        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var msg = JsonNode.Parse(line)!.AsObject();
            var id = msg["id"]?.ToString();

            try
            {
                var method = msg["method"]?.ToString();
                if (method == "tools/list")
                {
                    var items = openApi.Tools.Select(t => new {
                        name = t.Name,
                        description = t.Description,
                        inputSchema = t.ParametersSchema
                    });
                    WriteResult(id, new { tools = items });
                }
                else if (method == "tools/call")
                {
                    var p = msg["params"]!.AsObject();
                    var name = p["name"]!.ToString();
                    var argsObj = p["arguments"] as JsonObject ?? new JsonObject();
                    if (!toolIndex.TryGetValue(name, out var tool))
                        throw new InvalidOperationException($"Unknown tool: {name}");

                    var (url, body, q) = BuildHttpRequest(tool, argsObj);
                    using var req = new HttpRequestMessage(new HttpMethod(tool.HttpMethod), url);

                    if (body is not null)
                    {
                        req.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
                    }

                    var resp = await http.SendAsync(req);
                    var text = await resp.Content.ReadAsStringAsync();
                    resp.EnsureSuccessStatusCode();

                    // Return JSON if possible; otherwise wrap as string
                    JsonNode? payload;
                    try { payload = JsonNode.Parse(text); }
                    catch { payload = new JsonObject { ["text"] = text }; }

                    WriteResult(id, new { content = payload });
                }
                else
                {
                    // Unknown → no-op
                    WriteError(id, code: -32601, message: "Method not found");
                }
            }
            catch (Exception ex)
            {
                WriteError(id, code: -32000, message: ex.Message);
            }
        }
    }

    static (string url, JsonObject? body, Dictionary<string, string> query) BuildHttpRequest(McpToolDef tool, JsonObject args)
    {
        // 1) path params {x}
        var url = tool.PathTemplate;
        var pathParamNames = Regex.Matches(url, "{([^}]+)}").Select(m => m.Groups[1].Value).ToList();
        foreach (var p in pathParamNames)
        {
            if (!args.TryGetPropertyValue(p, out var val) || val is null)
                throw new ArgumentException($"Missing path parameter: {p}");
            url = url.Replace("{" + p + "}", Uri.EscapeDataString(val.ToString()!));
            args.Remove(p);
        }

        // 2) body (if provided as 'body')
        JsonObject? body = null;
        if (args.TryGetPropertyValue("body", out var bodyNode) && bodyNode is JsonObject bObj)
        {
            body = bObj;
            args.Remove("body");
        }

        // 3) remaining → query
        var query = new Dictionary<string, string>();
        foreach (var kv in args)
        {
            if (kv.Value is null) continue;
            query[kv.Key] = kv.Value.ToJsonString();
        }
        if (query.Count > 0)
        {
            var q = string.Join("&", query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(TrimJsonQuotes(kv.Value))}"));
            url = url.Contains("?") ? $"{url}&{q}" : $"{url}?{q}";
        }
        return (url, body, query);
    }

    static string TrimJsonQuotes(string s) => s.Trim().Trim('"');

    static string? GetArg(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    static void WriteResult(string? id, object result)
    {
        WriteJson(new { jsonrpc = "2.0", id, result });
    }

    static void WriteError(string? id, int code, string message)
    {
        WriteJson(new { jsonrpc = "2.0", id, error = new { code, message } });
    }

    static void WriteJson(object o)
    {
        var json = JsonSerializer.Serialize(o, new JsonSerializerOptions { WriteIndented = false });
        Console.Out.WriteLine(json);
        Console.Out.Flush();
    }
}

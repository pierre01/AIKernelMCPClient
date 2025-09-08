using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AIKernelClient.Services;

public sealed class McpClient : IAsyncDisposable
{
    readonly Process _p;
    readonly StreamReader _stdout;
    readonly StreamWriter _stdin;
    int _id;

    McpClient(Process p, StreamReader stdout, StreamWriter stdin) { _p = p; _stdout = stdout; _stdin = stdin; }

    public static async Task<McpClient> StartAsync(string exe, string args)
    {
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        p.Start();
        return new McpClient(p, p.StandardOutput, p.StandardInput);
    }

    public async Task<JsonObject> RpcAsync(string method, JsonObject @params = null)
    {
        var id = Interlocked.Increment(ref _id);
        var req = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method
        };
        if (@params is not null) req["params"] = @params;

        await _stdin.WriteLineAsync(req.ToJsonString());
        await _stdin.FlushAsync();

        // Simple line-by-line await (server writes 1 line per message)
        while (true)
        {
            var line = await _stdout.ReadLineAsync();
            if (line is null) throw new IOException("MCP server closed.");
            var msg = JsonNode.Parse(line)!.AsObject();

            if (msg["id"]?.GetValue<int>() == id)
            {
                if (msg["error"] is not null)
                {
                    var err = msg["error"]!.AsObject();
                    throw new InvalidOperationException(err["message"]?.ToString() ?? "MCP error");
                }
                return (JsonObject)msg["result"]!;
            }
            // else ignore notifications/other ids
        }
    }

    public Task<JsonObject> ToolsListAsync() => RpcAsync("tools/list");
    public Task<JsonObject> ToolsCallAsync(string name, JsonObject args)
        => RpcAsync("tools/call", new JsonObject { ["name"] = name, ["arguments"] = args });

    public async ValueTask DisposeAsync()
    {
        try { _p.Kill(entireProcessTree: true); } catch { }
        await Task.CompletedTask;
    }
}

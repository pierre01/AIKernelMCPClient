using Lights.MauiClient.Services.Interfaces;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using OpenAI;
using System.ClientModel;
using System.Diagnostics;
using System.Net.Security;

namespace Lights.MauiClient.Services;

/// <summary>
/// Provides the application's conversational agent and its MCP-backed light tools.
/// The legacy class name is retained so the UI-facing service contract does not change.
/// </summary>
public sealed class MicrosoftAgentsService : IMicrosoftAgentsService, IAsyncDisposable
{
    private const string RemoteModel = "gpt-5-mini";
    private const string LocalModel = "qwen/qwen3.6-35b-a3b";
    private const int MaxOutputTokens = 4096;

    private static readonly string McpMode = Environment.GetEnvironmentVariable("MCP_MODE") ?? "HTTP";
    private static readonly string McpExe = Environment.GetEnvironmentVariable("MCP_EXE")
        ?? @"G:\Dev\AI\AIKernelClient\Lights.McpServer\bin\Debug\net9.0\Lights.McpServer.exe";
    private static readonly string McpHttpUrl = Environment.GetEnvironmentVariable("MCP_HTTP_URL")
        ?? "https://localhost:5042/mcp";

    private AIAgent _agent;
    private AgentSession _session;
    private McpClient _mcpClient;
    private long _totalTokens;

    public async Task InitializeAgentAndToolsAsync()
    {
        try
        {
            _mcpClient = await CreateMcpClientAsync();
            var mcpTools = await _mcpClient.ListToolsAsync();

            var useLocal = true;
            var model = useLocal ? LocalModel : RemoteModel;
            var apiKey = useLocal ? "local-key" : await ApiKeyProvider.GetApiKeyAsync();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("API key is not set.");
            }

            var clientOptions = new OpenAIClientOptions();
            if (useLocal)
            {
                clientOptions.Endpoint = new Uri("http://127.0.0.1:8931/v1");
            }
            else
            {
                clientOptions.OrganizationId = await ApiKeyProvider.GetAiOrgId();
            }

            var chatClient = new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions)
                .GetChatClient(model);

            _agent = chatClient.AsIChatClient().AsAIAgent(
                name: "LightsAgent",
                instructions: "You are Lights' local copilot. Help the user control their lights and environment. Use the available MCP tools when they are helpful. /no_think",
                tools: [.. mcpTools.Cast<AITool>()]);

            _session = await _agent.CreateSessionAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing agent: {ex.Message}");
            await DisposeMcpClientAsync();
            throw;
        }
    }

    public async Task<AgentResponseResult> GetResponseAsync(string prompt)
    {
        var response = new AgentResponseResult();

        try
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                response.IsSuccess = false;
                response.Result = "Please enter a prompt";
                return response;
            }

            if (_agent is null || _session is null)
            {
                response.IsSuccess = false;
                response.Result = "Agent is not initialized.";
                return response;
            }

            var options = new ChatClientAgentRunOptions(new ChatOptions
            {
                Temperature = 1,
                MaxOutputTokens = MaxOutputTokens,
            });

            var stopwatch = Stopwatch.StartNew();
            var result = await _agent.RunAsync(prompt, _session, options);
            stopwatch.Stop();

            response.Result = result.Text;
            response.GenerationMilliseconds = stopwatch.ElapsedMilliseconds;

            if (result.Usage is { } usage)
            {
                var inputTokens = usage.InputTokenCount ?? 0;
                var outputTokens = usage.OutputTokenCount ?? 0;
                var requestTokens = usage.TotalTokenCount ?? inputTokens + outputTokens;

                _totalTokens += requestTokens;
                response.InputTokens = ToMetricInt(inputTokens);
                response.OutputTokens = ToMetricInt(outputTokens);
                response.RequestTokens = ToMetricInt(requestTokens);
                response.TotalTokens = ToMetricInt(_totalTokens);

                if (outputTokens > 0 && stopwatch.ElapsedMilliseconds > 0)
                {
                    response.PipelineTokensPerSecond =
                        (inputTokens + outputTokens) / (stopwatch.ElapsedMilliseconds / 1000.0);
                }
            }

            response.IsSuccess = true;
        }
        catch (Exception ex)
        {
            response.Result = $"Error getting response: {ex.Message}";
            Debug.WriteLine($"Error getting response: {ex}");
            response.IsSuccess = false;
        }

        return response;
    }

    private static async Task<McpClient> CreateMcpClientAsync()
    {
        if (!string.Equals(McpMode, "HTTP", StringComparison.OrdinalIgnoreCase))
        {
            return await McpClient.CreateAsync(new StdioClientTransport(new()
            {
                Name = "Lights.McpServer",
                Command = McpExe,
                Arguments = [],
            }));
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, errors) =>
                certificate?.Subject?.Contains("CN=localhost", StringComparison.OrdinalIgnoreCase) == true
                || errors == SslPolicyErrors.None,
        };

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Name = "Lights.McpServer",
                Endpoint = new Uri(McpHttpUrl),
                TransportMode = HttpTransportMode.StreamableHttp,
            },
            new HttpClient(handler),
            ownsHttpClient: true);

        return await McpClient.CreateAsync(transport);
    }

    private static int ToMetricInt(long value) =>
        (int)Math.Clamp(value, 0, int.MaxValue);

    public async ValueTask DisposeAsync()
    {
        await DisposeMcpClientAsync();
        GC.SuppressFinalize(this);
    }

    private async ValueTask DisposeMcpClientAsync()
    {
        if (_mcpClient is not null)
        {
            await _mcpClient.DisposeAsync();
            _mcpClient = null;
        }
    }
}

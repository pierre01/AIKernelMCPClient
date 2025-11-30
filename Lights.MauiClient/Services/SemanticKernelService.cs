using Lights.MauiClient.Services.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.SemanticKernel.Extensions;
using System.Diagnostics;
using System.Net.Security;

namespace Lights.MauiClient.Services;

public class SemanticKernelService : ISemanticKernelService
{
    // ===== OpenAI chat model config =====
    private const string chatModel = "gpt-5-nano";

    // ===== MCP transport config (override via env vars) =====
    // MCP_MODE: "STDIO" (default) or "WS"
    // MCP_EXE:  full path to Lights.McpServer.exe (when STDIO)
    // MCP_WS_URL: ws://localhost:5059/mcp (when WS)
    private static readonly string McpMode = Environment.GetEnvironmentVariable("MCP_MODE") ?? "SSE"; // SSE Or STDIO
    private static readonly string McpExe = Environment.GetEnvironmentVariable("MCP_EXE")
                                            ?? @"G:\Dev\AI\AIKernelClient\Lights.McpServer\bin\Debug\net9.0\Lights.McpServer.exe";
    private static readonly string McpWsUrl = Environment.GetEnvironmentVariable("MCP_WS_URL") ?? "https://localhost:3001/mcp/";  //"ws://localhost:3001/mcp/"

    private ChatHistory _history;
    private IKernelBuilder _builder;
    private Kernel _kernel;
    private IChatCompletionService _chatCompletionService;
    private OpenAIPromptExecutionSettings _openAIPromptExecutionSettings;

    private IChatHistoryReducer _reducer;

    /// <summary>
    /// Initialize SK, OpenAI, and attach MCP tools from Lights.McpServer.
    /// </summary>
    public async Task InitializeKernelAndPluginAsync()
    {
        try
        {        
            _history = [];

            _reducer = new ChatHistoryTruncationReducer(targetCount: 4, thresholdCount: 6);

            // If you keep cloud as an option, set useLocal = true/false to toggle
            var useLocal = true;
_builder = Kernel.CreateBuilder();

            string serviceID = "LocalGPT";
            if (!useLocal)
            {
                serviceID = "RemoteGPT"; //Use OpenAI API
                var openAiApiKey = await ApiKeyProvider.GetApiKeyAsync();
                var openApiOrgId = await ApiKeyProvider.GetAiOrgId();
                if (string.IsNullOrWhiteSpace(openAiApiKey))
                    throw new InvalidOperationException("API key is not set.");

                _builder.AddOpenAIChatCompletion(
                    apiKey: openAiApiKey,
                    modelId: chatModel,
                    orgId: openApiOrgId,
                    serviceId: serviceID
                );
            }
            else
            {
                serviceID = "LocalGPT";
                // Build a handler that skips CRL/OCSP (revocation) for localhost only.
                var handler = new HttpClientHandler
                {
                    CheckCertificateRevocationList = false,
                    ServerCertificateCustomValidationCallback = (req, cert, chain, errors) =>
                    {
                        // Allow only our localhost certs; still fail anything else
                        if (cert?.Subject?.Contains("CN=localhost", StringComparison.OrdinalIgnoreCase) == true)
                            return true;

                        return errors == SslPolicyErrors.None;
                    }
                };

                var httpsClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri("http://127.0.0.1:8931/v1")
                };

                // Register the local vLLM endpoint with Semantic Kernel
                _builder.AddOpenAIChatCompletion(
                    apiKey: "local-key",
                    modelId: "openai/gpt-oss-20b",            // must match --served-model-name
                    orgId: null,
                    serviceId: serviceID,
                    httpClient: httpsClient
                );


            }

            // Let the model auto-invoke MCP tools when helpful
            _openAIPromptExecutionSettings = new()
            {
                Temperature = 1,
                // This is the key line – lets the model pick functions
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,

                MaxTokens = 4096
            };

            _kernel = _builder.Build();


            // ===== Attach MCP tools (choose transport by env) =====
            if (string.Equals(McpMode, "SSE", StringComparison.OrdinalIgnoreCase))
            {
                // Default: start the MCP server locally via SSE and bind its tools
                // Connect to a running http  server 
                await _kernel.Plugins.AddMcpFunctionsFromSseServerAsync(
                    serverName: "Lights.McpServer",
                     endpoint: McpWsUrl);
            }
            else
            {
                // Not Supported anymore I switched to SSE by default
                var p = await _kernel.Plugins.AddMcpFunctionsFromStdioServerAsync(
                    serverName: "Lights.McpServer",
                    command: McpExe,
                    arguments: Array.Empty<string>());
            }

            // Optional: inspect/trace tool invocations
            _kernel.FunctionInvocationFilters.Add(new FunctionInvocationFilter());



            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();


        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing kernel: {ex.Message}");
            throw;
        }
    }

    private int _lastTotalTokens = 0;
    private int _totalTokens = 0;

    /// <summary>
    /// Chat with tool use (MCP functions auto-invoked when needed).
    /// </summary>
    public async Task<KernelPluginResult> GetResponseAsync(string prompt)
    {
        var response = new KernelPluginResult();
        try
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                response.IsSuccess = false;
                response.Result = "Please enter a prompt";
                return response;
            }

            if (_history == null)
            {
                response.IsSuccess = false;
                response.Result = "Chat history is not initialized.";
                return response;
            }

            _history.AddUserMessage(prompt);

            // If you want trimming, uncomment to apply reducer:
            // var reduced = await _reducer.ReduceAsync(_history);
            // if (reduced is not null) _history = new ChatHistory(reduced);

            if (_chatCompletionService is null)
            {
                response.IsSuccess = false;
                response.Result = "ChatCompletionService is not initialized.";
                return response;
            }

            var stopwatch = Stopwatch.StartNew();

            ChatMessageContent result = await _chatCompletionService.GetChatMessageContentAsync(
                _history,
                executionSettings: _openAIPromptExecutionSettings,
                kernel: _kernel);

            stopwatch.Stop();

            var toolTimeMs = FunctionInvocationFilter.ConsumeToolTimeMs();
            var llmTimeMs = stopwatch.ElapsedMilliseconds - toolTimeMs;
            if (llmTimeMs < 1) llmTimeMs = stopwatch.ElapsedMilliseconds; // fallback


            response.Result = result.ToString();

            // Token accounting (OpenAI connector metadata)
            // Token accounting
            if (result.Metadata != null &&
                result.Metadata.TryGetValue("Usage", out var usageObj) &&
                usageObj is OpenAI.Chat.ChatTokenUsage usage)
            {
                var totalTokens = usage.TotalTokenCount;
                var inputTokens = usage.InputTokenCount - _lastTotalTokens;
                _lastTotalTokens = usage.InputTokenCount;
                var outputTokens = usage.OutputTokenCount;

                _totalTokens += totalTokens;

                response.InputTokens = inputTokens;
                response.OutputTokens = outputTokens;
                response.TotalTokens = _totalTokens;
                response.RequestTokens = totalTokens;

                // ===== Tokens per Second =====
                response.GenerationMilliseconds = llmTimeMs;
                if (outputTokens > 0 && llmTimeMs > 0)
                {
                    response.PipelineTokensPerSecond =
                        (outputTokens + inputTokens) / (llmTimeMs / 1000.0);
                }
            }

            response.IsSuccess = true;
        }
        catch (Exception ex)
        {
            response.Result = $"Error getting response: {ex.Message}";
            Debug.WriteLine($"Error getting response: {ex.Message}");
            response.IsSuccess = false;
        }
        return response;
    }
}

/// <summary>
/// Optional function-invocation tracer
/// </summary>
public sealed class FunctionInvocationFilter : IFunctionInvocationFilter
{
    // Accumulates tool time per async flow
    private static readonly AsyncLocal<long> _toolTimeMs = new();

    // Helper so your service can access and reset it
    public static long ConsumeToolTimeMs()
    {
        var value = _toolTimeMs.Value;
        _toolTimeMs.Value = 0;
        return value;
    }

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            Debug.WriteLine($"Function {context.Function.Name} is about to be invoked.");
            await next(context);
            Debug.WriteLine($"Function {context.Function.Name} was invoked.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exception during function invocation: {ex}");
        }
        finally
        {
            sw.Stop();
            _toolTimeMs.Value = _toolTimeMs.Value + sw.ElapsedMilliseconds;
        }
    }
}


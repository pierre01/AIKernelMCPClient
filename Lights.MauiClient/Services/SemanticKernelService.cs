using AIKernelClient.Services.Interfaces;
using LightsAPICommon;
using LightsAPICommon.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.SemanticKernel.Extensions;
using System.Diagnostics;
using System.Net.Http.Json;

namespace AIKernelClient.Services;

public class SemanticKernelService : ISemanticKernelService
{
    // ===== OpenAI chat model config =====
    private const string chatModel = "gpt-5-mini";
    private const string openApiOrgId = "org-RRBnXYYjTq5b4qr7TLaaHsLD";

    // ===== MCP transport config (override via env vars) =====
    // MCP_MODE: "STDIO" (default) or "WS"
    // MCP_EXE:  full path to Lights.McpServer.exe (when STDIO)
    // MCP_WS_URL: ws://localhost:5059/mcp (when WS)
    private static readonly string McpMode = Environment.GetEnvironmentVariable("MCP_MODE") ?? "SSE"; // SSE Or STDIO
    private static readonly string McpExe = Environment.GetEnvironmentVariable("MCP_EXE")
                                            ?? @"G:\Dev\AI\AIKernelClient\Lights.McpServer\bin\Debug\net9.0\Lights.McpServer.exe";
    private static readonly string McpWsUrl = Environment.GetEnvironmentVariable("MCP_WS_URL") ?? "ws://localhost:5059/mcp";

    private ChatHistory _history;
    private IKernelBuilder _builder;
    private Kernel _kernel;
    private IChatCompletionService _chatCompletionService;
    private OpenAIPromptExecutionSettings _openAIPromptExecutionSettings;

#pragma warning disable SKEXP0001
    private IChatHistoryReducer _reducer;
#pragma warning restore SKEXP0001

    /// <summary>
    /// Initialize SK, OpenAI, and attach MCP tools from Lights.McpServer.
    /// </summary>
    public async Task InitializeKernelAndPluginAsync()
    {
        try
        {        
            _history = [];

#pragma warning disable SKEXP0001
            _reducer = new ChatHistoryTruncationReducer(targetCount: 4, thresholdCount: 6);
#pragma warning restore SKEXP0001

            var openAiApiKey = await ApiKeyProvider.GetApiKeyAsync();
            if (string.IsNullOrWhiteSpace(openAiApiKey))
                throw new InvalidOperationException("API key is not set.");

            _builder = Kernel.CreateBuilder();

            // OpenAI chat connector
            _builder.Services.AddOpenAIChatCompletion(
                modelId: chatModel,
                apiKey: openAiApiKey,
                orgId: openApiOrgId,
                serviceId: "lights"
            );



            // Let the model auto-invoke MCP tools when helpful
#pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _openAIPromptExecutionSettings = new()
            {
                //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true, AllowStrictSchemaAdherence =true }),
                Temperature = 1,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
            };
#pragma warning restore SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            _kernel = _builder.Build();


            // ===== Attach MCP tools (choose transport by env) =====
            if (string.Equals(McpMode, "SSE", StringComparison.OrdinalIgnoreCase))
            {
                // Connect to a running http  server 
                await _kernel.Plugins.AddMcpFunctionsFromSseServerAsync(
                    serverName: "Lights.McpServer",
                   // httpClient: null,
                    endpoint: "https://localhost:3001/");
            }
            else
            {
                // Default: start the MCP server locally via STDIO and bind its tools
                // No arguments needed for our Program.cs (it just speaks MCP over stdio)
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

            _history.AddUserMessage(prompt);

            // If you want trimming, uncomment to apply reducer:
            // var reduced = await _reducer.ReduceAsync(_history);
            // if (reduced is not null) _history = new ChatHistory(reduced);

            ChatMessageContent result = await _chatCompletionService.GetChatMessageContentAsync(
                _history,
                executionSettings: _openAIPromptExecutionSettings,
                kernel: _kernel);

            response.Result = result.ToString();

            // Token accounting (OpenAI connector metadata)
            if (result.Metadata.ContainsKey("Usage"))
            {
                var usage = (OpenAI.Chat.ChatTokenUsage)result.Metadata["Usage"];
                var totalTokens = usage.TotalTokenCount;
                var inputTokens = usage.InputTokenCount - _lastTotalTokens;
                _lastTotalTokens = usage.InputTokenCount;
                var outputTokens = usage.OutputTokenCount;

                _totalTokens += totalTokens;

                response.InputTokens = inputTokens;
                response.OutputTokens = outputTokens;
                response.TotalTokens = _totalTokens;
                response.RequestTokens = totalTokens;
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
    public async Task OnFunctionInvocationAsync(Microsoft.SemanticKernel.FunctionInvocationContext context, Func<Microsoft.SemanticKernel.FunctionInvocationContext, Task> next)
    {
        try
        {
            Debug.WriteLine($"Function {context.Function.Name} is about to be invoked.");
            await next(context);
            Debug.WriteLine($"Function {context.Function.Name} was invoked.");
        }
        catch (Exception ex)
        {

            throw;
        }

    }


}

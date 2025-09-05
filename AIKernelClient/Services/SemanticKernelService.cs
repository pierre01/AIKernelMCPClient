using AIKernelClient.Services.Interfaces;
using LightsAPICommon;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Diagnostics;

namespace AIKernelClient.Services;

public class SemanticKernelService:ISemanticKernelService
{
    // Set upcredentials and endpoints.
    private const string chatModel = "gpt-5-mini"; // or gpt-4 (but it is expensive) gpt-5-mini gpt-5-nano gpt-4o-mini
    private const string openApiOrgId = "org-RRBnXYYjTq5b4qr7TLaaHsLD";

    //private const string apiLocation = "rh8xzzh8-5042.usw3.devtunnels.ms";
    private const string apiLocation = "localhost:5042";

    private ChatHistory _history;
    private IKernelBuilder _builder;
    private Kernel _kernel;
    private IChatCompletionService _chatCompletionService;
    private OpenAIPromptExecutionSettings _openAIPromptExecutionSettings;

#pragma warning disable SKEXP0001 // Suppress the warning for evaluation purposes
    private IChatHistoryReducer _reducer;
#pragma warning restore SKEXP0001

    private readonly string[] _userPrompts = House.CustomPrompts;

    /// <summary>
    /// Initializes the kernel and sets up the OpenAI chat connector for controlling smart lights.
    /// </summary>
    /// <returns>Returns a Task representing the asynchronous operation.</returns>
    public async Task InitializeKernelAndPluginAsync()
    {
        try
        {   // Initialize the ChatHistory object.
            _history = [];
            //TODO: Add Sytem prompts to keep the conversation context
            // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/chat-history?pivots=programming-language-csharp
            
            string openAiApiKey = await ApiKeyProvider.GetApiKeyAsync();

            // Safety check
            if (string.IsNullOrEmpty(openAiApiKey))
            {
                throw new InvalidOperationException("API key is not set in environment variables.");
            }


            // this does not seem to help much :

            //_history.AddSystemMessage(@"
            //You are an AI assistant responsible for controlling smart lights.
            //To handle light control requests efficiently, follow this process:

            //1. **Initial API Calls (if data is not cached)**:
            //   - Retrieve all **rooms** using ('GetRooms').
            //   - Retrieve all **lights** using ('Getlights').
            //   - Store this data for quick access.

            //2. **Handling User Requests**:
            //   - If the user asks about a specific room, check stored room data before making API calls.
            //   - If the user asks about a specific light, check stored light data before making API calls.
            //   - Only update lights 'UpdateLights' if the light exists in stored data.

            //3. **Expected Commands & Responses**:
            //   - 'Turn on the desk light' → Validate existence, then 'UpdateLights'.
            //   - 'List all rooms' → Retrieve from stored rooms.
            //   - 'Get all light statuses' → Retrieve from stored lights.

            //Always prioritize retrieving data first before performing updates.
            //");

#pragma warning disable SKEXP0001 // Suppress the warning for evaluation purposes
            _reducer = new ChatHistoryTruncationReducer(targetCount: 4, thresholdCount: 6); // Keep System messages and reduce User messages
#pragma warning restore SKEXP0001

            _builder = Kernel.CreateBuilder();

            // Initialize the OpenAI Chat Connector.
            _builder.Services.AddOpenAIChatCompletion(
                modelId: chatModel,
                apiKey: openAiApiKey,
                orgId: openApiOrgId, // Optional; for OpenAI deployment
                serviceId: "lights" // Optional; for targeting specific services within Semantic Kernel
            );
            _kernel = _builder.Build();

            // Add filter directly to the Kernel instance
            _kernel.FunctionInvocationFilters.Add(new FunctionInvocationFilter());

            var uriOpenApi = new Uri($"https://{apiLocation}/openapi/v1/openapi.json");
            var uriServer = new Uri($"https://{apiLocation}");

            // Import the OpenAPI plugin into the _kernel.
            var client = new HttpClient
            {
                BaseAddress = uriServer
            };
            //client.DefaultRequestHeaders.Add("X-Tunnel-Authorization", "tunnel eyJhbGciOiJFUzI1NiIsImtpZCI6IkZCM0U2NTMwNjlDQ0I5MUFCQUUxRTNFQjk1RDc5NzdERDQxODM1QjYiLCJ0eXAiOiJKV1QifQ.eyJjbHVzdGVySWQiOiJ1c3czIiwidHVubmVsSWQiOiJmYW5jeS1yaXZlci0wY3JyNTUwIiwic2NwIjoiY29ubmVjdCIsImV4cCI6MTczOTA4NDAyMCwiaXNzIjoiaHR0cHM6Ly90dW5uZWxzLmFwaS52aXN1YWxzdHVkaW8uY29tLyIsIm5iZiI6MTczODk5NjcyMH0.J17Cw2wMJffdsp4_bFf2--PccruB7ikjNV92aWoK8G98vXuT-wQ_5oqZI33bOfpxP_LVGeI45jWBFMka_5dUOg");

            /////////////////////////////////////////////////////////////////////
            // Creation of the plugin from the OpenAPI document Endpoint
            var plugin = await _kernel.ImportPluginFromOpenApiAsync(
               pluginName: "lights",
               uri: uriOpenApi,

               executionParameters: new OpenApiFunctionExecutionParameters
               {
                   ServerUrlOverride = uriServer
               }
            );
            //////////////////////////////////////////////////////////////////////
            
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            // tell the openAI connector to invoke service if the prompt is understood
            _openAIPromptExecutionSettings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                //FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),
                Temperature = 1,      // Precise and deterministic -- Creativity level (0 = deterministic, 2 = highly random)
                FrequencyPenalty = 0.0, // Allows repeated words like "Turning on..."
                PresencePenalty = 0.0,  // Prevents forced response variations
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing kernel: {ex.Message}");
        }
    }

    int _lastTotalTokens = 0;
    int _totalTokens = 0;
    /// <summary>
    /// Call the plugin Chat Completion service
    /// </summary>
    /// <param name="prompt">Prompt to send to the OpenAI Chat completion service</param>
    /// <returns>Results from the OpenAI plugin</returns>
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
            // Get the current length of the chat history object
            int currentChatHistoryLength = _history.Count;

            _history.AddUserMessage(prompt);

            //var reducedMessages = await _reducer.ReduceAsync(_history);

            //if (reducedMessages is not null)
            //{
            //    _history = new ChatHistory(reducedMessages);
            //}

            ChatMessageContent result = await _chatCompletionService.GetChatMessageContentAsync(
            _history,
            executionSettings: _openAIPromptExecutionSettings,
            kernel: _kernel);

            response.Result = result.ToString();
            // Check if result.Metadata contains the key "Usage" and get the total token totalTokens.
            if (result.Metadata.ContainsKey("Usage"))
            {
                var usage = (OpenAI.Chat.ChatTokenUsage)result.Metadata["Usage"];
                var totalTokens = usage.TotalTokenCount;
                var inputTokens = usage.InputTokenCount - _lastTotalTokens;
                _lastTotalTokens = usage.InputTokenCount;
                var outputTokens = usage.OutputTokenCount;
                response.InputTokens = inputTokens;
                response.OutputTokens = outputTokens;
                _totalTokens += totalTokens;
                response.TotalTokens = _totalTokens;
                response.RequestTokens = totalTokens;
            }
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
/// Logs messages before and after a function is invoked. It allows for additional processing during the function
/// invocation.
/// Used if needed to decide which functions to call (invoked only if autoInvoke is false)
/// </summary>
/// <example>
/// 
///  _openAIPromptExecutionSettings = new()
///  {
///     FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false),
///     ....
/// 
///  _kernel.FunctionInvocationFilters.Add(new FunctionInvocationFilter());
///  
/// </example>
public sealed class FunctionInvocationFilter : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        Debug.WriteLine($"Function {context.Function.Name} is about to be invoked.");
        await next(context);
        Debug.WriteLine($"Function {context.Function.Name} was invoked.");
    }
}

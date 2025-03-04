using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LightsAPICommon;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Diagnostics;


namespace AIKernelClient.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    // Set upcredentials and endpoints.
    private const string openAiApiKey = "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";
    private const string chatModel = "gpt-4o-mini"; // or gpt-4 (but it is expensive)
    private const string openApiOrgId = "org-RRBnXYYjTq5b4qr7TLaaHsLD";

    //private const string apiLocation = "rh8xzzh8-5042.usw3.devtunnels.ms";
    private const string apiLocation = "localhost:5042";

    private ChatHistory _history;
    private IKernelBuilder _builder;
    private Kernel _kernel;
    private IChatCompletionService _chatCompletionService;
    private OpenAIPromptExecutionSettings _openAIPromptExecutionSettings;

    private int _promptIndex = -1;

    private readonly string[] _userPrompts = House.CustomPrompts;


    public MainPageViewModel()
    {
        InitializeKernelAndPluginAsync().ConfigureAwait(true);
    }


    private async Task InitializeKernelAndPluginAsync()
    {
        try
        {   // Initialize the ChatHistory object.
            _history = [];
            //TODO: Add Sytem prompts to keep the conversation context
            // https://learn.microsoft.com/en-us/semantic-kernel/concepts/ai-services/chat-completion/chat-history?pivots=programming-language-csharp
            //_history.AddSystemMessage("You are an home automation system controlling lights functions in the house, first get all the lights, then all the rooms and remeber where all the lights are located");


            _builder = Kernel.CreateBuilder();
            // Initialize the OpenAI Chat Connector.
            _builder.Services.AddOpenAIChatCompletion(
                modelId: chatModel,
                apiKey: openAiApiKey,
                orgId: openApiOrgId, // Optional; for OpenAI deployment
                serviceId: "lights" // Optional; for targeting specific services within Semantic Kernel
            );
            _kernel = _builder.Build();
            var uriOpenApi = new Uri($"https://{apiLocation}/openapi/v1/openapi.json");
            var uriServer = new Uri($"https://{apiLocation}");
            // Import the OpenAPI plugin into the _kernel.
            var client = new HttpClient
            {
                BaseAddress = uriServer
            };
            //client.DefaultRequestHeaders.Add("X-Tunnel-Authorization", "tunnel eyJhbGciOiJFUzI1NiIsImtpZCI6IkZCM0U2NTMwNjlDQ0I5MUFCQUUxRTNFQjk1RDc5NzdERDQxODM1QjYiLCJ0eXAiOiJKV1QifQ.eyJjbHVzdGVySWQiOiJ1c3czIiwidHVubmVsSWQiOiJmYW5jeS1yaXZlci0wY3JyNTUwIiwic2NwIjoiY29ubmVjdCIsImV4cCI6MTczOTA4NDAyMCwiaXNzIjoiaHR0cHM6Ly90dW5uZWxzLmFwaS52aXN1YWxzdHVkaW8uY29tLyIsIm5iZiI6MTczODk5NjcyMH0.J17Cw2wMJffdsp4_bFf2--PccruB7ikjNV92aWoK8G98vXuT-wQ_5oqZI33bOfpxP_LVGeI45jWBFMka_5dUOg");
            var plugin = await _kernel.ImportPluginFromOpenApiAsync(
               pluginName: "lights",
               uri: uriOpenApi,

               executionParameters: new OpenApiFunctionExecutionParameters
               {
                   ServerUrlOverride = uriServer
               }
            );
            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

            // tell the openAI connector to invoke sevice if the prompt is unnderstood
            _openAIPromptExecutionSettings = new()
            {
                //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, // Prefer using FuctionChoiceBehavior.Auto(autoInvoke: true)
                //FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false, options: new(){AllowParallelCalls = true,AllowConcurrentInvocation=true }),
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: true),
                Temperature = 0.4,      // Precise and deterministic -- Creativity level (0 = deterministic, 2 = highly random)
                TopP = 0.4,             // Limits randomness
                FrequencyPenalty = 0.0, // Allows repeated words like "Turning on..."
                PresencePenalty = 0.0,  // Prevents forced resp,onse variations
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing kernel: {ex.Message}");
        }
    }

    [ObservableProperty]
    public partial string CallTextInput { get; set; }

    [ObservableProperty]
    public partial string CallTextResult { get; set; }

    [ObservableProperty]
    public partial int TotalTokens { get; set; }

    [ObservableProperty]
    public partial int RequestTokens { get; set; }

    [ObservableProperty]
    public partial int InputTokens { get; set; }

    [ObservableProperty]
    public partial int OutputTokens { get; set; }


    [RelayCommand]
    private async Task SendRequest()
    {
        await GetResponseAsync(CallTextInput);
    }

    [RelayCommand]
    private void NextPrompt()
    {
        _promptIndex++;
        if (_promptIndex >= _userPrompts.Length)
        {
            _promptIndex = 0;
        }
        CallTextInput = _userPrompts[_promptIndex];
    }

    [RelayCommand]
    private void PreviousPrompt()
    {
        _promptIndex--;
        if (_promptIndex < 0)
        {
            _promptIndex = _userPrompts.Length - 1;
        }
        CallTextInput = _userPrompts[_promptIndex];
    }
    int _lastTotalTokens = 0;
    public async Task GetResponseAsync(string prompt)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                CallTextResult = "Please enter a prompt";
                return;
            }

            _history.AddUserMessage(prompt);
            var result = await _chatCompletionService.GetChatMessageContentAsync(
            _history,
            executionSettings: _openAIPromptExecutionSettings,
            kernel: _kernel);
            CallTextResult = result.ToString();
            // Check if result.Metadata contains the key "Usage" and get the total token totalTokens.
            if (result.Metadata.ContainsKey("Usage"))
            {
                var usage = (OpenAI.Chat.ChatTokenUsage)result.Metadata["Usage"];
                var totalTokens = usage.TotalTokenCount;
                var inputTokens = usage.InputTokenCount - _lastTotalTokens;
                _lastTotalTokens = usage.InputTokenCount;
                var outputTokens = usage.OutputTokenCount;
                InputTokens = inputTokens;
                OutputTokens = outputTokens;
                TotalTokens += totalTokens;
                RequestTokens = totalTokens;
            }

            _ = CheckForFunctionCallsAsync(result);

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting response: {ex.Message}");
            CallTextResult = $"Error getting response: {ex.Message}";
        }
    }


    /// <summary>
    /// Check if the AI model has chosen any function for invocation.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task CheckForFunctionCallsAsync(ChatMessageContent result)
    {
        // Check if the AI model has chosen any function for invocation.
        IEnumerable<FunctionCallContent> functionCalls = FunctionCallContent.GetFunctionCalls(result);
        if (!functionCalls.Any())
        {
            return;
        }

        // Sequentially iterating over each chosen function, invoke it, and add the result to the chat history.
        foreach (FunctionCallContent functionCall in functionCalls)
        {
            try
            {
                // Invoking the function
                FunctionResultContent resultContent = await functionCall.InvokeAsync(_kernel);

                // Adding the function result to the chat history
                //chatHistory.Add(resultContent.ToChatMessage());
            }
            catch (Exception ex)
            {
                // Adding function exception to the chat history.
                //chatHistory.Add(new FunctionResultContent(functionCall, ex).ToChatMessage());
                // or
                //chatHistory.Add(new FunctionResultContent(functionCall, "Error details that the AI model can reason about.").ToChatMessage());
            }
        }
    }
}

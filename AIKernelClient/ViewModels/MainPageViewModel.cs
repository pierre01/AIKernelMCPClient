using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System.Diagnostics;
using LightsAPICommon;


namespace AIKernelClient.ViewModels;

public partial class MainPageViewModel : ObservableObject
{
    // Set upcredentials and endpoints.
    private const string openAiApiKey = "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";
    private const string chatModel = "gpt-4o-mini"; // or gpt-4 (but it is expensive)
    private const string openApiOrgId = "org-RRBnXYYjTq5b4qr7TLaaHsLD";

    //private const string devTunnel = "rh8xzzh8-5042.usw3.devtunnels.ms";
    private const string devTunnel = "localhost:5042";

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
            var uriOpenApi = new Uri($"https://{devTunnel}/openapi/v1/openapi.json");
            var uriServer = new Uri($"https://{devTunnel}");
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
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.4,  // Creativity level (0 = deterministic, 2 = highly random)
                TopP = 0.4,         // Nucleus sampling for diversity
                PresencePenalty = 0.0, // Penalize repeating topics
                FrequencyPenalty = 0.0 // Penalize repeated words
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

    public async Task GetResponseAsync(string prompt)
    {
        try
        {
            if(string.IsNullOrWhiteSpace(prompt))
            {
                CallTextResult = "Please enter a prompt";
                return;
            }

            _history.AddUserMessage(prompt);
            //var result2 = await _chatCompletionService.GetChatMessageContentsAsync(
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
                var inputTokens =usage.InputTokenCount;
                var outputTokens =usage.OutputTokenCount;
                //var totalTokens = usage.TotalTokenCount;
                InputTokens = inputTokens;
                OutputTokens = outputTokens;
                TotalTokens += totalTokens;
                RequestTokens = totalTokens;
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting response: {ex.Message}");
            CallTextResult = $"Error getting response: {ex.Message}";
        }
    }

}

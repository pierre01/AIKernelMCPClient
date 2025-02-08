using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


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

    private int _promptIndex = 0;

    private readonly string[] _userPrompts =
    [
        "Turn on the living room lights.",
        "Switch all the lights in the Kitchen on, as well as the office lights, then change them to a medium intensity.",
        "Switch the kitchen lights to a very warm color (e.g. like a 2000K bulb)",
        "Are the lights in the office on or off?",
        "Turn off all the lights.",
        "When I tell you 'I'm home' you will switch the front living room lights, then the wall lights as well as the stairs lights, all with medium brightness",
        "I'm home",
        "when I tell you 'I'm leaving' you will turn all the lights off",
        "I'm leaving",
        "How many lights are currently on?"
    ];

    public MainPageViewModel()
    {
        InitializeKernelAndPluginAsync().ConfigureAwait(true);
    }


    private async Task InitializeKernelAndPluginAsync()
    {
        try
        {   // Initialize the ChatHistory object.
            _history = new ChatHistory();
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
            var client = new HttpClient();
            client.BaseAddress = uriServer;
            client.DefaultRequestHeaders.Add("X-Tunnel-Authorization","tunnel eyJhbGciOiJFUzI1NiIsImtpZCI6IkZCM0U2NTMwNjlDQ0I5MUFCQUUxRTNFQjk1RDc5NzdERDQxODM1QjYiLCJ0eXAiOiJKV1QifQ.eyJjbHVzdGVySWQiOiJ1c3czIiwidHVubmVsSWQiOiJmYW5jeS1yaXZlci0wY3JyNTUwIiwic2NwIjoiY29ubmVjdCIsImV4cCI6MTczOTA4NDAyMCwiaXNzIjoiaHR0cHM6Ly90dW5uZWxzLmFwaS52aXN1YWxzdHVkaW8uY29tLyIsIm5iZiI6MTczODk5NjcyMH0.J17Cw2wMJffdsp4_bFf2--PccruB7ikjNV92aWoK8G98vXuT-wQ_5oqZI33bOfpxP_LVGeI45jWBFMka_5dUOg");
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
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
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

    [RelayCommand]
    private async Task SendRequest()
    {
        await GetResponseAsync(CallTextInput);
    }

    [RelayCommand]
    private  void NextPrompt()
    {
        CallTextInput = _userPrompts[_promptIndex];
        _promptIndex++;
        if (_promptIndex >= _userPrompts.Length)
        {
            _promptIndex = 0;
        }
    }

    public async Task GetResponseAsync(string prompt)
    {
        try
        {
            _history.AddUserMessage(prompt);
            var result = await _chatCompletionService.GetChatMessageContentAsync(
            _history,
            executionSettings: _openAIPromptExecutionSettings,
            kernel: _kernel);            
            CallTextResult = result.ToString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error getting response: {ex.Message}");
            CallTextResult = $"Error getting response: {ex.Message}";
        }
    }

}

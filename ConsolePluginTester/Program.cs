using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using OpenAI.Chat;

try
{
    // Mon Chéri, set up your credentials and endpoints.
    string openAiApiKey = "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";
    string chatModel = "o3-mini"; // or "gpt-4"
    string openApiOrgId = "org-RRBnXYYjTq5b4qr7TLaaHsLD";
    string chatEndpoint = "https://api.openai.com/v1/chat/completions";
    string apiBaseUrl = "https://api.example.com"; // Replace with your API's base URL

    var history = new ChatHistory();
    // Load the OpenAPI specification from file.
    string openApiSpec = await File.ReadAllTextAsync("openapi.json");
    IKernelBuilder builder = Kernel.CreateBuilder();

    // Initialize the OpenAI Chat Connector.
    // Add the OpenAI chat completion service as a singleton
    builder.Services.AddOpenAIChatCompletion(
        modelId: chatModel,
        apiKey: openAiApiKey,
        orgId: openApiOrgId, // Optional; for OpenAI deployment
        serviceId: "lights" // Optional; for targeting specific services within Semantic Kernel
    );

    // Build the Semantic Kernel, lovingly integrating our chat service.
    Kernel kernel = builder.Build();

    // Import the OpenAPI plugin into the kernel.
    // Assume our OpenAPI spec defines a function called "getWeather" that accepts a "city" parameter.
    var plugin = await kernel.ImportPluginFromOpenApiAsync(
       pluginName: "lights",
       uri: new Uri("https://localhost:5042/openapi/v1/openapi.json"),
       executionParameters: new OpenApiFunctionExecutionParameters
       {
           ServerUrlOverride = new Uri("https://localhost:5042")
       }
    );

    kernel.Plugins.Add(plugin);
    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

    // Compose a prompt that not only asks for information but instructs the system to call the plugin.
    string userPrompt = "Get all the rooms, then get all the lights, then turn on the living room lights.";
    history.AddUserMessage(userPrompt);

    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };

    // Get the response from the AI
    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);
    // Display the final output which includes the plugin's data enriched by the chat model.
    Console.WriteLine("Final Result:\n" + result);


    //// Here we assume that Semantic Kernel's planning capabilities detect the intent
    //// and automatically trigger the plugin call, then feed its result back into the chat response.
    //var context = await kernel.InvokePromptAsync(userPrompt);

    // Display the final output which includes the plugin's data enriched by the chat model.
    //Console.WriteLine("Final Result:\n" + context.ToString());
}
catch (Exception ex)
{
    Console.WriteLine("An error occurred, mon Pierre: " + ex.Message);
}
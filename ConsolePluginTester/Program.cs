using System;
using System.IO;
using System.Threading.Tasks;
using LightsAPICommon;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.OpenApi;

try
{
    // Set upcredentials and endpoints.
    string openAiApiKey = "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";
    string chatModel = "gpt-4o-mini"; // or gpt-4 (but it is expensive)
    string openApiOrgId = "org-RRBnXYYjTq5b4qr7TLaaHsLD";

    var history = new ChatHistory();
    IKernelBuilder builder = Kernel.CreateBuilder();

    // Initialize the OpenAI Chat Connector.
    builder.Services.AddOpenAIChatCompletion(
        modelId: chatModel,
        apiKey: openAiApiKey,
        orgId: openApiOrgId, // Optional; for OpenAI deployment
        serviceId: "lights" // Optional; for targeting specific services within Semantic Kernel
    );

    // Build the Semantic Kernel, integrating our chat service.
    Kernel kernel = builder.Build();

    // Import the OpenAPI plugin into the kernel.
    var plugin = await kernel.ImportPluginFromOpenApiAsync(
       pluginName: "lights",
       uri: new Uri("https://localhost:5042/openapi/v1/openapi.json"),
       executionParameters: new OpenApiFunctionExecutionParameters
       {
           ServerUrlOverride = new Uri("https://localhost:5042")
       }
    );    
    //kernel.Plugins.Add(plugin); // if you ahve a local interface use this instead of the above

    var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

    // tell the openAI connector to invoke sevice if the prompt is unnderstood
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };
    // 2. Enable automatic function calling
    //OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new() 
    //{
    //    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
    //};

    string[] userPrompts = House.CustomPrompts;

    // Compose a prompt that not only asks for information but instructs the system to call the plugin.
    for (int i = 0; i < userPrompts.Length; i++)
    {
        history.AddUserMessage(userPrompts[i]);

        // Get the response from the AI
        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            executionSettings: openAIPromptExecutionSettings,
            kernel: kernel);
        // Display the final output which includes the plugin's data enriched by the chat model.
        Console.WriteLine("Final Result:\n" + result);
    }

    Console.WriteLine("All prompts completed.");
    foreach (var message in history)
    {
        Console.WriteLine(message.Role);
        foreach (var item in message.Items)
        {
            Console.WriteLine(item.InnerContent);
        }
    }

}
catch (Exception ex)
{
    Console.WriteLine("An error occurred, mon Pierre: " + ex.Message);
}
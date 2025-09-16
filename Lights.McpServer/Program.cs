using Lights.McpServer;
using LightsAPICommon;
using LightsAPICommon.Serialization;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

internal partial class Program
{
    [RequiresUnreferencedCode("Calls Microsoft.Extensions.DependencyInjection.McpServerBuilderExtensions.WithToolsFromAssembly(Assembly, JsonSerializerOptions)")]
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateSlimBuilder(args);

        // Configure Kestrel to use HTTPS
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(3001, listenOptions =>
            {
                listenOptions.UseHttps(); // HTTPS
            });
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, LightsJsonContext.Default);
        });

        // Combine resolvers so AOT metadata is available for *all* involved types
        var toolSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = LightsJsonContext.Default   // <- only your context
        };


        builder.Services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly(serializerOptions: toolSerializerOptions);

        var app = builder.Build();

        var mcpGroup = app.MapGroup("/mcp");
        mcpGroup.MapMcp();   // <— call MapMcp on the group; all routes get the prefix + auth

        app.Run();
    }
}

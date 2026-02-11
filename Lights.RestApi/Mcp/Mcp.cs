using ModelContextProtocol.Protocol;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lights.RestApi.Mcp;
internal static class Mcp
{
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public static CallToolResult Ok<T>(
        T value,
        JsonSerializerOptions json,
        string? message = null)
    {
        var r = new CallToolResult { IsError = false };

        r.StructuredContent = JsonNode.Parse(JsonSerializer.Serialize(value, json));

        if (!string.IsNullOrWhiteSpace(message))
            r.Content.Add(new TextContentBlock { Text = message });

        return r;
    }

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public static CallToolResult Error(
        string message,
        string code,
        object? details = null)
    {
        var r = new CallToolResult { IsError = true };

        r.Content.Add(new TextContentBlock { Text = message });

        r.StructuredContent = JsonNode.Parse(JsonSerializer.Serialize(new
        {
            error = new
            {
                code,
                message,
                details
            }
        }));

        return r;
    }
}


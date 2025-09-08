using System.Text.RegularExpressions;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using NJsonSchema;

public sealed class ToolRegistry
{
    public required List<MappedTool> Tools { get; init; }
    public JArray ExportForMcp()
    {
        var arr = new JArray();
        foreach (var t in Tools)
        {
            arr.Add(new JObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["inputSchema"] = t.InputSchema
            });
        }
        return arr;
    }
}

public sealed class MappedTool
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Route { get; init; }
    public required string Method { get; init; }
    public required JObject InputSchema { get; init; }

    // Parameter mapping
    public required List<string> PathParams { get; init; }
    public required List<string> QueryParams { get; init; }
    public required string? BodyParamName { get; init; }
}

public static class OpenApiToolMapper
{
    public static ToolRegistry BuildTools(OpenApiDocument doc)
    {
        var tools = new List<MappedTool>();

        foreach (var (path, pathItem) in doc.Paths)
        {
            foreach (var (method, op) in pathItem.Operations)
            {
                var name = !string.IsNullOrWhiteSpace(op.OperationId)
                    ? op.OperationId
                    : $"{method}_{SanitizePath(path)}";

                var desc = op.Summary ?? op.Description ?? $"{method} {path}";

                // Collect parameters
                var pathParams = new List<string>();
                var queryParams = new List<string>();
                foreach (var p in op.Parameters)
                {
                    if (p.In == ParameterLocation.Path) pathParams.Add(p.Name);
                    else if (p.In == ParameterLocation.Query) queryParams.Add(p.Name);
                }

                // Body schema (if any)
                string? bodyParamName = null;
                JObject inputSchema = OpenApiSchemaConverter.BuildInputJsonSchema(op, out bodyParamName);

                tools.Add(new MappedTool
                {
                    Name = name,
                    Description = desc,
                    Route = path,
                    Method = method.ToString().ToUpperInvariant(),
                    InputSchema = inputSchema,
                    PathParams = pathParams,
                    QueryParams = queryParams,
                    BodyParamName = bodyParamName
                });
            }
        }

        return new ToolRegistry { Tools = tools };
    }

    public static (string url, string? jsonBody) BindHttpRequest(
        string routeTemplate,
        string method,
        MappedTool tool,
        JObject? input)
    {
        input ??= new JObject();

        // Expand path params: /items/{id}
        var url = routeTemplate;
        foreach (var p in tool.PathParams)
        {
            var val = input[p]?.ToString() ?? throw new ArgumentException($"Missing path parameter '{p}'");
            url = Regex.Replace(url, "\\{" + Regex.Escape(p) + "\\}", Uri.EscapeDataString(val));
        }

        // Build query string
        var queryPairs = new List<string>();
        foreach (var q in tool.QueryParams)
        {
            if (input.TryGetValue(q, out var v) && v.Type != JTokenType.Null)
            {
                queryPairs.Add($"{Uri.EscapeDataString(q)}={Uri.EscapeDataString(v.ToString())}");
            }
        }
        if (queryPairs.Count > 0)
        {
            url += (url.Contains("?") ? "&" : "?") + string.Join("&", queryPairs);
        }

        // Body
        string? body = null;
        if (tool.BodyParamName != null && input.TryGetValue(tool.BodyParamName, out var bodyToken))
        {
            body = bodyToken.ToString(Newtonsoft.Json.Formatting.None);
        }
        else if (tool.BodyParamName == null && (method == "POST" || method == "PUT" || method == "PATCH"))
        {
            // If the operation expects a body but wasn’t named by converter, fall back to all extra props
            var extras = new JObject(input);
            foreach (var k in tool.PathParams.Concat(tool.QueryParams))
                extras.Remove(k);

            if (extras.Properties().Any())
                body = extras.ToString(Newtonsoft.Json.Formatting.None);
        }

        return (url, body);
    }

    private static string SanitizePath(string path)
        => path.Trim('/').Replace("/", "_").Replace("{", "").Replace("}", "");
}

// File: OpenApiToMcp.cs
using System.Text.Json;
using System.Text.Json.Nodes;

public static class OpenApiToMcp
{
    public static OpenApiDoc Load(string openApiJsonPath, string? serverOverride = null)
    {
        var root = JsonNode.Parse(File.ReadAllText(openApiJsonPath))!.AsObject();

        var info = root["info"]?.AsObject();
        var title = (string?)info?["title"] ?? "OpenAPI";
        var version = (string?)info?["version"] ?? "0.0.0";

        string? serverUrl = serverOverride;
        if (serverUrl is null)
        {
            var servers = root["servers"]?.AsArray();
            serverUrl = servers?.FirstOrDefault()?["url"]?.ToString();
        }

        var tools = new List<McpToolDef>();
        var paths = root["paths"]?.AsObject() ?? new JsonObject();

        foreach (var (path, pathObjNode) in paths)
        {
            var pathObj = pathObjNode!.AsObject();
            foreach (var kv in pathObj)
            {
                var method = kv.Key.ToUpperInvariant(); // get/post/patch…
                if (method is not ("GET" or "POST" or "PUT" or "PATCH" or "DELETE")) continue;

                var op = kv.Value!.AsObject();

                // name/description
                var opId = (string?)op["operationId"] ?? $"{method}_{path.Trim('/').Replace('/', '_')}";
                var desc = (string?)op["description"] ?? (string?)op["summary"] ?? opId;

                // Build JSON Schema for parameters
                // We combine path/query/header/body into one object schema.
                var schema = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject(),
                    ["required"] = new JsonArray()
                };
                var props = (JsonObject)schema["properties"]!;
                var required = (JsonArray)schema["required"]!;

                // 1) Parameters (path/query/header/cookie)
                var parameters = op["parameters"]?.AsArray();
                if (parameters is not null)
                {
                    foreach (var pNode in parameters)
                    {
                        var p = pNode!.AsObject();
                        var name = (string?)p["name"] ?? "";
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        var req = p["required"]?.GetValue<bool>() ?? false;
                        if (req) required.Add(name);

                        var pSchema = p["schema"] as JsonObject ?? new JsonObject { ["type"] = "string" };
                        props[name] = pSchema.DeepClone();
                    }
                }

                // 2) Request body (JSON only)
                var reqBody = op["requestBody"]?.AsObject();
                var content = reqBody?["content"]?.AsObject();
                var appJson = content?["application/json"]?.AsObject();
                var bodySchema = appJson?["schema"] as JsonObject;
                if (bodySchema is not null)
                {
                    // Flatten body under a single arg 'body'
                    props["body"] = bodySchema.DeepClone();
                    if (reqBody?["required"]?.GetValue<bool>() == true) required.Add("body");
                }

                // Create tool def
                var tool = new McpToolDef(
                    Name: SanitizeToolName(opId),
                    Description: desc,
                    ParametersSchema: schema,
                    HttpMethod: method,
                    PathTemplate: path
                );
                tools.Add(tool);
            }
        }

        return new OpenApiDoc(title, version, serverUrl, tools);
    }

    static string SanitizeToolName(string s)
    {
        // Keep MCP tool names simple: letters, digits, underscores
        var clean = new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
        return clean.Trim('_');
    }
}


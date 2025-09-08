using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using NJsonSchema;

public static class OpenApiSchemaConverter
{
    /// <summary>
    /// Builds a single JSON schema object for the tool input.
    /// - All path & query params become top-level properties.
    /// - If there is a requestBody (application/json), we wrap it as a single property named:
    ///     1) The schema's title if present, else
    ///     2) "body"
    /// </summary>
    public static JObject BuildInputJsonSchema(OpenApiOperation op, out string? bodyParamName)
    {
        var root = new JsonSchema
        {
            Type = JsonObjectType.Object,
            AllowAdditionalProperties = true
        };

        // Path & Query parameters → top-level properties
        foreach (var p in op.Parameters)
        {
            var prop = ConvertToProperty(p.Schema);
            // Add description, format, defaults from the OpenAPI param itself if present
            if (!string.IsNullOrWhiteSpace(p.Description))
                prop.Description = p.Description;

            root.Properties[p.Name] = prop;

            // In NJsonSchema, "required" is set per-property via IsRequired
            if (p.Required)
                prop.IsRequired = true;
        }

        bodyParamName = null;

        // Request body (JSON only)
        if (op.RequestBody != null &&
            op.RequestBody.Content.TryGetValue("application/json", out var media) &&
            media?.Schema != null)
        {
            var bodyProp = ConvertToProperty(media.Schema);

            bodyParamName = media.Schema.Title?.Trim();
            if (string.IsNullOrWhiteSpace(bodyParamName))
                bodyParamName = "body";

            root.Properties[bodyParamName] = bodyProp;

            if (op.RequestBody.Required)
                bodyProp.IsRequired = true;
        }

        // Serialize as JSON Schema Draft-07 for MCP
        var j = JObject.Parse(root.ToJson());
        j["$schema"] = "http://json-schema.org/draft-07/schema#";
        return j;
    }

    /// <summary>
    /// Convert an OpenAPI schema to a JsonSchemaProperty (recursively).
    /// JsonSchemaProperty derives from JsonSchema, so it carries items/properties as well.
    /// </summary>
    private static JsonSchemaProperty ConvertToProperty(OpenApiSchema? o)
    {
        var prop = new JsonSchemaProperty
        {
            AllowAdditionalProperties = true
        };

        if (o == null)
            return prop;

        // Map basic JSON types
        prop.Type = o.Type switch
        {
            "string" => JsonObjectType.String,
            "integer" => JsonObjectType.Integer,
            "number" => JsonObjectType.Number,
            "boolean" => JsonObjectType.Boolean,
            "array" => JsonObjectType.Array,
            "object" => JsonObjectType.Object,
            _ => JsonObjectType.None
        };

        // Format / description / default
        if (!string.IsNullOrWhiteSpace(o.Format))
            prop.Format = o.Format;

        if (!string.IsNullOrWhiteSpace(o.Description))
            prop.Description = o.Description;

        if (o.Default != null)
            prop.Default = o.Default;

        // Enum
        if (o.Enum != null && o.Enum.Count > 0)
        {
            foreach (var e in o.Enum)
                prop.Enumeration.Add(e?.ToString());
        }

        // Arrays
        if (prop.Type == JsonObjectType.Array && o.Items != null)
        {
            // For arrays, set Item to the element schema
            prop.Item = ConvertToSchema(o.Items);
        }

        // Objects (nested properties)
        if (prop.Type == JsonObjectType.Object && o.Properties != null && o.Properties.Count > 0)
        {
            foreach (var kv in o.Properties)
            {
                var childName = kv.Key;
                var childOpenApi = kv.Value;

                var childProp = ConvertToProperty(childOpenApi);
                prop.Properties[childName] = childProp;

                // OpenAPI required list lives on the parent schema
                if (o.Required != null && o.Required.Contains(childName))
                    childProp.IsRequired = true;
            }
        }

        // Nullable (OpenAPI 3): if nullable, allow null as well
        if (o.Nullable == true)
        {
            // NJsonSchema does not have a direct "nullable" flag for draft-07;
            // allowing multiple types is the usual approach (string|null, etc.).
            // Easiest: leave type as-is and rely on consumers tolerating nulls.
            // If you need strict null support, you can model using oneOf with "null".
        }

        return prop;
    }

    /// <summary>
    /// Helper: convert OpenAPI schema to a plain JsonSchema (used for array item).
    /// </summary>
    private static JsonSchema ConvertToSchema(OpenApiSchema? o)
    {
        // Reuse ConvertToProperty then cast up (JsonSchemaProperty derives from JsonSchema)
        return ConvertToProperty(o);
    }
}

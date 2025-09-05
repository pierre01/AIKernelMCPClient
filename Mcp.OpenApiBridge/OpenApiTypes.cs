using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

public sealed record McpToolDef(
    string Name,
    string Description,
    JsonObject ParametersSchema,   // JSON Schema for args
    string HttpMethod,             // GET/POST/PATCH/PUT/DELETE
    string PathTemplate            // e.g. "/lights/{id}"
);

public sealed record OpenApiDoc(
    string Title,
    string Version,
    string? ServerUrl,
    List<McpToolDef> Tools
);

using System.Collections.Generic;
using System.Text.Json.Serialization;
using LightsAPICommon; // where Light is defined

namespace Lights.McpServer.Serialization;

// If you prefer Web defaults (camelCase, etc.), add: JsonSourceGenerationOptions(GenerationMode = ...)
[JsonSerializable(typeof(List<Light>))]
[JsonSerializable(typeof(List<Room>))]
[JsonSerializable(typeof(Light[]))]
[JsonSerializable(typeof(Room[]))]
[JsonSerializable(typeof(Light))]
[JsonSerializable(typeof(Capabilities))]
[JsonSerializable(typeof(List<LightUpdateRequest>))]
[JsonSerializable(typeof(List<UpdateLightResponse>))]
[JsonSerializable(typeof(LightUpdateRequest[]))]
[JsonSerializable(typeof(UpdateLightResponse[]))]
[JsonSerializable(typeof(UpdateLightResponse))]
[JsonSerializable(typeof(LightUpdateRequest))]
[JsonSerializable(typeof(PatchResponse))]
[JsonSerializable(typeof(PatchRequest))]
[JsonSerializable(typeof(House))]
internal partial class LightsJsonContext : JsonSerializerContext
{
}

using System.Text.Json.Serialization;

namespace UOTDBot;

[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class AppJsonContext : JsonSerializerContext;

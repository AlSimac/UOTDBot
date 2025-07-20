using System.Text.Json.Serialization;
using UOTDBot.Models;

namespace UOTDBot;

[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<ulong>))]
[JsonSerializable(typeof(MapFeatures))]
public sealed partial class AppJsonContext : JsonSerializerContext;

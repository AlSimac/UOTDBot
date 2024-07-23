namespace UOTDBot.Models;

public sealed class MapFeatures
{
    public required string DefaultCar { get; set; }
    public List<string> Gates { get; set; } = [];
    public Dictionary<string, CarDistribution>? CarDistribution { get; set; }
    public float? NonStadiumDistribution { get; set; }
}

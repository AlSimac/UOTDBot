namespace UOTDBot.Models;

public sealed class MapFeatures
{
    public int Id { get; set; }
    public required Car DefaultCar { get; set; }

    public int MapId { get; set; }
    public Map Map { get; set; } = default!;

    public List<Car> Gates { get; } = [];
}

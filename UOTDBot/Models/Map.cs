namespace UOTDBot.Models;

internal sealed class Map
{
    public int Id { get; set; }
    public required Guid MapId { get; set; }
    public required string MapUid { get; set; }
    public required string Name { get; set; }
}

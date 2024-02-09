using TmEssentials;

namespace UOTDBot.Models;

public sealed class Map
{
    public int Id { get; set; }
    public required Guid MapId { get; set; }
    public required string MapUid { get; set; }
    public required string Name { get; set; }
    public required string ThumbnailUrl { get; set; }
    public required string DownloadUrl { get; set; }
    public required TimeInt32 AuthorTime { get; set; }
    public required int FileSize { get; set; }
    public required DateTimeOffset UploadedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
}

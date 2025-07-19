using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TmEssentials;

namespace UOTDBot.Models;

[Index(nameof(MapId), IsUnique = true)]
[Index(nameof(MapUid))]
public sealed class Map
{
    public int Id { get; set; }
    public required Guid MapId { get; set; }

    [StringLength(32)]
    public required string MapUid { get; set; }

    [StringLength(byte.MaxValue)]
    public required string Name { get; set; }

    [StringLength(byte.MaxValue)]
    public required string ThumbnailUrl { get; set; }

    [StringLength(byte.MaxValue)]
    public required string DownloadUrl { get; set; }

    public required TimeInt32 AuthorTime { get; set; }
    public required int FileSize { get; set; }
    public required DateTimeOffset UploadedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required MapFeatures Features { get; set; }
    public required DateOnly Totd { get; set; }
    public required Guid AuthorGuid { get; set; }

    [StringLength(byte.MaxValue)]
    public string? AuthorName { get; set; }

    public int? CupId { get; set; }
}

namespace UOTDBot.Models;

public sealed class ReportMessage
{
    public int Id { get; set; }
    public required ulong MessageId { get; set; }
    public ulong? OriginalChannelId { get; set; } // for generating jump links
    public required Map Map { get; set; }
    public ReportChannel? Channel { get; set; }
    public ReportUser? DM { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

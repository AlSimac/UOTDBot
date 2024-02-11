namespace UOTDBot.Models;

public sealed class ReportChannel
{
    public int Id { get; set; }
    public required ulong ChannelId { get; set; }
    public required ulong GuildId { get; set; }
    public required bool IsEnabled { get; set; }
    public required bool AutoThread { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required ReportConfiguration Configuration { get; set; }
}

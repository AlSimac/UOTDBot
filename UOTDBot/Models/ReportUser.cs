using Microsoft.EntityFrameworkCore;

namespace UOTDBot.Models;

[Index(nameof(UserId), IsUnique = true)]
public sealed class ReportUser
{
    public int Id { get; set; }
    public required ulong UserId { get; set; }
    public required bool IsEnabled { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required ReportConfiguration Configuration { get; set; }
}

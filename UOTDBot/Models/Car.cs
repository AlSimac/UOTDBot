using System.ComponentModel.DataAnnotations;

namespace UOTDBot.Models;

public sealed class Car
{
    [StringLength(16)]
    public required string Id { get; set; }

    [StringLength(byte.MaxValue)]
    public string? DisplayName { get; set; }

    public string GetName(ReportConfiguration config) => config.Format switch
    {
        "official" => Id,
        "standard" or _ => DisplayName ?? Id
    };
}

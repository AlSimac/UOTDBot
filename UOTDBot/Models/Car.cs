namespace UOTDBot.Models;

public sealed class Car
{
    public required string Id { get; set; }
    public string? DisplayName { get; set; }

    public string GetName(ReportConfiguration config) => config.Format switch
    {
        "official" => Id,
        "standard" or _ => DisplayName ?? Id
    };
}

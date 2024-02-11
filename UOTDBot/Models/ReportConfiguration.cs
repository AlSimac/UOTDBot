namespace UOTDBot.Models;

public sealed class ReportConfiguration
{
    public int Id { get; set; }
    public string Format { get; set; } = "standard";
    public Dictionary<string, string> Emotes { get; set; } = [];
}

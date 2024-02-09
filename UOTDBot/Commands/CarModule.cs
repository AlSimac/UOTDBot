using Discord;
using Discord.Interactions;

namespace UOTDBot.Commands;

[Group("car", "Features related to cars.")]
public sealed class CarModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AppDbContext _db;

    public CarModule(AppDbContext db)
    {
        _db = db;
    }

    [SlashCommand("emote", "Get or set the car emote for reports.")]
    public async Task Emote([Choice("SnowCar", "CarSnow")] string car, string? emote = null, bool reset = false)
    {
        if (Discord.Emote.TryParse(emote, out var emoteModel))
        {
            await RespondAsync($"Car emote for {car} set to {emoteModel}.");
        }
        else if (Emoji.TryParse(emote, out var emojiModel))
        {
            await RespondAsync($"Car emote for {car} set to {emojiModel}.");
        }
        else
        {
            await RespondAsync("Invalid emote.");
        }
    }

    [SlashCommand("format", "Get or set the car name format for reports.")]
    public async Task Format([Choice("SnowCar, RallyCar, ...", "standard"), Choice("CarSnow, CarRally, ...", "official")] string format)
    {

    }
}
using Discord;
using Discord.Interactions;

namespace UOTDBot.Modules;

public sealed class RulesModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("rules", "What are the requirements of TOTD to be reported by UOTD?")]
    public async Task Rules()
    {
        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("UOTD Rules")
            .WithDescription("The UOTD must be a Time of the Day (TOTD) map.")
            .AddField("1. Map Type", "The map must be a Time of the Day (TOTD) map.")
            .AddField("2. Map Size", "The map must be 90x90 or larger.")
            .AddField("3. Map Environment", "The map must be a Stadium map.")
            .AddField("4. Map Author Time", "The map must have an author time of 45 seconds or less.")
            .AddField("5. Map Thumbnail", "The map must have a thumbnail.")
            .AddField("6. Map Download", "The map must have a download link.")
            .WithColor(Color.Blue)
            .Build());
    }
}

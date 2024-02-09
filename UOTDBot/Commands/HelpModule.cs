using Discord;
using Discord.Interactions;

namespace UOTDBot.Commands;

public sealed class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Show help.")]
    public async Task Help()
    {
        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("United of the Day (UOTD)")
            .WithDescription($"UOTD is a bot by {MentionUtils.MentionUser(297344639776587784)} & {MentionUtils.MentionUser(241249362301157376)} that reports Trackmania (2020) track of the days (Cup of the Day maps) that use a different car than the Stadium one.\nThis bot has been made to support the TMUF community and the new direction of TM2020.")
            .AddField("Commands", "```/report subscribe [channel: other]\n/report unsubscribe [channel: other]\n/report dm```")
            .Build());
    }
}

using Discord;
using Discord.Interactions;

namespace UOTDBot.Modules;

public sealed class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Show help.")]
    public async Task Help()
    {
        await RespondAsync(embed: new EmbedBuilder()
            .WithTitle("United of the Day (UOTD)")
            .WithDescription($"UOTD (United of the Day) is a bot by {MentionUtils.MentionUser(297344639776587784)} & {MentionUtils.MentionUser(241249362301157376)} that reports Trackmania (2020) track of the days (Cup of the Day maps) that use a different car than the Stadium one. This bot has been made to support the TMUF/TM2 community and the new direction of TM2020.\n\nTo receive these 'UOTD' reports, start with the `/report subscribe` command.\n\nSee `/rules` to know the exact specifics when UOTD is reported.\n\nThank you and enjoy!")
            .AddField("Powered by", "- [GBX.NET](https://github.com/BigBang1112/gbx-net)\n- [ManiaAPI.NET](https://github.com/BigBang1112/maniaapi-net/tree/dev)\n- [TmEssentials](https://github.com/BigBang1112/tm-essentials)\n- [Discord.NET](https://discordnet.dev/)")
            .AddField("Support", $"- [GitHub](https://github.com/AlSimac/UOTDBot) or {MentionUtils.MentionUser(241249362301157376)} / {MentionUtils.MentionUser(297344639776587784)}")
            .Build(),
                ephemeral: true);
    }
}

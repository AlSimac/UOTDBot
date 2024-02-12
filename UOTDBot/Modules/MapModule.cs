using Discord.Interactions;

namespace UOTDBot.Modules;

[Group("map", "UOTD maps.")]
public sealed class MapModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AppDbContext _db;

    public MapModule(AppDbContext db)
    {
        _db = db;
    }

    [SlashCommand("list", "Get the UOTD map list.")]
    public async Task List()
    {
        
    }
}
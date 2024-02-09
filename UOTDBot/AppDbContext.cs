using Microsoft.EntityFrameworkCore;
using TmEssentials;
using UOTDBot.Models;

namespace UOTDBot;

public sealed class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Map> Maps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Map>()
            .Property(x => x.AuthorTime)
            .HasConversion(
                timeInt32 => timeInt32.TotalMilliseconds,
                totalMs => new TimeInt32(totalMs));
    }
}

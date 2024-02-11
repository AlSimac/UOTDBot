using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Hosting;
using TmEssentials;
using UOTDBot.Models;

namespace UOTDBot;

public sealed class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Map> Maps { get; set; }
    public DbSet<ReportChannel> ReportChannels { get; set; }
    public DbSet<ReportUser> ReportUsers { get; set; }
    public DbSet<ReportMessage> ReportMessages { get; set; }
    public DbSet<Car> Cars { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .Entity<Map>()
            .Property(x => x.AuthorTime)
            .HasConversion(
                timeInt32 => timeInt32.TotalMilliseconds,
                totalMs => new TimeInt32(totalMs));

        modelBuilder
            .Entity<ReportMessage>()
            .Property(x => x.CreatedAt)
            .HasConversion(new DateTimeOffsetToBinaryConverter());

        modelBuilder.Entity<Car>().HasData(
            new Car { Id = "CarSnow", DisplayName = "SnowCar" },
            new Car { Id = "CarRally", DisplayName = "RallyCar" },
            new Car { Id = "CarDesert", DisplayName = "DesertCar" });
    }
}

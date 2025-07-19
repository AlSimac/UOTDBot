using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Collections.Immutable;
using System.Text.Json;
using TmEssentials;
using UOTDBot.Models;

namespace UOTDBot;

public sealed class AppDbContext(DbContextOptions options) : DbContext(options)
{
    private static readonly ValueComparer<Dictionary<string, string>> dictionaryStringStringValueComparer = new(
        (x, y) => x!.Count == y!.Count && !x.Except(y).Any(),
        x => x.Aggregate(0, (current, item) => HashCode.Combine(current, item.Key.GetHashCode(), item.Value.GetHashCode())),
        x => new Dictionary<string, string>(x));

    private static readonly ValueComparer<List<ulong>> listUint64ValueComparer = new(
        (x, y) => x!.SequenceEqual(y!),
        x => x.Aggregate(0, (current, item) => HashCode.Combine(current, item.GetHashCode())),
        x => x.ToList());

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
            new Car { Id = "CarSport", DisplayName = "StadiumCar" },
            new Car { Id = "CarSnow", DisplayName = "SnowCar" },
            new Car { Id = "CarRally", DisplayName = "RallyCar" },
            new Car { Id = "CarDesert", DisplayName = "DesertCar" });

        modelBuilder.Entity<ReportConfiguration>()
            .Property(x => x.Emotes)
            .HasConversion(
                x => JsonSerializer.Serialize(x, AppJsonContext.Default.DictionaryStringString),
                x => string.IsNullOrEmpty(x) ? new() : JsonSerializer.Deserialize(x, AppJsonContext.Default.DictionaryStringString) ?? new(),
                dictionaryStringStringValueComparer)
            .HasMaxLength(byte.MaxValue);

        modelBuilder.Entity<ReportConfiguration>()
            .Property(x => x.PingRoles)
            .HasConversion(
                x => JsonSerializer.Serialize(x, AppJsonContext.Default.ListUInt64),
                x => string.IsNullOrEmpty(x) ? new() : JsonSerializer.Deserialize(x, AppJsonContext.Default.ListUInt64) ?? new(),
                listUint64ValueComparer)
            .HasMaxLength(byte.MaxValue);

        modelBuilder.Entity<Map>()
            .Property(x => x.Features)
            .HasConversion(
                x => JsonSerializer.Serialize(x, AppJsonContext.Default.MapFeatures),
                x => JsonSerializer.Deserialize(x, AppJsonContext.Default.MapFeatures)!)
            .HasMaxLength(byte.MaxValue);
    }
}

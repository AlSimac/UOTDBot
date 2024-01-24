using Microsoft.EntityFrameworkCore;
using UOTDBot.Models;

namespace UOTDBot;

internal sealed class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Map> Maps { get; set; }
}

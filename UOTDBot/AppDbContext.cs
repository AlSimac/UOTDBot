using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace UOTDBot;

internal sealed class AppDbContext(DbContextOptions options) : DbContext(options)
{
}

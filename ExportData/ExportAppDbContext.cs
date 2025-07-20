using Microsoft.EntityFrameworkCore;
using UOTDBot;

namespace ExportData;

public class ExportAppDbContext : AppDbContext
{
    private readonly string _connectionString;

    public ExportAppDbContext(string connectionString) : base(new DbContextOptions<ExportAppDbContext>())
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite(_connectionString);
}

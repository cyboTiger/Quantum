using Microsoft.EntityFrameworkCore;
using Quantum.Core.Models;

namespace Quantum.Core.Repository;

public class QuantumDbContext : DbContext
{
    public DbSet<Teacher> Teachers { get; set; } = null!;
    public DbSet<DatabaseConfig> Configs { get; set; } = null!;
    public DbSet<User> UserInfo { get; set; } = null!;
    public DbSet<Cookie> Cookies { get; set; } = null!;

    private readonly string _dbPath = Path.Combine(".", "data", "quantum.db");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dataDir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    public async Task ResetStateAsync()
    {
        UserInfo.RemoveRange(UserInfo);
        Cookies.RemoveRange(Cookies);
        await SaveChangesAsync();
    }
}

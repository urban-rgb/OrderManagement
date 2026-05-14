using Microsoft.EntityFrameworkCore;
using backend.Domain;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            modelBuilder.Entity<Order>().Ignore(o => o.Version);
        }
        else
        {
            modelBuilder.Entity<Order>()
                .Property(o => o.Version)
                .IsRowVersion();
        }
    }
}
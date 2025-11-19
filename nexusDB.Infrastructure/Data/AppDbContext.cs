using Microsoft.EntityFrameworkCore;
using nexusDB.Domain.Entities;

namespace nexusDB.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        :base(options)
    {
    }
    
    // Tables:
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de seeding para la tabla Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, SpecificRole = "User" },
            new Role { Id = 2, SpecificRole = "Admin" }
        );
    }
}

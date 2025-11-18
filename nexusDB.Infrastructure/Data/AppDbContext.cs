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
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion; // Necesario para ValueConverter
using nexusDB.Domain.Entities;
using nexusDB.Domain.VOs;

namespace nexusDB.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        :base(options)
    {
    }
    
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Instance> Instances { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Configuración de Value Objects ---
        modelBuilder.Entity<User>(builder =>
        {
            // Mapeo para Email Value Object
            // Usamos ValueConverter para que EF Core sepa cómo traducir Email a string y viceversa,
            // y cómo usarlo en consultas LINQ.
            builder.Property(u => u.Email)
                .HasConversion(new ValueConverter<Email, string>(
                    v => v.Value, // Convertir Email a string para la BD
                    v => Email.Create(v).Value! // Convertir string de la BD a Email
                ))
                .HasMaxLength(255); // Define un tamaño apropiado para la columna de email
            
            // Mapeo para Name Value Object
            builder.Property(u => u.Name)
                .HasConversion(new ValueConverter<PersonName, string>(
                    v => v.Value,
                    v => PersonName.Create(v).Value!
                ))
                .HasMaxLength(50);

            // Mapeo para LastName Value Object
            builder.Property(u => u.LastName)
                .HasConversion(new ValueConverter<PersonName, string>(
                    v => v.Value,
                    v => PersonName.Create(v).Value!
                ))
                .HasMaxLength(50);
            
            // Asegurarse de que el email sea único en la base de datos
            // El índice único se aplica a la columna subyacente (string)
            builder.HasIndex(u => u.Email).IsUnique();
        });

        // Configuración de seeding para la tabla Roles
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, SpecificRole = "User" },
            new Role { Id = 2, SpecificRole = "Admin" }
        );
    }
}

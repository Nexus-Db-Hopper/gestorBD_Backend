using System.Collections.Generic;

namespace nexusDB.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    
    // Se inicializa con null! para satisfacer al compilador de C# sobre nulabilidad.
    // Entity Framework Core se encargará de poblar esta propiedad al leer desde la base de datos.
    public string SpecificRole { get; set; } = null!;

    // Inverse relation
    public ICollection<User> Users { get; set; } = new List<User>();
}

namespace nexusDB.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string SpecificRole { get; set; }

    // Inverse relation
    public ICollection<User> Users { get; set; } = new List<User>();

}
namespace nexusDB.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public int IdRole { get; set; }
    
    //Relation 1:N
    public Role Role { get; set; }
    
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
}
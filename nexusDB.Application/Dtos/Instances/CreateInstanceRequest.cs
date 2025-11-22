namespace nexusDB.Application.Dtos.Instances;

public class CreateInstanceRequest
{
    public string Engine { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string UserPassword { get; set; } = string.Empty;
    public int OwnerUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public string ContainerName { get; set; } = string.Empty;
}

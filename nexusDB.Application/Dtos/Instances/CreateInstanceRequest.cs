namespace nexusDB.Application.Dtos.Instances;

public class CreateInstanceRequest
{
    public string Engine { get; set; }
    public string Name { get; set; }
    public string Username { get; set; }
    public string UserPassword { get; set; }
    public int OwnerUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public string ContainerName { get; set; }
}
namespace nexusDB.Domain.Entities;

public class Instance
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string UserPasswordEncrypted { get; set; } =string.Empty;
    public int CreatedByUserId { get; set; }
    public int OwnerUserId { get; set; }
    public string ContainerName { get; set; }
    public  InstanceState State { get; set; }
        
    public User? OwnerUser { get; set; }
    public User? CreatedByUser { get; set; }

    public Instance(string Name, string Engine,string username, string userPasswordEncrypted, int CreatedByUserId, int OwnerUserId, string containerName)
    {
        this.Id = Id;
        this.Name = Name;
        this.Engine = Engine;
        this.Username = username;
        this.UserPasswordEncrypted = userPasswordEncrypted;
        this.CreatedByUserId = CreatedByUserId;
        this.OwnerUserId = OwnerUserId;
        this.ContainerName = containerName;
    }
}

public enum InstanceState
{
    Active,
    Suspended,
    Deleted
}
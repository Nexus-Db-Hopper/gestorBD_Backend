namespace nexusDB.Domain.Entities;

public class Instance
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } =string.Empty;
    public int CreatedByUserId { get; set; }
    public int OwnerUserId { get; set; }
    public string ContainerId { get; set; }
    public  InstanceState State { get; set; }
        
    public User? OwnerUser { get; set; }
    public User? CreatedByUser { get; set; }

    public Instance(string Name, string Engine, string Port,string username, string PasswordEncrypted, int CreatedByUserId, int OwnerUserId)
    {
        this.Id = Id;
        this.Name = Name;
        this.Engine = Engine;
        this.Port = Port;
        this.Username = username;
        this.PasswordEncrypted = PasswordEncrypted;
        this.CreatedByUserId = CreatedByUserId;
        this.OwnerUserId = OwnerUserId;
        this.ContainerId = ContainerId;
    }
}

public enum InstanceState
{
    Running,
    Stopped
}
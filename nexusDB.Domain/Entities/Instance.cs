namespace nexusDB.Domain.Entities;

public class Instance
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Database Name
    public string Engine { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string UserPasswordEncrypted { get; set; } = string.Empty;
    public int CreatedByUserId { get; set; }
    public int OwnerUserId { get; set; }
    public string? ContainerName { get; set; }

    // --- New Properties for Docker ---
    public string? ContainerId { get; set; } // To store the ID Docker assigns to the container
    public int HostPort { get; set; } // The port on the host machine mapped to the container's port

    public InstanceState State { get; set; }

    public User? OwnerUser { get; set; }
    public User? CreatedByUser { get; set; }

    // Constructor for EF Core
    private Instance() { }

    public Instance(string name, string engine, string username, string userPasswordEncrypted, int createdByUserId, int ownerUserId, string? containerName)
    {
        Name = name;
        Engine = engine;
        Username = username;
        UserPasswordEncrypted = userPasswordEncrypted;
        CreatedByUserId = createdByUserId;
        OwnerUserId = ownerUserId;
        ContainerName = containerName;
        State = InstanceState.Suspended; // Start as suspended until container is running
    }
}

public enum InstanceState
{
    Active,
    Suspended,
    Deleted
}

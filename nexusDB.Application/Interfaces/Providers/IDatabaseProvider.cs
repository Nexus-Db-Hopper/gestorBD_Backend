using nexusDB.Domain.Entities;

namespace nexusDB.Application.Interfaces.Providers;

public interface IDatabaseProvider
{
    string Engine { get; } 

    Task<string> CreateContainerAsync(Instance instance, string password);
    Task StartAsync(Instance instance);
    Task StopAsync(Instance instance);
    Task<string> ExecuteQueryAsync(Instance instance, string query);
}
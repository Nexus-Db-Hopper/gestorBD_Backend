using nexusDB.Application.Dtos.Instances;
using nexusDB.Domain.Entities;

namespace nexusDB.Application.Interfaces.Providers;

public interface IDatabaseProvider
{
    string Engine { get; } 

    Task<string> CreateContainerAsync(Instance instance, string password, string rootPassword);
    Task StartAsync(Instance instance);
    Task StopAsync(Instance instance);
    Task<QueryResultDto> ExecuteQueryAsync(Instance instance, string query, string decryptedPassword);
}
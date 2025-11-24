using nexusDB.Application.Dtos.Instances;

namespace nexusDB.Application.Interfaces.Instances;

public interface IInstanceService
{
    Task<int> CreateInstanceAsync(CreateInstanceRequest request);
    Task<QueryResultDto> ExecuteQueryAsync(QueryRequestDto queryRequest);
    public Task StartInstanceAsync(int ownerId);
    public Task StopInstanceAsync(int ownerId);


}
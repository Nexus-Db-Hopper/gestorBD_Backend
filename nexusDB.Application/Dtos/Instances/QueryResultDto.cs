namespace nexusDB.Application.Dtos.Instances;

public class QueryResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<IDictionary<string, object?>>? Data { get; set; } = new List<IDictionary<string, object?>>();
}
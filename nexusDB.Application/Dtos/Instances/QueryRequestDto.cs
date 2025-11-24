namespace nexusDB.Application.Dtos.Instances;

public class QueryRequestDto
{
    public int OwnerUserId { get; set; }
    public string Query { get; set; } = string.Empty;
}
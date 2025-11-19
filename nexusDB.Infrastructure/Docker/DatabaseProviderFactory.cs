using nexusDB.Application.Interfaces.Providers;
using nexusDB.Application.Interfaces.Repositories;


namespace nexusDB.Domain.Docker;

public class DatabaseProviderFactory : IDatabaseProviderFactory
{
    private readonly Dictionary<string, IDatabaseProvider> _providers;

    public DatabaseProviderFactory(IEnumerable<IDatabaseProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Engine);
    }

    public IDatabaseProvider GetProvider(string engine)
    {
        if (!_providers.ContainsKey(engine))
            throw new Exception($"Engine '{engine}' is not supported.");

        return _providers[engine];
    }
}
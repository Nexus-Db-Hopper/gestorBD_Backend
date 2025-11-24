using nexusDB.Application.Interfaces.Providers;
using nexusDB.Application.Interfaces.Repositories;


namespace nexusDB.Domain.Docker;

public class DatabaseProviderFactory : IDatabaseProviderFactory
{
    private readonly Dictionary<string, IDatabaseProvider> _providers;

    // Aqui se toman todos los servicios disponibles para saber a donde mandar al usuario
    public DatabaseProviderFactory(IEnumerable<IDatabaseProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Engine);
    }

    public IDatabaseProvider GetProvider(string engine)
    {
        
       
        // aqui te devuelve los proveedores del servicio en especifico
        return _providers[engine];
    }
}
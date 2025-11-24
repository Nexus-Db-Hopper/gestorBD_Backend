using nexusDB.Application.Interfaces.Providers;

namespace nexusDB.Application.Interfaces.Repositories;

public interface IDatabaseProviderFactory
{
    IDatabaseProvider GetProvider(string engine);

}
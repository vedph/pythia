using Pythia.Core.Config;

namespace Pythia.Api.Services
{
    public interface IQueryPythiaFactoryProvider
    {
        PythiaFactory GetFactory();
    }
}
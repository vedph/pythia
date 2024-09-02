using Pythia.Core.Config;

namespace Pythia.Web.Shared.Services;

public interface IQueryPythiaFactoryProvider
{
    PythiaFactory GetFactory();
}
using Pythia.Core.Config;

namespace Pythia.Web.Shared.Services;

public interface IPythiaFactoryProvider
{
    PythiaFactory GetFactory(string profile);
}

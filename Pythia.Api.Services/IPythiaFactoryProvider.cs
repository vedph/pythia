using Pythia.Core.Config;

namespace Pythia.Api.Services
{
    public interface IPythiaFactoryProvider
    {
        PythiaFactory GetFactory(string profile);
    }
}
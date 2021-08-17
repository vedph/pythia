using System.Threading.Tasks;

namespace Pythia.Cli.Commands
{
    public interface ICommand
    {
        Task<int> Run();
    }
}

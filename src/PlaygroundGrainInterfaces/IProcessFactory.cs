using System.Linq;
using System.Threading.Tasks;

namespace GrainInterfaces;

public enum ProcessType
{
    Build,
    Test
}

public interface IProcess
{
    Task ExecuteAsync();
    ProcessType ProcessType { get; }
}

public interface IProcessFactory
{
    IProcess? Get(ProcessType processType);
}
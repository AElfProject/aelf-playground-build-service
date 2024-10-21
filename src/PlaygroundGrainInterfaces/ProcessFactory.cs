using System.Linq;
using System.Threading.Tasks;

using GrainInterfaces;

namespace GrainInterfaces;

public class ProcessFactory : IProcessFactory
{
    private readonly IProcess[] _processes;

    public ProcessFactory(IProcess[] processes)
    {
        _processes = processes;
    }

    public IProcess? Get(ProcessType processType)
    {
        return _processes.FirstOrDefault(p => p.ProcessType == processType);
    }
}

public class BuildProcess : IProcess
{
    public async Task ExecuteAsync()
    {
        //do build
        await Task.Delay(1000);
    }

    public ProcessType ProcessType { get; } = ProcessType.Build;
}

public class TestProcess : IProcess
{
    public async Task ExecuteAsync()
    {
        //to test
        await Task.Delay(1000);
    }

    public ProcessType ProcessType { get; } = ProcessType.Test;
}
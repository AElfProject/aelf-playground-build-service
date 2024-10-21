using GrainInterfaces;

// https://github.com/dotnet/orleans/issues/6389#issuecomment-597567547
namespace Grains;
public class ProcessGrain : Grain, IProcessGrain
{
    private readonly IProcessFactory _processFactory;
    private Task? _myLongRunningTask;
    private CancellationTokenSource _cancellation = new CancellationTokenSource();

    public ProcessGrain(IProcessFactory processFactory)
    {
        _processFactory = processFactory;
    }

    public Task StartAsync(ProcessType processType)
    {
        // option 1: let it run in the current grain activation scheduler
        // _myLongRunningTask = Task.Factory.StartNew(_ => DoLongRunningWorkAsync(), null, _cancellation.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Current).Unwrap();

        // option 2: make it run on the thread pool scheduler
        _myLongRunningTask = Task.Run(() => _processFactory.Get(processType).ExecuteAsync(), _cancellation.Token);

        return Task.CompletedTask;
    }

    public Task<TaskStatus?> GetStatusAsync()
    {
        return Task.FromResult(_myLongRunningTask?.Status);
    }

    public Task StopAsync()
    {
        _cancellation.Cancel();
        return Task.CompletedTask;
    }
}
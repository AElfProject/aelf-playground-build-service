using Orleans.Concurrency;
using GrainInterfaces;

// https://github.com/dotnet/orleans/issues/6389#issuecomment-597567547
namespace Grains;

[StatelessWorker]
public class ProcessGrain<TRequest, TResponse> : Grain, IProcessGrain<TRequest, TResponse>
{
    protected Task<TResponse?> _myLongRunningTask;
    protected TRequest _request;
    protected CancellationTokenSource _cancellation = new CancellationTokenSource();

    public Task StartAsync(TRequest request)
    {
        _request = request;
        // option 1: let it run in the current grain activation scheduler
        // _myLongRunningTask = Task.Factory.StartNew(_ => DoLongRunningWorkAsync(), null, _cancellation.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Current).Unwrap();

        // option 2: make it run on the thread pool scheduler
        _myLongRunningTask = Task.Run(() => DoLongRunningWorkAsync(), _cancellation.Token);

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

    protected virtual async Task<TResponse?> DoLongRunningWorkAsync()
    {
        try
        {
            while (!_cancellation.Token.IsCancellationRequested)
            {
                // do some work
                await Task.Delay(5000);
                StopAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // cleanup
        }

        return default;
    }

    public Task<TResponse?> GetResultAsync()
    {
        return _myLongRunningTask;
    }
}
namespace GrainInterfaces;

public interface IProcessGrain<TRequest, TResponse> : IGrainWithGuidKey
{
    Task StartAsync(TRequest request);
    Task<TaskStatus?> GetStatusAsync();
    Task StopAsync();
    Task<TResponse?> GetResultAsync();
}
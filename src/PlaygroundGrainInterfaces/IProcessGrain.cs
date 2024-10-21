namespace GrainInterfaces;

public interface IProcessGrain : IGrainWithGuidKey
{
    Task StartAsync();
    Task<TaskStatus?> GetStatusAsync();
    Task StopAsync();
}
namespace GrainInterfaces;

public interface IProcessGrain : IGrainWithGuidKey
{
    Task StartAsync(ProcessType processType);
    Task<TaskStatus?> GetStatusAsync();
    Task StopAsync();
}

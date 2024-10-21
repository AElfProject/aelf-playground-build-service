namespace Client.Services;

public interface IVirusScanService
{
    Task<bool> IsFileInfected(byte[] file);
}
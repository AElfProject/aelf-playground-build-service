using ClamAV.Net.Client;
using ClamAV.Net.Client.Results;

namespace Client.Services;

public class VirusScanService : IVirusScanService
{
    private readonly IConfiguration _configuration;
    private readonly IClamAvClient _clamAvClient;
    private readonly ILogger<VirusScanService> _logger;

    public VirusScanService(IConfiguration configuration, ILogger<VirusScanService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _clamAvClient = ClamAvClient.Create(new Uri(_configuration.GetSection("ClamAV:ConnectionString").Value));
    }

    public async Task<bool> IsFileInfected(byte[] file)
    {
        using var stream = new MemoryStream(file);
        ScanResult res = await _clamAvClient.ScanDataAsync(stream).ConfigureAwait(false);

        _logger.LogInformation($"Scan result : Infected - {res.Infected} , Virus name {res.VirusName}");

        return !res.Infected;
    }
}
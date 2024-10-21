using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;
using GrainUtilities;

namespace Grains;

public class TemplateGrain : ProcessGrain<TemplateRequestDto, TemplateResponseDto>, ITemplateGrain
{
    private string _tempFolder;
    protected override async Task<TemplateResponseDto?> DoLongRunningWorkAsync()
    {
        try
        {
            while (!_cancellation.Token.IsCancellationRequested)
            {
                var request = _request;
                _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(_tempFolder);

                // inside the temp folder, run the command "dotnet new aelf -n <templateName>"
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"new {request.Template} -n {request.TemplateName}",
                        WorkingDirectory = _tempFolder,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return new TemplateResponseDto
                    {
                        Status = false,
                        Message = "Template creation failed"
                    };
                }

                var zipBytes = ZipUtilities.ZipFolderToByteArray(_tempFolder);

                return new TemplateResponseDto
                {
                    Status = true,
                    Message = "Template created",
                    ZipFile = zipBytes
                };

                StopAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // cleanup
        }
        finally
        {
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }
        }

        return new TemplateResponseDto
        {
            Status = false,
            Message = "Template failed"
        };
    }
}


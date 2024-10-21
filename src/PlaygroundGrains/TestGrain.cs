using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;

namespace Grains;

public class TestGrain : ProcessGrain<TestRequestDto, TestResponseDto>, ITestGrain
{
    private string _tempFolder;
    protected override async Task<TestResponseDto?> DoLongRunningWorkAsync()
    {
        try
        {
            while (!_cancellation.Token.IsCancellationRequested)
            {
                var request = _request;
                _tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(_tempFolder);

                await using var zipStream = new MemoryStream(request.ZipFile);
                using var archive = new ZipArchive(zipStream);
                archive.ExtractToDirectory(_tempFolder);

                // find the first .csproj file that is a .Tests.csproj
                var projectFile = Directory.GetFiles(_tempFolder, "*.csproj", SearchOption.AllDirectories)
                    .FirstOrDefault(file => file.Contains(".Tests.csproj"));

                if (projectFile == null)
                {
                    return new TestResponseDto
                    {
                        Status = false,
                        Message = "No project file found"
                    };
                }

                var projectFolder = Path.GetDirectoryName(projectFile);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "test",
                        WorkingDirectory = projectFolder,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                return new TestResponseDto
                {
                    Status = false,
                    Message = process.StandardOutput.ReadToEnd()
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

        return new TestResponseDto
        {
            Status = false,
            Message = "Test failed"
        };
    }
}


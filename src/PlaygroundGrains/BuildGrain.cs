using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;

namespace Grains;

public class BuildGrain : ProcessGrain<BuildRequestDto, BuildResponseDto>, IBuildGrain
{
    private string _tempFolder;
    protected override async Task<BuildResponseDto?> DoLongRunningWorkAsync()
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

                // find the first .csproj file that is not a .Tests.csproj
                var projectFile = Directory.GetFiles(_tempFolder, "*.csproj", SearchOption.AllDirectories)
                    .FirstOrDefault(file => !file.Contains(".Tests.csproj"));

                if (projectFile == null)
                {
                    return new BuildResponseDto
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
                        Arguments = "build -p:RunAnalyzers=false",
                        WorkingDirectory = projectFolder,
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
                    // return the stdout in message
                    return new BuildResponseDto
                    {
                        Status = false,
                        Message = process.StandardOutput.ReadToEnd()
                    };
                }

                // search for the first .dll file
                var dllFile = Directory.GetFiles(projectFolder, "*.dll", SearchOption.AllDirectories).FirstOrDefault();

                if (dllFile == null)
                {
                    return new BuildResponseDto
                    {
                        Status = false,
                        Message = "No dll file found"
                    };
                }

                // get the base64 string of the dll file
                var bytes = await File.ReadAllBytesAsync(dllFile);
                var base64 = Convert.ToBase64String(bytes);

                // return the base64 string of the dll file
                return new BuildResponseDto
                {
                    Status = true,
                    Message = base64
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

        return new BuildResponseDto
        {
            Status = false,
            Message = "Build failed"
        };
    }
}


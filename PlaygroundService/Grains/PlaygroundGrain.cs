using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Orleans;

namespace PlaygroundService.Grains;

public class PlaygroundGrain : Grain, IPlaygroundGrain
{
    private readonly ILogger<PlaygroundGrain> _logger;

    public PlaygroundGrain(
        ILogger<PlaygroundGrain> logger)
    {
        _logger = logger;
    }

    public async Task<(bool, string)> BuildProject(string directory)
    {
        string projectDirectory = directory;
        try
        {
            // Check if directory exists
            if (!Directory.Exists(directory))
            {
                return (false, "Directory does not exist: " + directory);
            }

            // Get all files in the directory
            string[] files;
            try
            {
                files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                return (false, "Error getting files from directory: " + e.Message);
            }

            // Print all files in the directory
            foreach (var file in files)
            {
                _logger.LogInformation("file name uploaded is: " + file);
            }

            // Create ProcessStartInfo
            ProcessStartInfo psi;

            var directoryTree = PrintDirectoryTree(directory);
            Console.WriteLine("files in extracted path");
            Console.WriteLine(directoryTree);
            
            // before running the process dotnet build check if the directory has a .csproj file and .sln file
            var csprojFiles = files.Where(file => file.EndsWith(".csproj")).ToList();
            var slnFiles = files.Where(file => file.EndsWith(".sln")).ToList();
            
            if (csprojFiles.Count == 0)
            {
                return (false, "No .csproj file found in the directory");
            }
            
            // if (slnFiles.Count == 0)
            // {
            //     return (false, "No .sln file found in the directory");
            // }
            
            try
            {
                // Check if directory exists
                if (!Directory.Exists(directory))
                {
                    return (false, "Directory does not exist: " + directory);
                }

                // Get the first subdirectory
                var subdirectory = Directory.GetDirectories(directory).FirstOrDefault();
                if (subdirectory == null)
                {
                    return (false, "No subdirectories found in the directory");
                }

                // Use the subdirectory as the project directory
                projectDirectory = subdirectory;
                psi = new ProcessStartInfo("dotnet", "build " + projectDirectory)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
            }
            catch (Exception e)
            {
                return (false, "Error creating ProcessStartInfo: " + e.Message);
            }

            // Run psi
            Process proc;
            try
            {
                proc = Process.Start(psi);
                if (proc == null)
                {
                    return (false, "Process could not be started.");
                }

                proc.WaitForExit();

                string errorMessage;
                using (var sr = proc.StandardError)
                {
                    errorMessage = await sr.ReadToEndAsync();
                }

                if (string.IsNullOrEmpty(errorMessage))
                {
                    using (var sr = proc.StandardOutput)
                    {
                        errorMessage = await sr.ReadToEndAsync();
                    }
                }

                if (proc.ExitCode != 0)
                {
                    _logger.LogError("Error executing process: " + errorMessage);
                    return (false, "Error executing process: " + errorMessage);
                }
            }
            catch (Exception e)
            {
                return (false, "Error starting process: " + e.Message);
            }
            
            // as the build is successful. lookup for the dll file in the bin folder 
            // dll file will be under one of the subdirectories of the projectDirectory
            var binDirectory = Path.Combine(projectDirectory, "bin");
            var dllFiles = Directory.GetFiles(binDirectory, "*.dll.patched", SearchOption.AllDirectories);
            
            //print dll file name
            foreach (var dllFile in dllFiles)
            {
                _logger.LogInformation("dll file name is: " + dllFile);
            }
            
            if (dllFiles.Length == 0)
            {
                return (false, "No .dll file found in the bin directory");
            }
            
            //extract the first dll file entry and return it in response
            var dllFileName = Path.GetFileName(dllFiles[0]);
            
            _logger.LogInformation("dll file name is: " + dllFileName);

            return (true, dllFiles[0]);
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
            return (false, e.Message);
        }
    }
    
    private string PrintDirectoryTree(string directoryPath)
    {
        var indent = new string(' ', 4);
        var directoryInfo = new DirectoryInfo(directoryPath);
        var stringBuilder = new StringBuilder();

        PrintDirectory(directoryInfo, string.Empty, indent, stringBuilder);

        return stringBuilder.ToString();
    }

    private void PrintDirectory(DirectoryInfo directoryInfo, string prefix, string indent, StringBuilder stringBuilder)
    {
        var isLast = directoryInfo.Parent.GetDirectories().Last().Equals(directoryInfo);

        stringBuilder.AppendLine($"{prefix}{(isLast ? "└── " : "├── ")}{directoryInfo.Name}");

        var newPrefix = prefix + (isLast ? "    " : "│   ");

        foreach (var fileInfo in directoryInfo.GetFiles())
        {
            isLast = fileInfo.Directory.GetFiles().Last().Equals(fileInfo);
            stringBuilder.AppendLine($"{newPrefix}{(isLast ? "└── " : "├── ")}{fileInfo.Name}");
        }

        foreach (var subDirectoryInfo in directoryInfo.GetDirectories())
        {
            PrintDirectory(subDirectoryInfo, newPrefix, indent, stringBuilder);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;
using PlaygroundService.Grains;

namespace PlaygroundService.Controllers
{
    [ApiController]
    [Route("playground")]
    public class PlaygroundController : ControllerBase
    {
        private readonly IClusterClient _client;
        private readonly ILogger<PlaygroundController> _logger;

        public PlaygroundController(IClusterClient client, ILogger<PlaygroundController> logger)
        {
            _client = client;
            _logger = logger;
        }
        
        [HttpGet("templates")]
        public async Task<IActionResult> GetTemplateConfig()
        {
            _logger.LogInformation("templates  - GetTemplateConfig started time: "+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            List<string> myList = new List<string> { "item1", "item2", "item3" };

            return Ok(myList); 
        }
        
        [HttpGet("templateInfo")]
        public async Task<IActionResult> GetTemplateInfo([FromQuery] string template, [FromQuery] string templateName)
        {
            _logger.LogInformation("templates  - GetTemplateInfo started time: "+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>("userId");
            var zipFilePath = await codeGeneratorGrain.GenerateZip(template, templateName);
            // var res = Content(Convert.ToBase64String(Read(zipFilePath)));
            return Content(zipFilePath);
            // var stream = System.IO.File.OpenRead(zipFilePath);
            // return File(stream, "application/zip", Path.GetFileName(zipFilePath));
        }

        [HttpPost("build")]
        public async Task<IActionResult> Build(IFormFile contractFiles)
        {
            _logger.LogInformation("Build  - Build started time: "+ DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            return await BuildService(contractFiles);
        }
        
        public byte[] Read(string path)
        {
            try
            {
                byte[] code = System.IO.File.ReadAllBytes(path);
                return code;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public async Task<IActionResult> BuildService(IFormFile contractFiles)
        {
            _logger.LogInformation("PlaygroundController - Build method started for: "+ contractFiles.FileName + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")) ;
            
            var tempPath = Path.GetTempPath();
            var zipPath = Path.Combine(tempPath, contractFiles.FileName);
            
            _logger.LogInformation("PlaygroundController - Zip file path: " + zipPath);

            await using var zipStream = new FileStream(zipPath, FileMode.Create);
            await contractFiles.CopyToAsync(zipStream);
            await zipStream.FlushAsync(); // Ensure all data is written to the file
            
            _logger.LogInformation("PlaygroundController - Zip file saved to disk"  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                // If we get here, the file is a valid zip file
            }
            catch (InvalidDataException)
            {
                _logger.LogError("PlaygroundController - The uploaded file is not a valid zip file"  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                
                // The file is not a valid zip file
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "PlaygroundController - The uploaded file is not a valid zip file"
                });
            }
            
            var extractPath = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(contractFiles.FileName), Guid.NewGuid().ToString());

            _logger.LogInformation("PlaygroundController - ExtractPath or destination directory where files are extracted is: "+extractPath  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    var destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                    // Ensure the destination file path is within the destination directory
                    if (!destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                    {
                        _logger.LogError("PlaygroundController - Invalid entry in the zip file: " + entry.FullName  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                        return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                        {
                            Success = false,
                            Message = $"PlaygroundController - Invalid entry in the zip file: {entry.FullName}"
                        });
                    }

                    // Create the directory for the file if it does not exist
                    var destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }
                    // Extract the entry to the destination path
                    try
                    {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }
                    catch(UnauthorizedAccessException ex)
                    {
                        _logger.LogError("PlaygroundController - build ex1:  "+ex.ToString()  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("PlaygroundController - build ex: : "+ex.ToString()  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    }
                }
            }
            
            _logger.LogInformation("PlaygroundController - Files extracted to disk"  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            
            //validate if the extracted path contain .csProj file
            var csprojFiles = Directory.GetFiles(extractPath, "*.csproj", SearchOption.AllDirectories);
            if (csprojFiles.Length == 0)
            {
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = false,
                    Message = "PlaygroundController - No .csproj file found in the uploaded zip file"
                });
            }
            
            var codeGeneratorGrain = _client.GetGrain<IPlaygroundGrain>("userId");
            var (success, message) = await codeGeneratorGrain.BuildProject(extractPath);

            if (success)
            {
                _logger.LogInformation("PlaygroundController - BuildProject method returned success: " + message  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                var pathToDll = message;
                var fileName = Path.GetFileName(pathToDll);
                _logger.LogInformation("PlaygroundController - Files return fileName:" + pathToDll);
                if (!System.IO.File.Exists(pathToDll))
                {
                    _logger.LogError("PlaygroundController - BuildProject method returned error: file not exist " + message  + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                    {
                        Success = success,
                        Message = message
                    });
                }
                var res = Content(Convert.ToBase64String(Read(pathToDll)));
                
                _logger.LogInformation("PlaygroundController - BuildProject method over: " + " time:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                await codeGeneratorGrain.DelData(zipPath, extractPath);

                return res;
                // return File(Read(pathToDll), "application/octet-stream");

                // var memoryStream = new MemoryStream();
                // using (var stream = new FileStream(pathToDll, FileMode.Open))
                // {
                //     await stream.CopyToAsync(memoryStream);
                // }
                // memoryStream.Position = 0;
                // return File(memoryStream, "application/octet-stream");
                // return PhysicalFile(pathToDll, "application/octet-stream", fileName);
            }
            else
            {
                _logger.LogError("PlaygroundController - BuildProject method returned error: " + message);
                return BadRequest(new PlaygroundSchema.PlaygroundContractGenerateResponse
                {
                    Success = success,
                    Message = message
                });
            }
        }
    }
}
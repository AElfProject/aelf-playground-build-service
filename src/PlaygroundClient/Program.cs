using Microsoft.Extensions.Hosting;
using Client.Services;
using Microsoft.AspNetCore.Mvc;
using GrainInterfaces;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);
builder.Host
    .UseOrleansClient(client =>
    {
        client.UseLocalhostClustering();
    })
    .ConfigureLogging(logging => logging.AddConsole());

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IVirusScanService, VirusScanService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// upload zip file and return the extracted files
app.MapPost("/playground/build", async ([FromServices] IClusterClient _client, IFormFile file) =>
{
    Console.WriteLine("Build started");

    var grain = _client.GetGrain<IBuildGrain>(Guid.Empty); // stateless worker: https://learn.microsoft.com/en-us/dotnet/orleans/grains/stateless-worker-grains
    BuildRequestDto request = new BuildRequestDto
    {
        ZipFile = await file.GetBytesAsync()
    };

    await grain.StartAsync(request);

    var status = await grain.GetStatusAsync();

    while (status != TaskStatus.RanToCompletion)
    {
        await Task.Delay(1000);
        status = await grain.GetStatusAsync();
    }

    Console.WriteLine("Build completed");

    var result = await grain.GetResultAsync();

    return result;

})
.DisableAntiforgery();

app.MapPost("/playground/test", async ([FromServices] IClusterClient _client, IFormFile file) =>
{
    Console.WriteLine("Test started");

    var grain = _client.GetGrain<ITestGrain>(Guid.Empty); // stateless worker: https://learn.microsoft.com/en-us/dotnet/orleans/grains/stateless-worker-grains
    TestRequestDto request = new TestRequestDto
    {
        ZipFile = await file.GetBytesAsync()
    };

    await grain.StartAsync(request);

    var status = await grain.GetStatusAsync();

    while (status != TaskStatus.RanToCompletion)
    {
        await Task.Delay(1000);
        status = await grain.GetStatusAsync();
    }

    Console.WriteLine("Test completed");

    var result = await grain.GetResultAsync();

    return result;
})
.DisableAntiforgery();

app.MapGet("/playground/templates", () =>
{
    return new List<string> { "aelf", "aelf-lottery", "aelf-nft-sale", "aelf-simple-dao" };
});

app.MapGet("/playground/template", async ([FromServices] IClusterClient _client, string template, string templateName) =>
{
    Console.WriteLine("Template started");

    var grain = _client.GetGrain<ITemplateGrain>(Guid.Empty); // stateless worker: https://learn.microsoft.com/en-us/dotnet/orleans/grains/stateless-worker-grains
    TemplateRequestDto request = new TemplateRequestDto
    {
        Template = template,
        TemplateName = templateName
    };

    await grain.StartAsync(request);

    var status = await grain.GetStatusAsync();

    while (status != TaskStatus.RanToCompletion)
    {
        await Task.Delay(1000);
        status = await grain.GetStatusAsync();
    }

    Console.WriteLine("Template completed");

    var result = await grain.GetResultAsync();

    return result;
});

app.MapPost("/playground/share/create", ([FromServices] IClusterClient _client, IFormFile file) =>
{
    // TODO
})
.DisableAntiforgery();

app.Run();

public static class FormFileExtensions
{
    public static async Task<byte[]> GetBytesAsync(this IFormFile formFile)
    {
        await using var memoryStream = new MemoryStream();
        await formFile.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }
}

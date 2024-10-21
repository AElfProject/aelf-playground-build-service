using Microsoft.Extensions.Hosting;
using Client.Services;
using Microsoft.AspNetCore.Mvc;
using GrainInterfaces;

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
    // TODO
    var friend = _client.GetGrain<IHelloGrain>("friend");
    var result = await friend.SayHello("Good morning!");
    Console.WriteLine($"""

        {result}

        """);

})
.DisableAntiforgery();

app.MapPost("/playground/test", async ([FromServices] IClusterClient _client, IFormFile file) =>
{
    // TODO
})
.DisableAntiforgery();

app.MapGet("/playground/templates", () =>
{
    return new List<string> { "aelf", "aelf-lottery", "aelf-nft-sale", "aelf-simple-dao" };
});

app.MapGet("/playground/template", async ([FromServices] IClusterClient _client, string template, string templateName) =>
{
    // TODO
});

app.MapPost("/playground/share/create", async ([FromServices] IVirusScanService _virusScanService, IFormFile file) =>
{
    // get virus scan service
    var result = await _virusScanService.IsFileInfected(await file.GetBytesAsync());
    if (!result)
    {
        return "File is infected";
    }

    // TODO: save to storage

    return "File is clean";
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

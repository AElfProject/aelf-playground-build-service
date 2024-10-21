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
    var grain = _client.GetGrain<IProcessGrain>(Guid.NewGuid());
    await grain.StartAsync();

    var status = await grain.GetStatusAsync();

    while (status != TaskStatus.RanToCompletion)
    {
        await Task.Delay(1000);
        status = await grain.GetStatusAsync();
    }

    return "Build completed";

})
.DisableAntiforgery();

app.MapPost("/playground/test", ([FromServices] IClusterClient _client, IFormFile file) =>
{
    // TODO
})
.DisableAntiforgery();

app.MapGet("/playground/templates", () =>
{
    return new List<string> { "aelf", "aelf-lottery", "aelf-nft-sale", "aelf-simple-dao" };
});

app.MapGet("/playground/template", ([FromServices] IClusterClient _client, string template, string templateName) =>
{
    // TODO
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

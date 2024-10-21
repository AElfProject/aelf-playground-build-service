namespace GrainInterfaces;

public interface ITemplateGrain : IProcessGrain<TemplateRequestDto, TemplateResponseDto>
{
}

[GenerateSerializer]
public sealed class TemplateRequestDto
{
    [Id(0)]
    public string Template { get; set; }
    [Id(1)]
    public string TemplateName { get; set; }
}

[GenerateSerializer]
public sealed class TemplateResponseDto
{
    [Id(0)]
    public bool Status { get; set; }
    [Id(1)]
    public string Message { get; set; }
    [Id(2), Immutable]
    public byte[]? ZipFile { get; set; }
}
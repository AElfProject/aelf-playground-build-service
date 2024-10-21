namespace GrainInterfaces;

public interface IBuildGrain : IProcessGrain<BuildRequestDto, BuildResponseDto>
{
}

[GenerateSerializer]
public sealed class BuildRequestDto
{
    [Id(0), Immutable]
    public byte[] ZipFile { get; set; }
}

[GenerateSerializer]
public sealed class BuildResponseDto
{
    [Id(0)]
    public bool Status { get; set; }
    [Id(1)]
    public string Message { get; set; }
}
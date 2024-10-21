namespace GrainInterfaces;

public interface IBuildGrain : IProcessGrain<BuildRequestDto, BuildResponseDto>
{
}

[GenerateSerializer]
public class BuildRequestDto
{
    [Id(0)]
    public byte[] ZipFile { get; set; }
}

[GenerateSerializer]
public class BuildResponseDto
{
    [Id(0)]
    public bool Status { get; set; }
    [Id(1)]
    public string Message { get; set; }
}
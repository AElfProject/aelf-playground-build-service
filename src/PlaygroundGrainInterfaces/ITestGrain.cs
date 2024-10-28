namespace GrainInterfaces;

public interface ITestGrain : IProcessGrain<TestRequestDto, TestResponseDto>
{
}

[GenerateSerializer]
public sealed class TestRequestDto
{
    [Id(0), Immutable]
    public byte[] ZipFile { get; set; }
}

[GenerateSerializer]
public sealed class TestResponseDto
{
    [Id(0)]
    public bool Status { get; set; }
    [Id(1)]
    public string Message { get; set; }
}
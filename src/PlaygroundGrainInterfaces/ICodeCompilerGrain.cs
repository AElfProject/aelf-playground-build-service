namespace GrainInterfaces;

public interface ICodeCompilerGrain : IGrainWithStringKey
{
    Task<string> CompileCSharpCode(string sourceCode);
}
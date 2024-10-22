namespace GrainInterfaces;

public interface ICodeCompilerGrain : IGrainWithStringKey
{
    ValueTask<string> CompileCSharpCode(string sourceCode);
}
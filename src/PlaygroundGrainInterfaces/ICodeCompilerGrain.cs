using System.Threading.Tasks;
using Orleans;

public interface ICodeCompilerGrain : IGrainWithStringKey
{
    Task<string> CompileCSharpCode(string sourceCode);
}
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Orleans;

public class CodeCompilerGrain : Grain, ICodeCompilerGrain
{
    public async Task<string> CompileCSharpCode(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        var assemblyName = Path.GetRandomFileName();
        var references = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => !a.IsDynamic)
                            .Select(a => MetadataReference.CreateFromFile(a.Location))
                            .Cast<MetadataReference>();

        var compilation = CSharpCompilation.Create(assemblyName,
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using (var ms = new MemoryStream())
        {
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

                var errors = new StringBuilder();
                foreach (var diagnostic in failures)
                {
                    errors.AppendLine($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }

                throw new InvalidOperationException($"Compilation failed: {errors}");
            }

            ms.Seek(0, SeekOrigin.Begin);
            var rawAssembly = ms.ToArray();
            return Convert.ToBase64String(rawAssembly);
        }
    }
}
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Scripting.Analyzers;

var dependenciesLoadContext = new AssemblyLoadContext("ScriptDependencies", false);
var assemblyLoadContext = new AssemblyLoadContext("Scripts", true);


var systemRuntime = dependenciesLoadContext.LoadFromAssemblyName(new AssemblyName("System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"));


IReadOnlyCollection<MetadataReference> References = ImmutableArray
    .Create(
        MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Environment).GetTypeInfo().Assembly.Location),
        MetadataReference
            .CreateFromFile(systemRuntime.Location));

var parseOptions = CSharpParseOptions
       .Default
       .WithLanguageVersion(LanguageVersion.Latest)
       .WithKind(SourceCodeKind.Script);

var compilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication)
       .WithOptimizationLevel(OptimizationLevel.Release)
       .WithNullableContextOptions(NullableContextOptions.Enable)
       .WithUsings("System")
       .WithAllowUnsafe(false)
       .WithPlatform(Platform.AnyCpu);

var analyzer = new RestrictionAnalyzer();



var builder = new StringBuilder();

await CompileAndRun(@"

Console.WriteLine(123);
System.Environment.Exit(-5);
System.Console.WriteLine(321);

");
return;


while (true)
{
    var text = Console.ReadLine().Trim();

    if (!text.Contains("!run"))
    {
        builder.AppendLine(text);
        continue;
    }



    await CompileAndRun(builder.ToString());
    builder.Clear();
    Console.ReadKey();
}

async Task CompileAndRun(string code)
{
    var scriptSyntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);
    Console.WriteLine("\n\n\t\t<====[Compile candidate]====>");
    Console.WriteLine(code);
    Console.WriteLine("\n\n\t\t<====[Compiling]====>");

    // compile code
    var compilation = CSharpCompilation
        .Create(Guid.NewGuid().ToString(), options: compilationOptions, references: References)
        .AddSyntaxTrees(scriptSyntaxTree)
        .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));

    var diagnostics = await compilation.GetAllDiagnosticsAsync();
    if (diagnostics.Any())
    {
        Console.WriteLine("\n\n\t\t<====[Errors]====>");

        foreach (var item in diagnostics)
        {
            Console.WriteLine(item);
        }
        return;
    }



    using var memoryStream = new MemoryStream();
    var emitResult = compilation.Compilation.Emit(memoryStream);

    if (!emitResult.Success)
    {
        Console.WriteLine("\n\n\t\t<====[Emit errors]====>");
        foreach (var item in emitResult.Diagnostics)
        {
            Console.WriteLine(item);
        }
        return;
    }

    memoryStream.Seek(0, SeekOrigin.Begin);
    Console.WriteLine("\n\n\t\t<====[Executing code]====>");

    var assembly = assemblyLoadContext.LoadFromStream(memoryStream);

    var type = assembly.GetType("Script");
    var main = type.GetMethod("<Main>", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    var result = main.Invoke(null, null);
    Console.WriteLine("\n\n\t\t<====[Script done]====>");
}


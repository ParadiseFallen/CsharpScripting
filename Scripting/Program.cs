using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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



var builder = new StringBuilder();

while (true)
{
    var text = Console.ReadLine().Trim();

    if (!text.Contains("!run"))
    {
        builder.AppendLine(text);
        continue;
    }



    Console.WriteLine();
    Console.WriteLine("Compiling and running...");
    CompileAndRun(builder.ToString());
    builder.Clear();
    Console.ReadKey();
}

void CompileAndRun(string code)
{
    var scriptSyntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);
    using var memoryStream = new MemoryStream();

    // compile code
    var compilation = CSharpCompilation
        .Create(Guid.NewGuid().ToString(), options: compilationOptions, references: References)
        .AddSyntaxTrees(scriptSyntaxTree);

    var emitResult = compilation.Emit(memoryStream);

    if (!emitResult.Success)
    {
        Console.WriteLine("Error while compiling");
        foreach (var item in emitResult.Diagnostics)
        {
            Console.WriteLine(item);
        }
        return;
    }

    memoryStream.Seek(0, SeekOrigin.Begin);

    var assembly = assemblyLoadContext.LoadFromStream(memoryStream);

    var type = assembly.GetType("Script");
    var main = type.GetMethod("<Main>", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    var result = main.Invoke(null, null);
    Console.WriteLine(result);
}


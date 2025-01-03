﻿using System;
using System.IO;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bonsai.Sgen.Tests
{
    internal static class CompilerTestHelper
    {
        public static void CompileFromSource(string code)
        {
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5);
            var syntaxTree = CSharpSyntaxTree.ParseText(code, options);
            var serializerDependencies = new[]
            {
                typeof(YamlDotNet.Core.Parser).Assembly.Location,
                typeof(Newtonsoft.Json.JsonConvert).Assembly.Location,
                typeof(System.Reactive.Linq.Observable).Assembly.Location,
                typeof(Combinator).Assembly.Location
            };
            var assemblyReferences = serializerDependencies.Select(path => MetadataReference.CreateFromFile(path)).ToList();
            assemblyReferences.AddRange(Net60.References.All);

            var compilation = CSharpCompilation.Create(
                nameof(CompilerTestHelper),
                syntaxTrees: new[] { syntaxTree },
                references: assemblyReferences,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            using var memoryStream = new MemoryStream();
            var result = compilation.Emit(memoryStream);
            if (!result.Success)
            {
                var errorMessages = (from diagnostic in result.Diagnostics
                                     where diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error
                                     select $"{diagnostic.Id}: {diagnostic.GetMessage()}")
                                     .ToList();
                Assert.Fail(string.Join(Environment.NewLine, errorMessages));
            }
        }
    }
}

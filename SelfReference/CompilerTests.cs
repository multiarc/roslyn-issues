using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace SelfReference
{
    public class CompilerTests
    {
        [Fact]
        public void RoslynBadImageFormatRelease()
        {
            string code = @"
    namespace Tests
    {
        public static class TestClass
        {
            private static void Main(string[] args) { }

            public static object Test(dynamic value)
            {
                return value.Count > 0;
            }
        }
    }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create(
                Guid.NewGuid().ToString("N"), new[] {tree},
                AssemblyHelper.GetApplicationReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Release)
                    .WithGeneralDiagnosticOption(ReportDiagnostic.Default));
            var diagnostics = compilation.GetDiagnostics();
            Assert.False(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning));
            using (var codeStream = new MemoryStream())
            {
                using (var symbolStream = new MemoryStream())
                {
                    compilation.Emit(codeStream, symbolStream,
                        options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                    codeStream.Seek(0, SeekOrigin.Begin);
                    symbolStream.Seek(0, SeekOrigin.Begin);
                    Assembly.Load(codeStream.ToArray(), symbolStream.ToArray());
                }
            }
        }

        [Fact]
        public void RoslynBadImageFormatDebug()
        {
            string code = @"
    namespace Tests
    {
        public static class TestClass
        {
            private static void Main(string[] args) { }

            public static object Test(dynamic value)
            {
                return value.Count > 0;
            }
        }
    }";
            var tree = CSharpSyntaxTree.ParseText(code);
            var compilation = CSharpCompilation.Create(
                Guid.NewGuid().ToString("N"), new[] { tree },
                AssemblyHelper.GetApplicationReferences(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithOptimizationLevel(OptimizationLevel.Debug)
                    .WithGeneralDiagnosticOption(ReportDiagnostic.Default));
            var diagnostics = compilation.GetDiagnostics();
            Assert.False(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error || d.Severity == DiagnosticSeverity.Warning));
            using (var codeStream = new MemoryStream())
            {
                using (var symbolStream = new MemoryStream())
                {
                    compilation.Emit(codeStream, symbolStream,
                        options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                    codeStream.Seek(0, SeekOrigin.Begin);
                    symbolStream.Seek(0, SeekOrigin.Begin);
                    Assembly.Load(codeStream.ToArray(), symbolStream.ToArray());
                }
            }
        }
    }
}

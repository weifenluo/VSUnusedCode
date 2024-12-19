using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSUnusedCode
{
    #pragma warning disable RS1036

    [Generator]
    public class CSharpGenerator : ISourceGenerator
    {
        private class MethodSyntaxReciever : ISyntaxContextReceiver
        {
            public List<IMethodSymbol> Methods { get; } = new List<IMethodSymbol>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is MethodDeclarationSyntax methodDeclaration && methodDeclaration.AttributeLists.Any())
                {
                    var method = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
                    if (method is IMethodSymbol methodInfo && methodInfo.GetAttributes().Any(x => x.AttributeClass?.ToDisplayString() == "VSUnusedCode.InitializerAttribute"))
                        Methods.Add(methodInfo);
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new MethodSyntaxReciever());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxContextReceiver is not MethodSyntaxReciever methodReciever) return;
            var sourceBuilder = new StringBuilder();
            foreach (var containingClassGroup in methodReciever.Methods.GroupBy(x => x.ContainingType, SymbolEqualityComparer.Default))
            {
                var containingClass = (INamedTypeSymbol)containingClassGroup.Key!;
                var containingNamespace = containingClass.ContainingNamespace;
                var source = GenerateClass(context, containingClass, containingNamespace, containingClassGroup.ToList());
                context.AddSource($"{containingNamespace}.{containingClass.Name}.EntityType.g", SourceText.From(source, Encoding.UTF8));
            }
        }

        private string GenerateClass(GeneratorExecutionContext context, INamedTypeSymbol @class, INamespaceSymbol @namespace, List<IMethodSymbol> methods)
        {
            var classBuilder = new StringBuilder();
            classBuilder.AppendLine($"namespace {@namespace.ToDisplayString()}");
            classBuilder.AppendLine("{");
            classBuilder.AppendLine($"partial class {@class.Name}");
            classBuilder.AppendLine("{");
            classBuilder.AppendLine($"{@class.Name}()");
            classBuilder.AppendLine("{");
            // Iterate over the fields and create the properties
            foreach (IMethodSymbol method in methods)
            {
                var methodName = method.Name;
                classBuilder.AppendLine($"RegisterInitializer<{@class.Name}>(x => x.{methodName}());");
                classBuilder.AppendLine("{");
                classBuilder.AppendLine("}");
            }
            classBuilder.AppendLine("}");
            classBuilder.AppendLine("}");
            classBuilder.AppendLine("}");
            return classBuilder.ToString();
        }
    }
}

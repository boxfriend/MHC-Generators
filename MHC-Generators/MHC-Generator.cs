using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MHC_Generators
{
    [Generator]
    public class EnemyEnumGenerator : ISourceGenerator
    {
        public void Execute (GeneratorExecutionContext context)
        {
            var builder = new StringBuilder();
            builder.AppendLine("namespace Boxfriend.Enemy\n{\n\tinternal enum EnemyType\n\t{");
            builder.AppendLine("\t\tUnselected,");

            var enemies = ((EnemySyntaxReceiver)context.SyntaxReceiver)?.EnemyClasses;
            if(enemies != null)
            {
                foreach(var enemy in enemies)
                {
                    builder.AppendLine($"\t\t{enemy.Identifier},");
                }
            }

            builder.AppendLine("\t}\n}");
            context.AddSource("EnemyType.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
        }

        public void Initialize (GeneratorInitializationContext context) =>
            context.RegisterForSyntaxNotifications(() => new EnemySyntaxReceiver());
    }

    internal class EnemySyntaxReceiver : ISyntaxReceiver
    {
        private static readonly string _attributeName = nameof(EnemyAttribute).Replace("Attribute", "");
        public List<ClassDeclarationSyntax> EnemyClasses { get; } = new List<ClassDeclarationSyntax>();
        public void OnVisitSyntaxNode (SyntaxNode syntaxNode)
        {
            if(syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                var attributes = classDeclaration.AttributeLists.SelectMany(x => x.Attributes);
                if(attributes.Where(x => x.Name.ToString() == _attributeName).Any())
                    EnemyClasses.Add(classDeclaration);

            }
        }
    }
}

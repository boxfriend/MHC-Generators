using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MHC_Generators
{
    [Generator]
    internal sealed class EnemyEnumGenerator : ISourceGenerator
    {
        private const string _enemyAttribute = @"
using System;

namespace Boxfriend.Generators
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EnemyAttribute : Attribute { }
}";
        public void Execute (GeneratorExecutionContext context)
        {
            var builder = new StringBuilder();
            builder.AppendLine("namespace Boxfriend.Generators\n{\n\tinternal enum EnemyType\n\t{");
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
            context.AddSource("EnemyType.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
        }

        public void Initialize (GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(x => x.AddSource("EnemyAttribute.g.cs", _enemyAttribute));
            context.RegisterForSyntaxNotifications(() => new EnemySyntaxReceiver());
        }
    }

    internal sealed class EnemySyntaxReceiver : ISyntaxReceiver
    {
        private const string _attributeName = "Enemy";
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

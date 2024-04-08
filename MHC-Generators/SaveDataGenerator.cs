using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MHC_Generators
{
    [Generator]
    internal class SaveDataGenerator : ISourceGenerator
    {
        public void Execute (GeneratorExecutionContext context)
        {
            var saveClasses = ((SaveDataSyntaxReceiver)context.SyntaxReceiver)?.SaveClasses;
            if (saveClasses is null || saveClasses.Count < 1)
                return;

            var dataBuilder = new StringBuilder();
            foreach(var saveClass in saveClasses)
            {
                var members = GetSaveMembers(saveClass);
                BuildSaveStruct($"{saveClass.Identifier}SaveData", members, dataBuilder);
                context.AddSource($"{saveClass.Identifier}SaveData", SourceText.From(dataBuilder.ToString(), Encoding.UTF8));
                dataBuilder.Clear();
                
                //TODO: Check if class is partial before proceeding

                BuildSaveMethod(saveClass.Identifier.ToString(), members, dataBuilder);
                context.AddSource($"{saveClass.Identifier}-ToData", SourceText.From(dataBuilder.ToString(), Encoding.UTF8));
                dataBuilder.Clear();

                BuildLoadMethod(saveClass.Identifier.ToString(), members, dataBuilder);
                context.AddSource($"{saveClass.Identifier}-FromData", SourceText.From(dataBuilder.ToString(), Encoding.UTF8));
                dataBuilder.Clear();
            }

        }
        private void BuildSaveStruct (string className, IEnumerable<(string Type, string Identifier)> members, StringBuilder builder)
        {

        }

        private void BuildSaveMethod(string  className, IEnumerable<(string Type, string Identifier)> members, StringBuilder builder)
        {

        }

        private void BuildLoadMethod (string className, IEnumerable<(string Type, string Identifier)> members, StringBuilder builder)
        {

        }

        private IEnumerable<(string Type, string Identifier)> GetSaveMembers(ClassDeclarationSyntax classDeclaration)
        {
            var members = classDeclaration.Members;

            foreach(var member in members)
            {
                if(!HasAttribute(member))
                    continue;

                if(member is FieldDeclarationSyntax fds)
                {
                    foreach(var field in fds.Declaration.Variables)
                    {
                        yield return (fds.Declaration.Type.ToFullString(), field.Identifier.ToString());
                    }
                }
                else if(member is PropertyDeclarationSyntax pds)
                {
                    yield return (pds.Type.ToFullString(), pds.Identifier.ToString());
                }
            }

            yield return default;

            bool HasAttribute (MemberDeclarationSyntax mem) => mem.AttributeLists.SelectMany(x => x.Attributes)
                .Where(x => x.Name.ToString() == "Save").Any();
        }

        public void Initialize (GeneratorInitializationContext context) =>
            context.RegisterForSyntaxNotifications(() => new SaveDataSyntaxReceiver());
    }

    internal class SaveDataSyntaxReceiver : ISyntaxReceiver
    {
        private static readonly string _attributeName = "";

        public List<ClassDeclarationSyntax> SaveClasses { get; } = new List<ClassDeclarationSyntax>();

        public void OnVisitSyntaxNode (SyntaxNode syntaxNode)
        {
            if(syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                var attributes = classDeclaration.AttributeLists.SelectMany(x => x.Attributes);
                if (attributes.Where(x => x.Name.ToString() == _attributeName).Any())
                    SaveClasses.Add(classDeclaration);
            }
        }
    }
}

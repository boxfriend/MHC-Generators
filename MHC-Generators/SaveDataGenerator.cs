using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MHC_Generators
{
    [Generator]
    internal sealed class SaveDataGenerator : ISourceGenerator
    {
        private const string _saveAttribute = "Save";

        private const string _attribute = @"
using System;
namespace Boxfriend.Generators
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class SaveDataAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SaveAttribute : Attribute { }
}
";

        public void Execute (GeneratorExecutionContext context)
        {
            var saveClasses = ((SaveDataSyntaxReceiver)context.SyntaxReceiver)?.SaveClasses;
                
            if (saveClasses is null)
                return;

            var dataBuilder = new StringBuilder();
            foreach (var saveClass in saveClasses)
            {
                var className = saveClass.Identifier.ToString();
                var members = GetSaveMembers(saveClass);
                BuildSaveStruct(className, members, dataBuilder);
                context.AddSource($"{saveClass.Identifier}SaveData.g.cs", SourceText.From(dataBuilder.ToString(), Encoding.UTF8));
                dataBuilder.Clear();

                if (!saveClass.Modifiers.Any(x => x.IsKind(SyntaxKind.PartialKeyword)))
                    continue;

                BuildSaveMethod(className, members, dataBuilder);
                context.AddSource($"{saveClass.Identifier}.ToData.g.cs", SourceText.From(dataBuilder.ToString(), Encoding.UTF8));
                dataBuilder.Clear();

                BuildLoadMethod(className, members, dataBuilder);
                context.AddSource($"{saveClass.Identifier}.FromData.g.cs", SourceText.From(dataBuilder.ToString(), Encoding.UTF8));
                dataBuilder.Clear();
            }
        }
        private void BuildSaveStruct (string className, IEnumerable<(string Type, string Identifier)> members, StringBuilder builder)
        {
            builder.AppendLine($@"
namespace Boxfriend.Generators
{{
    [System.Serializable]
    public struct {className}SaveData
    {{
");
            foreach(var (Type, Identifier) in members)
            {
                builder.AppendLine($"\t\tpublic {Type} {RemovePrefix(Identifier)};");
            }
            builder.AppendLine("\t}\n}");
        }

        private string RemovePrefix(string toModify)
        {
            var i = 0;
            
            while (!char.IsLetter(toModify[i]))
                i++;

            return toModify.Substring(i);
        }

        private void BuildSaveMethod(string  className, IEnumerable<(string Type, string Identifier)> members, StringBuilder builder)
        {
            builder.AppendLine($"public partial class {className}\n{{");
            var saveName = $"Boxfriend.Generators.{className}SaveData";
            builder.AppendLine($"\tpublic {saveName} ToSaveData()\n\t{{");
            builder.AppendLine($"\t\tvar data = new {saveName}();");
            foreach(var (_, Identifier) in members)
            {
                builder.AppendLine($"\t\tdata.{RemovePrefix(Identifier)} = {Identifier};");
            }
            builder.AppendLine($"\t\treturn data;\n\t}}");

            builder.AppendLine($@"
    public async System.Threading.Tasks.Task<bool> TrySaveData(string path, System.Threading.CancellationToken cancellationToken)
    {{
        var data = ToSaveData();
        try
        {{
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            using var writer = new System.IO.StreamWriter(path);
            await writer.WriteAsync(json);
            writer.Close();
        }}
        catch (System.Exception ex)
        {{
#if UNITY_2021_1_OR_NEWER
            UnityEngine.Debug.LogException(ex);
#endif
            return false;
        }}
        return true;
    }}
}}
");
        }

        private void BuildLoadMethod (string className, IEnumerable<(string Type, string Identifier)> members, StringBuilder builder)
        {
            builder.AppendLine($"public partial class {className}\n{{");
            var saveName = $"Boxfriend.Generators.{className}SaveData";
            builder.AppendLine($"\tpublic void ApplySaveData({saveName} data)\n\t{{");
            foreach (var (_, Identifier) in members)
            {
                builder.AppendLine($"\t\t{Identifier} = data.{RemovePrefix(Identifier)};");
            }
            builder.AppendLine("\t}");

            builder.AppendLine($@"
    public async System.Threading.Tasks.Task<bool> TryLoadData(string path, System.Threading.CancellationToken cancellationToken)
    {{
        try
        {{
            using var reader = new System.IO.StreamReader(path);
            var data = await reader.ReadToEndAsync();
            reader.Close();
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<{saveName}>(data);
            ApplySaveData(obj);
        }}
        catch (System.Exception ex)
        {{
#if UNITY_2021_1_OR_NEWER
            UnityEngine.Debug.LogException(ex);
#endif
            return false;
        }}
        return true;
    }}
}}
");
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

            bool HasAttribute (MemberDeclarationSyntax mem) => mem.AttributeLists.SelectMany(x => x.Attributes)
                .Where(x => x.Name.ToString() == _saveAttribute).Any();
        }

        public void Initialize (GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(x => x.AddSource("SaveAttributes.g.cs", _attribute));
            context.RegisterForSyntaxNotifications(() => new SaveDataSyntaxReceiver());
        }
    }

    internal sealed class SaveDataSyntaxReceiver : ISyntaxReceiver
    {
        private const string _attributeName = "SaveData";

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

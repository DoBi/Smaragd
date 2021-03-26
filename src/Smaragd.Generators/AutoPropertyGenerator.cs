using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NKristek.Smaragd.Generators
{
    [Generator]
    public class AutoPropertyGenerator : ISourceGenerator
    {
        private const string attributeText = 
@"using System;

#nullable enable
namespace NKristek.Smaragd.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""AutoPropertyGenerator_DEBUG"")]
    sealed class AutoPropertyAttribute : Attribute
    {
        public AutoPropertyAttribute()
        {
            PropertyName = String.Empty;
            NotifyMethod = String.Empty;
        }

        public string PropertyName { get; set; }

        public string NotifyMethod { get; set; }
    }
}
";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource("AutoPropertyAttribute", attributeText));

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new AutoPropertySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (!(context.SyntaxContextReceiver is AutoPropertySyntaxReceiver receiver))
                return;

            // get the added attribute, and INotifyPropertyChanged
            var attributeSymbol = context.Compilation.GetTypeByMetadataName("NKristek.Smaragd.Attributes.AutoPropertyAttribute");
            var notifySymbol = context.Compilation.GetTypeByMetadataName("NKristek.Smaragd.ViewModels.Bindable");

            if (attributeSymbol == null || notifySymbol == null)
                return;

            // group the fields by class, and generate the source
            foreach (var group in receiver.Fields.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                if (group != null && group.Key is INamedTypeSymbol groupKey)
                {
                    string classSource = ProcessClass(groupKey, group.ToList(), attributeSymbol, notifySymbol, context);
                    context.AddSource($"{groupKey.Name}_AutoProperty.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return string.Empty; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"
namespace {namespaceName}
{{
    public partial class {classSymbol.Name} ");

            if (!IsType(classSymbol, notifySymbol))
            {
                source.Append($@": {notifySymbol.ToDisplayString()}");
            }

            source.Append(@"
    {
");

            // create properties for each field 
            foreach (IFieldSymbol fieldSymbol in fields)
            {
                ProcessField(source, fieldSymbol, attributeSymbol);
            }

            source.Append("} }");
            return source.ToString();
        }

        private bool IsType(INamedTypeSymbol classSymbol, ISymbol notifySymbol)
        {
            if (classSymbol.Equals(notifySymbol, SymbolEqualityComparer.Default))
            {
                return true;
            }

            if (classSymbol.BaseType != null)
            {
                return IsType(classSymbol.BaseType, notifySymbol);
            }

            return false;
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;
            TypedConstant notifyMethodOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "NotifyMethod").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;
            }

            if (notifyMethodOpt.IsNull)
            {
                source.Append($@"
public {fieldType} {propertyName} 
{{
    get => this.{fieldName};
    set => SetProperty(ref this.{fieldName}, value);
}}

");
            }
            else
            {
                source.Append($@"
public {fieldType} {propertyName} 
{{
    get => this.{fieldName};
    set
    {{
        if (SetProperty(ref this.{fieldName}, value))
        {{
            this.{notifyMethodOpt.Value}();
        }}
    }}
}}

");
            }

            string chooseName(string fieldName, TypedConstant overridenNameOpt)
            {
                if (!overridenNameOpt.IsNull)
                {
                    return overridenNameOpt.Value.ToString();
                }

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }

        }
    }
}

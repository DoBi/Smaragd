using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace NKristek.Smaragd.Generators
{
    /// <summary>
    /// Generator which automatically adds properties to ViewModels or Bindables
    /// </summary>
    [Generator]
    public class AutoPropertyGenerator : ISourceGenerator
    {
        internal const string AttributeName = @"AutoPropertyAttribute";
        internal const string AttributeNamespace = @"NKristek.Smaragd.Generators.Attributes";
        private const string Bindable = @"NKristek.Smaragd.ViewModels.Bindable";

        /// <summary>
        /// Attribute, which will be generated
        /// </summary>
        private const string _attributeText =
@"using System;

#nullable enable
namespace NKristek.Smaragd.Generators.Attributes
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""AutoPropertyGenerator_DEBUG"")]
    sealed class " + AttributeName + @" : Attribute
    {
        public " + AttributeName + @"()
        {
            PropertyName = String.Empty;
            NotifyMethod = String.Empty;
        }

        public string PropertyName { get; set; }

        public string NotifyMethod { get; set; }
    }
}
";

        /// <inheritdoc />
        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterForPostInitialization((i) => i.AddSource(AttributeName, _attributeText));

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new AutoPropertySyntaxReceiver());
        }

        /// <inheritdoc />
        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (!(context.SyntaxContextReceiver is AutoPropertySyntaxReceiver receiver))
                return;

            // get the added attribute, and INotifyPropertyChanged
            var attributeSymbol = context.Compilation.GetTypeByMetadataName($"{AttributeNamespace}.{AttributeName}");
            var notifySymbol = context.Compilation.GetTypeByMetadataName(Bindable);

            if (attributeSymbol == null || notifySymbol == null)
                return;

            // group the fields by class, and generate the source
            foreach (var group in receiver.Fields.GroupBy(f => f.ContainingType, SymbolEqualityComparer.Default))
            {
                if (group?.Key is INamedTypeSymbol groupKey)
                {
                    string classSource = ProcessClass(groupKey, group.ToList(), attributeSymbol, notifySymbol);
                    context.AddSource($"{groupKey.Name}_AutoProperty.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }

        /// <summary>
        /// Generate the partial class source code for the given class
        /// </summary>
        /// <param name="classSymbol">The class symbol</param>
        /// <param name="fields">The fields</param>
        /// <param name="attributeSymbol">The AutoProperty attribute</param>
        /// <param name="notifySymbol">Bindable class symbol</param>
        /// <returns>The generated partial class code</returns>
        private string ProcessClass(INamedTypeSymbol classSymbol, List<IFieldSymbol> fields, ISymbol attributeSymbol, ISymbol notifySymbol)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return string.Empty; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            var source = new StringBuilder($@"
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

        /// <summary>
        /// Add the property code for the given field
        /// </summary>
        /// <param name="source">The <see cref="StringBuilder"/></param>
        /// <param name="fieldSymbol">The field</param>
        /// <param name="attributeSymbol">The AutoProperty attribute</param>
        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            AttributeData attributeData = fieldSymbol.GetAttributes().Single(ad =>
                ad.AttributeClass != null && ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant overridenNameOpt =
                attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;
            TypedConstant notifyMethodOpt =
                attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "NotifyMethod").Value;

            var propertyName = chooseName(fieldName, overridenNameOpt);
            if (string.IsNullOrEmpty(propertyName) || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;
            }

            source.Append($@"
            public {fieldType} {propertyName}
            {{
                get => this.{fieldName};");

            if (notifyMethodOpt.IsNull)
            {
                source.Append($@"
                set => SetProperty(ref this.{fieldName}, value);");
            }
            else
            {
                source.Append($@"
                set
                {{
                    if (SetProperty(ref this.{fieldName}, value))
                    {{
                        this.{notifyMethodOpt.Value}();
                    }}
                }}");
            }

            source.Append(@"
            }
");

            string? chooseName(string fieldName, TypedConstant overridenNameOpt)
            {
                if (!overridenNameOpt.IsNull && overridenNameOpt.Value != null)
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

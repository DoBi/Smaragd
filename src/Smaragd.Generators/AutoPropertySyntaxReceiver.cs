using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NKristek.Smaragd.Generators
{
    /// <summary>
    /// Created on demand before each generation pass
    /// </summary>
    internal class AutoPropertySyntaxReceiver : ISyntaxContextReceiver
    {
        public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

        /// <summary>
        /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
        /// </summary>
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            // any field with at least one attribute is a candidate for property generation
            if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
                && fieldDeclarationSyntax.AttributeLists.Count > 0)
            {
                foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                {
                    // Get the symbol being declared by the field, and keep it if its annotated
                    if (context.SemanticModel.GetDeclaredSymbol(variable) is IFieldSymbol fieldSymbol &&
                        fieldSymbol.GetAttributes().Any(ad =>
                            ad.AttributeClass?.ToDisplayString() ==
                            $"{AutoPropertyGenerator.AttributeNamespace}.{AutoPropertyGenerator.AttributeName}"))
                    {
                        Fields.Add(fieldSymbol);
                    }
                }
            }
        }
    }
}
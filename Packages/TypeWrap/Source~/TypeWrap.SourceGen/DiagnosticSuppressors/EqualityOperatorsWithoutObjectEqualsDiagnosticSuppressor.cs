using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGen.Common;

namespace TypeWrap.SourceGen
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class EqualityOperatorsWithoutObjectEqualsDiagnosticSuppressor : DiagnosticSuppressor
    {
        public static readonly SuppressionDescriptor CS0660 = new(
              id: "TYPEWRAP_CS0660"
            , suppressedDiagnosticId: "CS0660"
            , justification: "Type wrappers can define equality operators without overriding Object.Equals"
        );

        public const string NAMESPACE = "TypeWrap";
        public const string WRAP_TYPE = "WrapType";
        public const string WRAP_RECORD = "WrapRecord";

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(CS0660);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
            {
                var syntaxNode = diagnostic.Location.SourceTree
                    ?.GetRoot(context.CancellationToken)
                    ?.FindNode(diagnostic.Location.SourceSpan);

                if (syntaxNode is null)
                {
                    continue;
                }

                if (syntaxNode is not TypeDeclarationSyntax typeSyntax
                    || HasAttribute(typeSyntax) == false
                )
                {
                    continue;
                }

                context.ReportSuppression(Suppression.Create(CS0660, diagnostic));
            }
        }

        private static bool HasAttribute(TypeDeclarationSyntax typeSyntax)
        {
            return typeSyntax.HasAttributeCandidate(NAMESPACE, WRAP_TYPE)
                || typeSyntax.HasAttributeCandidate(NAMESPACE, WRAP_RECORD);
        }
    }
}

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SourceGen.Common;

namespace TypeWrap.SourceGen
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ObsoleteMemberOnRefStructDiagnosticSuppressor : DiagnosticSuppressor
    {
        public static readonly SuppressionDescriptor CS0809 = new(
              id: "TYPEWRAP_CS0809"
            , suppressedDiagnosticId: "CS0809"
            , justification: "Obsolete members of a type wrapper can override non-obsolete member of the underlying type"
        );

        public const string WRAP_TYPE_ATTRIBUTE = "global::TypeWrap.WrapTypeAttribute";
        public const string WRAP_RECORD_ATTRIBUTE = "global::TypeWrap.WrapRecordAttribute";

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create(CS0809);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
            {
                var syntaxNode = diagnostic.Location.SourceTree
                    ?.GetRoot(context.CancellationToken)
                    ?.FindNode(diagnostic.Location.SourceSpan);

                if (syntaxNode is not MemberDeclarationSyntax memberSyntax
                    || memberSyntax.HasAttributeCandidate("System", "Obsolete") == false
                    || memberSyntax.Parent is not TypeDeclarationSyntax typeSyntax
                )
                {
                    continue;
                }

                var model = context.GetSemanticModel(typeSyntax.SyntaxTree);
                var declaredSymbol = model.GetDeclaredSymbol(typeSyntax, context.CancellationToken);

                if (declaredSymbol is ITypeSymbol typeSymbol && HasAttribute(typeSymbol))
                {
                    context.ReportSuppression(Suppression.Create(CS0809, diagnostic));
                }
            }
        }

        private static bool HasAttribute(ITypeSymbol type)
        {
            return type.HasAttribute(WRAP_TYPE_ATTRIBUTE)
                || type.HasAttribute(WRAP_RECORD_ATTRIBUTE);
        }
    }
}

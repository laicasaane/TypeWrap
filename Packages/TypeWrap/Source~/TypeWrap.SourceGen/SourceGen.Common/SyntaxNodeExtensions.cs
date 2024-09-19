using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGen.Common
{
    public static class SyntaxNodeExt
    {
        public static string GetGeneratedSourceFileName(this SyntaxTree syntaxTree, string generatorName, int salting = 0)
        {
            var (isSuccess, fileName) = TryGetFileNameWithoutExtension(syntaxTree);
            var stableHashCode = SourceGenHelpers.GetStableHashCode(syntaxTree.FilePath) & 0x7fffffff;

            var postfix = generatorName.Length > 0 ? $"__{generatorName}" : string.Empty;

            if (isSuccess)
                fileName = $"{fileName}{postfix}_{stableHashCode}{salting}.g.cs";
            else
                fileName = Path.Combine($"{Path.GetRandomFileName()}{postfix}", ".g.cs");

            return fileName;
        }

        public static string GetGeneratedSourceFileName(this SyntaxTree syntaxTree, string generatorName, SyntaxNode node, string typeName)
            => GetGeneratedSourceFileName(syntaxTree, generatorName, node.GetLocation().GetLineSpan().StartLinePosition.Line, typeName);

        public static string GetGeneratedSourceFileName(this SyntaxTree syntaxTree, string generatorName, int salting, string typeName)
        {
            var (isSuccess, fileName) = TryGetFileNameWithoutExtension(syntaxTree);
            var stableHashCode = SourceGenHelpers.GetStableHashCode(syntaxTree.FilePath) & 0x7fffffff;

            var postfix = generatorName.Length > 0 ? $"__{generatorName}" : string.Empty;

            if (string.IsNullOrWhiteSpace(typeName) == false)
            {
                postfix = $"__{typeName}{postfix}";
            }

            if (isSuccess)
                fileName = $"{fileName}{postfix}_{stableHashCode}{salting}.g.cs";
            else
                fileName = Path.Combine($"{Path.GetRandomFileName()}{postfix}", ".g.cs");

            return fileName;
        }

        public static string GetGeneratedSourceFilePath(this SyntaxTree syntaxTree, string assemblyName, string generatorName)
        {
            var fileName = GetGeneratedSourceFileName(syntaxTree, generatorName);
            if (SourceGenHelpers.CanWriteToProjectPath)
            {
                var saveToDirectory = $"{SourceGenHelpers.ProjectPath}/Temp/GeneratedCode/{assemblyName}/";
                Directory.CreateDirectory(saveToDirectory);
                return saveToDirectory + fileName;
            }
            return $"Temp/GeneratedCode/{assemblyName}";
        }

        public static (bool IsSuccess, string FileName) TryGetFileNameWithoutExtension(this SyntaxTree syntaxTree)
        {
            var fileName = Path.GetFileNameWithoutExtension(syntaxTree.FilePath);
            return (IsSuccess: true, fileName);
        }

        private class PreprocessorTriviaRemover : CSharpSyntaxRewriter
        {
            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                return trivia.Kind() switch {
                    SyntaxKind.DisabledTextTrivia
                 or SyntaxKind.PreprocessingMessageTrivia
                 or SyntaxKind.IfDirectiveTrivia
                 or SyntaxKind.ElifDirectiveTrivia
                 or SyntaxKind.ElseDirectiveTrivia
                 or SyntaxKind.EndIfDirectiveTrivia
                 or SyntaxKind.RegionDirectiveTrivia
                 or SyntaxKind.EndRegionDirectiveTrivia
                 or SyntaxKind.DefineDirectiveTrivia
                 or SyntaxKind.UndefDirectiveTrivia
                 or SyntaxKind.ErrorDirectiveTrivia
                 or SyntaxKind.WarningDirectiveTrivia
                 or SyntaxKind.PragmaWarningDirectiveTrivia
                 or SyntaxKind.PragmaChecksumDirectiveTrivia
                 or SyntaxKind.ReferenceDirectiveTrivia
                 or SyntaxKind.BadDirectiveTrivia
                     => default,

                    _ => trivia,
                };
            }
        }

        public static T WithoutPreprocessorTrivia<T>(this T node)
            where T : SyntaxNode
        {
            var preprocessorTriviaRemover = new PreprocessorTriviaRemover();
            return (T)preprocessorTriviaRemover.Visit(node);
        }

        /// <summary>
        /// Test if a node represent an identifier by comparing string followed
        /// by a TypeArgumentListSyntax, which is returned through typeArgumentListSyntax.
        /// </summary>
        /// <param name="syntaxNode"></param>
        /// <param name="identifier"></param>
        /// <param name="typeArgumentListSyntax"></param>
        /// <returns></returns>
        public static bool IsIdentifier(this SyntaxNode syntaxNode, string identifier, out TypeArgumentListSyntax typeArgumentListSyntax)
        {
            switch (syntaxNode)
            {
                case GenericNameSyntax genericNameSyntax:
                    typeArgumentListSyntax = genericNameSyntax.TypeArgumentList;
                    return genericNameSyntax.Identifier.ValueText == identifier;
                case SimpleNameSyntax simpleNameSyntax:
                    typeArgumentListSyntax = default;
                    return simpleNameSyntax.Identifier.ValueText == identifier;
            }
            typeArgumentListSyntax = default;
            return false;
        }

        /// <summary>
        /// Figures out as fast as possible if the syntax node does not represent a type name.
        /// Use for early-out tests within the OnVisitSyntaxNode calls.
        /// Use the SemanticModel from GeneratorExecutionContext.Compilation.GetSemanticModel() to get an accurate result during the Execute call.
        ///
        /// Returns false if the node is found to *not* be equal using fast early-out tests.
        /// Returns true if type name is likely equal.
        /// </summary>
        /// <param name="syntaxNode"></param>
        /// <param name="typeNameNamesapce">The host namepsace of the type name. e.g. "Unity.Entities"</param>
        /// <param name="typeName">The unqualified type name of the generic type. e.g. "Entity" </param>
        /// <returns></returns>
        public static bool IsTypeNameCandidate(this SyntaxNode syntaxNode, string typeNameNamesapce, string typeName)
            => IsTypeNameCandidate(syntaxNode, typeNameNamesapce, typeName, out _);

        /// <summary>
        /// Figures out as fast as possible if the syntax node does not represent a type name.
        /// Use for early-out tests within the OnVisitSyntaxNode calls.
        /// Use the SemanticModel from GeneratorExecutionContext.Compilation.GetSemanticModel() to get an accurate result during the Execute call.
        ///
        /// Returns false if the node is found to *not* be equal using fast early-out tests.
        /// Returns true if type name is likely equal.
        /// </summary>
        /// <param name="syntaxNode"></param>
        /// <param name="typeNameNamesapce">The host namepsace of the type name. e.g. "Unity.Entities"</param>
        /// <param name="typeName">The unqualified type name of the generic type. e.g. "Entity" </param>
        /// <param name="typeArgumentListSyntax">output the TypeArgumentListSyntax node if the type represented by this SyntaxNode is generic</param>
        /// <returns></returns>
        public static bool IsTypeNameCandidate(
              this SyntaxNode syntaxNode
            , string typeNameNamesapce
            , string typeName
            , out TypeArgumentListSyntax typeArgumentListSyntax
        )
        {
            switch (syntaxNode)
            {
                case QualifiedNameSyntax qualifiedNameSyntax:
                    // Fast estimate right part and extract a possible TypeArgumentListSyntax to our own TypeArgumentListSyntax output
                    if (!IsIdentifier(qualifiedNameSyntax.Right, typeName, out typeArgumentListSyntax))
                    {
                        return false;
                    }
                    var iLastDot = typeNameNamesapce.LastIndexOf('.');
                    if (iLastDot < 0)
                    {
                        //End of qualified names
                        var typename = qualifiedNameSyntax.Left.ToString();

                        if (typename.StartsWith("global::"))
                            typename = typename.Substring(8);

                        typeArgumentListSyntax = default;
                        return typename == typeNameNamesapce;
                    }
                    else if (qualifiedNameSyntax.Left != null)
                    {
                        // Fast estimate left part without extracting any TypeArgumentListSyntax
                        return qualifiedNameSyntax.Left.IsTypeNameCandidate(
                              typeNameNamesapce.Substring(0, iLastDot)
                            , typeNameNamesapce.Substring(iLastDot + 1)
                        );
                    }
                    else
                    {
                        // Limit the test here, any remaining qualified name is assumed to be a known scope. e.g. part of a using statement or other type defined withing the same unit.
                        return true;
                    }

                default:
                    // Check if current node is the identifier symbolName
                    // and if the current node's scope knows of the scope name symbolNamesapce
                    return IsIdentifier(syntaxNode, typeName, out typeArgumentListSyntax);
            }
        }

        /// <summary>
        /// Figures out as fast as possible if the node has an attribute that may be equal the to string provided.
        /// Use for early-out tests within the OnVisitSyntaxNode calls.
        /// Use the SemanticModel from GeneratorExecutionContext.Compilation.GetSemanticModel() to get an accurate result during the Execute call.
        ///
        /// Returns false if no attribute is likely equal using fast early-out tests.
        /// Returns true if an attribute is likely equal.
        /// </summary>
        /// <param name="syntaxNode">Node to test type name against</param>
        /// <param name="attributeNameSpace">The host namepsace of the attribute type name. e.g. "Unity.Entities"</param>
        /// <param name="attributeName">The unqualified attribute name. e.g. "UpdateBefore" </param>
        /// <returns></returns>
        public static bool HasAttributeCandidate(this SyntaxNode syntaxNode, string attributeNameSpace, string attributeName)
        {
            foreach (var attribListCandidate in syntaxNode.ChildNodes())
            {
                if (attribListCandidate == null || attribListCandidate.IsKind(SyntaxKind.AttributeList) == false)
                {
                    continue;
                }

                foreach (var attribCandidate in attribListCandidate.ChildNodes())
                {
                    if (attribCandidate is AttributeSyntax attrib
                        && attrib.Name.IsTypeNameCandidate(attributeNameSpace, attributeName)
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

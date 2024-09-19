using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

// Everything in this file was copied from Unity's source generators.
namespace SourceGen.Common
{
    public static class SymbolExtensions
    {
        public static SymbolDisplayFormat QualifiedFormat { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

        public static SymbolDisplayFormat MemberName { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle:
                    SymbolDisplayTypeQualificationStyle.NameOnly,
                genericsOptions:
                    SymbolDisplayGenericsOptions.None,
                memberOptions:
                    SymbolDisplayMemberOptions.None,
                parameterOptions:
                    SymbolDisplayParameterOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.None
            );

        public static SymbolDisplayFormat QualifiedMemberFormatWithGlobalPrefix { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle:
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut,
                globalNamespaceStyle:
                    SymbolDisplayGlobalNamespaceStyle.Included,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

        public static SymbolDisplayFormat QualifiedMemberNameWithGlobalPrefix { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle:
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.None,
                globalNamespaceStyle:
                    SymbolDisplayGlobalNamespaceStyle.Included,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

        public static SymbolDisplayFormat QualifiedMemberFormatWithType { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle:
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

        public static SymbolDisplayFormat QualifiedFormatWithoutGlobalPrefix { get; }
            = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes
            );

        public static bool Is(this ITypeSymbol symbol, string fullyQualifiedName, bool checkBaseType = true)
        {
            fullyQualifiedName = PrependGlobalIfMissing(fullyQualifiedName);

            if (symbol is null)
                return false;

            if (symbol.ToDisplayString(QualifiedFormat) == fullyQualifiedName)
                return true;

            return checkBaseType && symbol.BaseType.Is(fullyQualifiedName);
        }

        public static void GetUnmanagedSize(this ITypeSymbol symbol, ref int size, HashSet<ITypeSymbol> seenSymbols = null)
        {
            if (symbol == null)
            {
                return;
            }

            switch (symbol.SpecialType)
            {
                case SpecialType.System_Char:
                {
                    size += sizeof(char);
                    return;
                }

                case SpecialType.System_Boolean:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                {
                    size += sizeof(byte);
                    return;
                }

                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                {
                    size += sizeof(ushort);
                    return;
                }

                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Single:
                {
                    size += sizeof(uint);
                    return;
                }

                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Double:
                case SpecialType.System_DateTime:
                {
                    size += sizeof(ulong);
                    return;
                }

                case SpecialType.System_Decimal:
                {
                    size += sizeof(decimal);
                    return;
                }

                default:
                {
                    if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.EnumUnderlyingType != null)
                    {
                        GetUnmanagedSize(namedTypeSymbol.EnumUnderlyingType, ref size, seenSymbols);
                    }
                    else if (symbol.IsUnmanagedType)
                    {
                        seenSymbols ??= new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

                        if (seenSymbols.Add(symbol))
                        {
                            foreach (var field in symbol.GetMembers().OfType<IFieldSymbol>())
                            {
                                if (field.IsStatic == false && field.IsConst == false)
                                {
                                    GetUnmanagedSize(field.Type, ref size, seenSymbols);
                                }
                            }
                        }
                    }

                    return;
                }
            }
        }

        public static string ToFullName(this ITypeSymbol symbol)
            => symbol.ToDisplayString(QualifiedFormat);

        public static string ToValidIdentifier(this ITypeSymbol symbol)
            => symbol.ToDisplayString(QualifiedFormatWithoutGlobalPrefix).ToValidIdentifier();

        public static bool Is(this ITypeSymbol symbol, string nameSpace, string typeName, bool checkBaseType = true)
        {
            if (symbol is null)
                return false;

            if (symbol.Name == typeName && symbol.ContainingNamespace?.Name == nameSpace)
                return true;

            return checkBaseType && symbol.BaseType.Is(nameSpace, typeName);
        }

        public static bool InheritsFromInterface(this ITypeSymbol symbol, string interfaceName, bool checkBaseType = true)
        {
            if (symbol is null)
                return false;

            interfaceName = PrependGlobalIfMissing(interfaceName);

            foreach (var @interface in symbol.Interfaces)
            {
                if (@interface.ToDisplayString(QualifiedFormat) == interfaceName)
                    return true;

                if (checkBaseType)
                {
                    foreach (var baseInterface in @interface.AllInterfaces)
                    {
                        if (baseInterface.ToDisplayString(QualifiedFormat) == interfaceName)
                            return true;

                        if (baseInterface.InheritsFromInterface(interfaceName))
                            return true;
                    }
                }
            }

            if (checkBaseType && symbol.BaseType != null)
            {
                if (symbol.BaseType.InheritsFromInterface(interfaceName))
                    return true;
            }

            return false;
        }

        public static bool InheritsFromType(this ITypeSymbol symbol, string typeName, bool checkBaseType = true)
        {
            typeName = PrependGlobalIfMissing(typeName);

            if (symbol is null)
                return false;

            if (symbol.ToDisplayString(QualifiedFormat) == typeName)
                return true;

            if (checkBaseType && symbol.BaseType != null)
            {
                if (symbol.BaseType.InheritsFromType(typeName))
                    return true;
            }

            return false;
        }

        public static bool HasAttributeSimple(this ISymbol typeSymbol, string attributeName)
        {
            return typeSymbol.GetAttributes()
                .Any(attribute => attribute.AttributeClass.Name == attributeName);
        }
        
        public static bool HasAttribute(this ISymbol typeSymbol, string fullyQualifiedAttributeName)
        {
            fullyQualifiedAttributeName = PrependGlobalIfMissing(fullyQualifiedAttributeName);

            return typeSymbol.GetAttributes()
                .Any(attribute => attribute.AttributeClass.ToFullName() == fullyQualifiedAttributeName);
        }

        public static bool HasAttributeOrFieldWithAttribute(this ITypeSymbol typeSymbol, string fullyQualifiedAttributeName)
        {
            fullyQualifiedAttributeName = PrependGlobalIfMissing(fullyQualifiedAttributeName);

            return typeSymbol.HasAttribute(fullyQualifiedAttributeName)
                || typeSymbol.GetMembers().OfType<IFieldSymbol>()
                    .Any(f => !f.IsStatic && f.Type.HasAttributeOrFieldWithAttribute(fullyQualifiedAttributeName));
        }

        private static string PrependGlobalIfMissing(this string typeOrNamespaceName)
            => typeOrNamespaceName.StartsWith("global::") == false
            ? $"global::{typeOrNamespaceName}"
            : typeOrNamespaceName;

        public static ImmutableArray<ISymbol> FindInterfaceMembers(this ISymbol symbol, ImmutableArray<INamedTypeSymbol> interfaces)
        {
            if (symbol.Kind is not (SymbolKind.Method or SymbolKind.Property or SymbolKind.Event))
            {
                return ImmutableArray<ISymbol>.Empty;
            }

            using var result = ImmutableArrayBuilder<ISymbol>.Rent();
            var type = symbol.ContainingType;

            foreach (var @interface in interfaces)
            {
                foreach (var member in @interface.GetMembers())
                {
                    var impl = type.FindImplementationForInterfaceMember(member);

                    if (SymbolEqualityComparer.Default.Equals(symbol, impl))
                    {
                        result.Add(member);
                        break;
                    }
                }
            }

            return result.ToImmutable();
        }
    }
}


﻿using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using SourceGen.Common;

namespace TypeWrap.SourceGen
{
    partial class WrapDeclaration
    {
        private const string AGGRESSIVE_INLINING = "[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]";
        private const string GENERATED_CODE = "[global::System.CodeDom.Compiler.GeneratedCode(\"TypeWrap.SourceGen.TypeWrapGenerator\", \"1.0.0\")]";
        private const string EXCLUDE_COVERAGE = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";
        private const string OBSOLETE = "[global::System.Obsolete(\"Not supported\", true)]";

        private SpecialMethodType _writenSpecialMethods;

        public string WriteCode()
        {
            var scopePrinter = new SyntaxNodeScopePrinter(Printer.DefaultLarge, Syntax.Parent);
            var p = scopePrinter.printer;

            p = p.IncreasedIndent();
            {
                if (ExcludeConverter == false && IsRefStruct == false)
                {
                    p.PrintBeginLine("[global::System.ComponentModel.TypeConverter(typeof(")
                        .Print(FullTypeName).Print(".")
                        .Print(TypeName).PrintEndLine("TypeConverter))]");
                }

                p.PrintBeginLine()
                    .PrintIf(IsRefStruct, "ref ")
                    .Print("partial ")
                    .PrintIf(IsRecord, "record ")
                    .PrintIf(IsStruct, "struct ", "class ")
                    .Print(TypeName);

                if (IsRefStruct)
                {
                    p.PrintEndLine();
                }
                else
                {
                    p.Print(" : global::TypeWrap.IWrap<")
                        .Print(FieldTypeName)
                        .PrintEndLine(">");

                    WriteInterfaces(ref p);
                }

                p.OpenScope();
                {
                    if (IsRecord == false)
                    {
                        WriteBackingField(ref p);
                        WritePrimaryConstructor(ref p);
                    }

                    WriteFields(ref p);
                    WriteProperties(ref p);
                    WriteEvents(ref p);
                    WriteMethods(ref p);
                    WriteConversionOperators(ref p);
                    WriteOperators(ref p);
                    WriteTypeConverter(ref p);
                }
                p.CloseScope();
            }
            p = p.DecreasedIndent();

            //p.PrintEndLine();
            //p.PrintEndLine();
            //p.PrintEndLine();
            //p.PrintLine("#region    SYMBOL");
            //p.PrintLine("#endregion ======");
            //PrintDebug(ref p, Symbol);

            //p.PrintEndLine();
            //p.PrintEndLine();
            //p.PrintEndLine();
            //p.PrintLine("#region    FIELD_TYPE_SYMBOL");
            //p.PrintLine("#endregion =================");
            //PrintDebug(ref p, FieldTypeSymbol);

            return p.Result;
        }

        private void WriteInterfaces(ref Printer p)
        {
            p = p.IncreasedIndent();

            if (ImplementInterfaces.HasFlag(InterfaceKind.Equatable))
            {
                p.PrintBeginLine(", ").Print("global::System.IEquatable<").Print(FullTypeName).PrintEndLine(">");
                p.PrintBeginLine(", ").Print("global::System.IEquatable<").Print(FieldTypeName).PrintEndLine(">");
            }

            if (ImplementInterfaces.HasFlag(InterfaceKind.Comparable))
            {
                p.PrintBeginLine(", ").Print("global::System.IComparable<").Print(FullTypeName).PrintEndLine(">");
                p.PrintBeginLine(", ").Print("global::System.IComparable<").Print(FieldTypeName).PrintEndLine(">");
            }

            //foreach (var @interface in Interfaces)
            //{
            //    p.PrintLine($", {@interface.ToFullName()}");
            //}

            p = p.DecreasedIndent();
        }

        private void WriteBackingField(ref Printer p)
        {
            if (IsFieldDeclared)
            {
                return;
            }

            p.PrintLine(GENERATED_CODE);
            p.PrintBeginLine("public ")
                .PrintIf(IsReadOnly, "readonly ")
                .Print(FieldTypeName)
                .Print(" ")
                .Print(FieldName)
                .PrintEndLine($";");
            p.PrintEndLine();
        }

        private void WritePrimaryConstructor(ref Printer p)
        {
            p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"public {TypeName}({FieldTypeName} value)");
            p.OpenScope();
            {
                p.PrintLine($"this.{FieldName} = value;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteFields(ref Printer p)
        {
            foreach (var field in Fields)
            {
                WriteField(ref p, field);
            }
        }

        void WriteField(ref Printer p, IFieldSymbol field)
        {
            var returnTypeName = field.Type.ToFullName();
            var sameType = SymbolEqualityComparer.Default.Equals(field.Type, FieldTypeSymbol);
            var name = field.Name;

            if (field.IsConst)
            {
                if (sameType)
                {
                    p.PrintLine(GENERATED_CODE);
                    p.PrintLine($"public static readonly {FullTypeName} {name} = new {FullTypeName}({FieldTypeName}.{name});");
                }
                else
                {
                    p.PrintLine(GENERATED_CODE);
                    p.PrintLine($"public const {returnTypeName} {name} = {FieldTypeName}.{name};");
                }
            }
            else if (field.IsStatic)
            {
                if (field.IsReadOnly && sameType)
                {
                    p.PrintLine(GENERATED_CODE);
                    p.PrintLine($"public static readonly {FullTypeName} {name} = new {FullTypeName}({FieldTypeName}.{name});");
                }
                else if (field.IsReadOnly)
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public static {returnTypeName} {name}");
                    p.OpenScope();
                    {
                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"get => {FieldTypeName}.{name};");
                    }
                    p.CloseScope();
                }
                else if (sameType)
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public static {FullTypeName} {name}");
                    p.OpenScope();
                    {
                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"get => new {FullTypeName}({FieldTypeName}.{name});");
                        p.PrintEndLine();

                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"set => {FieldTypeName}.{name} = value.{FieldName};");
                    }
                    p.CloseScope();
                }
                else
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public static {returnTypeName} {name}");
                    p.OpenScope();
                    {
                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"get => {FieldTypeName}.{name};");
                        p.PrintEndLine();

                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"set => {FieldTypeName}.{name} = value;");
                    }
                    p.CloseScope();
                }
            }
            else
            {
                if (field.IsReadOnly && sameType)
                {
                    p.PrintLine(GENERATED_CODE);
                    p.PrintLine($"public readonly {FullTypeName} {name} = new {FullTypeName}(this.{FieldName}.{name});");
                }
                else if (field.IsReadOnly)
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public {returnTypeName} {name}");
                    p.OpenScope();
                    {
                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"get => this.{FieldName}.{name};");
                    }
                    p.CloseScope();
                }
                else if (sameType)
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public {FullTypeName} {name}");
                    p.OpenScope();
                    {
                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"get => new {FullTypeName}(this.{FieldName}.{name});");
                        p.PrintEndLine();

                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"set => this.{FieldName}.{name} = value.{FieldName};");
                    }
                    p.CloseScope();
                }
                else
                {
                    p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                    p.PrintLine($"public {returnTypeName} {name}");
                    p.OpenScope();
                    {
                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"get => this.{FieldName}.{name};");
                        p.PrintEndLine();

                        p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE).PrintLine(AGGRESSIVE_INLINING);
                        p.PrintLine($"set => this.{FieldName}.{name} = value;");
                    }
                    p.CloseScope();
                }
            }

            p.PrintEndLine();
        }

        private void WriteProperties(ref Printer p)
        {
            foreach (var property in Properties)
            {
                if (property.ExplicitInterfaceImplementations.Length > 0)
                {
                    continue;
                }

                WriteProperty(ref p, property);
            }
        }

        private void WriteProperty(ref Printer p, IPropertySymbol property)
        {
            var name = property.ToDisplayString(SymbolExtensions.QualifiedMemberNameWithGlobalPrefix);
            var returnTypeName = property.Type.ToFullName();
            var sameType = SymbolEqualityComparer.Default.Equals(property.Type, FieldTypeSymbol);
            var hasParams = property.IsIndexer || property.Parameters.Length > 0;
            var isPublic = property.DeclaredAccessibility == Accessibility.Public;
            var wrapperIsStruct = IsStruct;
            var wrapperIsReadOnly = IsReadOnly;
            var fieldTypeIsStruct = FieldTypeSymbol.TypeKind == TypeKind.Struct;

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLineIf(isPublic, "public ", "");
            p.PrintIf(property.IsStatic, "static ");
            p.PrintIf(property.RefKind == RefKind.Ref, "ref ");
            p.PrintIf(property.RefKind == RefKind.RefReadOnly, "ref readonly ");

            var isRef = property.RefKind is RefKind.Ref or RefKind.RefReadOnly;
            var canConvertType = wrapperIsStruct && sameType && isRef == false;

            p.PrintIf(canConvertType, FullTypeName, returnTypeName);
            p.Print(" ");

            string explicitTypeName;
            bool isIndexer;

            if (property.ExplicitInterfaceImplementations.Length > 0)
            {
                var explicitImpl = property.ExplicitInterfaceImplementations[0];
                explicitTypeName = explicitImpl.ContainingType.ToFullName();
                isIndexer = explicitImpl.IsIndexer;

                p.Print(explicitTypeName).Print(".");
            }
            else
            {
                explicitTypeName = string.Empty;
                isIndexer = property.IsIndexer;
            }

            if (hasParams)
            {
                p.PrintIf(isIndexer, "this", name)
                    .Print("[");

                var propParams = property.Parameters;
                var last = propParams.Length - 1;

                for (var i = 0; i <= last; i++)
                {
                    var param = propParams[i];

                    p.Print($"{param.Type.ToFullName()} {param.Name}");
                    p.PrintIf(i < last, ", ");
                }

                p.Print("]");
            }
            else
            {
                p.Print(name);
            }

            p.PrintEndLine();

            p.OpenScope();
            {
                var fieldName = string.IsNullOrEmpty(explicitTypeName)
                    ? $"this.{FieldName}"
                    : $"(({explicitTypeName})this.{FieldName})";

                var accessor = property.IsStatic ? returnTypeName : fieldName;

                if (hasParams)
                {
                    WriteIndexerBody(
                          ref p
                        , property
                        , accessor
                        , isRef
                        , wrapperIsStruct
                        , wrapperIsReadOnly
                        , fieldTypeIsStruct
                    );
                }
                else
                {
                    WritePropertyBody(
                          ref p
                        , property
                        , accessor
                        , name
                        , isRef
                        , wrapperIsStruct
                        , wrapperIsReadOnly
                        , fieldTypeIsStruct
                    );
                }
            }
            p.CloseScope();
            p.PrintEndLine();

            static void WriteIndexerBody(
                  ref Printer p
                , IPropertySymbol property
                , string accessor
                , bool isRef
                , bool wrapperIsStruct
                , bool wrapperIsReadOnly
                , bool fieldTypeIsStruct
            )
            {
                if (property.GetMethod != null)
                {
                    var isGetterRef = property.RefKind != RefKind.Ref && property.GetMethod.RefKind == RefKind.Ref;
                    var isGetterRefRO = property.RefKind != RefKind.RefReadOnly && property.GetMethod.RefKind == RefKind.RefReadOnly;

                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintBeginLine();
                    p.PrintIf(isGetterRef, "ref ");
                    p.PrintIf(isGetterRefRO, "ref readonly ");
                    p.Print("get => ");
                    p.PrintIf(isRef, "ref ");
                    p.Print(accessor).Print("[");
                    WriteIndexerParams(ref p, property);
                    p.Print("];");
                    p.PrintEndLine();
                    p.PrintEndLine();
                }

                if (fieldTypeIsStruct || (wrapperIsStruct && wrapperIsReadOnly))
                {
                    return;
                }

                if (property.SetMethod != null)
                {
                    var isSetterRef = property.RefKind != RefKind.Ref && property.SetMethod.RefKind == RefKind.Ref;
                    var isSetterRefRO = property.RefKind != RefKind.RefReadOnly && property.SetMethod.RefKind == RefKind.RefReadOnly;

                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintBeginLine();
                    p.PrintIf(isSetterRef, "ref ");
                    p.PrintIf(isSetterRefRO, "ref readonly ");
                    p.Print("set => ").Print(accessor).Print("[");
                    WriteIndexerParams(ref p, property);
                    p.PrintEndLine("] = value;");
                }
            }

            static void WritePropertyBody(
                  ref Printer p
                , IPropertySymbol property
                , string accessor
                , string propName
                , bool isRef
                , bool wrapperIsStruct
                , bool wrapperIsReadOnly
                , bool fieldTypeIsStruct
            )
            {
                if (property.GetMethod != null)
                {
                    var isGetterRef = property.RefKind != RefKind.Ref && property.GetMethod.RefKind == RefKind.Ref;
                    var isGetterRefRO = property.RefKind != RefKind.RefReadOnly && property.GetMethod.RefKind == RefKind.RefReadOnly;

                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintBeginLine();
                    p.PrintIf(isGetterRef, "ref ");
                    p.PrintIf(isGetterRefRO, "ref readonly ");
                    p.Print("get => ");
                    p.PrintIf(isRef, "ref ");

                    p.Print($"{accessor}.{propName}");
                    p.Print(";").PrintEndLine().PrintEndLine();
                }

                if (fieldTypeIsStruct || (wrapperIsStruct && wrapperIsReadOnly))
                {
                    return;
                }

                if (property.SetMethod != null)
                {
                    var isSetterRef = property.RefKind != RefKind.Ref && property.SetMethod.RefKind == RefKind.Ref;
                    var isSetterRefRO = property.RefKind != RefKind.RefReadOnly && property.SetMethod.RefKind == RefKind.RefReadOnly;

                    p.PrintLine(AGGRESSIVE_INLINING);
                    p.PrintBeginLine();
                    p.PrintIf(isSetterRef, "ref ");
                    p.PrintIf(isSetterRefRO, "ref readonly ");
                    p.PrintEndLine($"set => {accessor}.{propName} = value;");
                }
            }

            static void WriteIndexerParams(ref Printer p, IPropertySymbol property)
            {
                var propParams = property.Parameters;
                var last = propParams.Length - 1;

                for (var i = 0; i <= last; i++)
                {
                    var param = propParams[i];

                    p.Print(param.Name);
                    p.PrintIf(i < last, ", ");
                }
            }
        }

        private void WriteEvents(ref Printer p)
        {
            foreach (var evt in Events)
            {
                if (evt.ExplicitInterfaceImplementations.Length > 0)
                {
                    continue;
                }

                WriteEvent(ref p, evt);
            }
        }

        private void WriteEvent(ref Printer p, IEventSymbol evt)
        {
            var name = evt.ToDisplayString(SymbolExtensions.QualifiedMemberNameWithGlobalPrefix);
            var returnTypeName = evt.Type.ToFullName();
            var isPublic = evt.DeclaredAccessibility == Accessibility.Public;

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLineIf(isPublic, "public ", "");
            p.PrintIf(evt.IsStatic, "static ");
            p.Print("event ");

            p.Print(returnTypeName);
            p.Print(" ");

            string explicitTypeName;

            if (evt.ExplicitInterfaceImplementations.Length > 0)
            {
                var explicitImpl = evt.ExplicitInterfaceImplementations[0];
                explicitTypeName = explicitImpl.ContainingType.ToFullName();

                p.Print(explicitTypeName).Print(".");
            }
            else
            {
                explicitTypeName = string.Empty;
            }

            p.PrintEndLine(name);
            p.OpenScope();
            {
                var fieldName = string.IsNullOrEmpty(explicitTypeName)
                    ? $"this.{FieldName}"
                    : $"(({explicitTypeName})this.{FieldName})";

                var accessor = evt.IsStatic ? returnTypeName : fieldName;

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintBeginLine("add => ").Print(accessor).Print(".").Print(name).PrintEndLine(" += value;");
                p.PrintEndLine();

                p.PrintLine(AGGRESSIVE_INLINING);
                p.PrintBeginLine("remove => ").Print(accessor).Print(".").Print(name).PrintEndLine(" -= value;");
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        private void WriteMethods(ref Printer p)
        {
            foreach (var method in Methods)
            {
                if (method.ExplicitInterfaceImplementations.Length > 0)
                {
                    continue;
                }

                WriteMethod(ref p, method);
            }

            WriteAdditionalMethods(ref p);
        }

        private void WriteAdditionalMethods(ref Printer p)
        {
            if (IgnoreInterfaces.HasFlag(InterfaceKind.Comparable) == false
                && ImplementInterfaces.HasFlag(InterfaceKind.Comparable)
            )
            {
                p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine("public ").PrintIf(IsStruct, "readonly ", "virtual ")
                    .Print("int CompareTo(").Print(FullTypeName).PrintEndLine(" other)");
                p = p.IncreasedIndent();
                {
                    p.PrintBeginLine("=> ").Print(FieldName).Print(".CompareTo(other.").Print(FieldName).PrintEndLine(");");
                }
                p = p.DecreasedIndent();
                p.PrintEndLine();
            }

            if (IsStruct == false && IsRecord)
            {
                return;
            }

            if (IgnoreInterfaces.HasFlag(InterfaceKind.Equatable) == false
                && (ImplementOperators.HasFlag(OperatorKind.Equal)
                || ImplementInterfaces.HasFlag(InterfaceKind.Equatable)
            ))
            {
                p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine("public ").PrintIf(IsStruct, "readonly ", "virtual ")
                    .Print("bool Equals(").Print(FullTypeName).PrintEndLine(" other)");
                p = p.IncreasedIndent();
                {
                    if (ImplementOperators.HasFlag(OperatorKind.Equal))
                    {
                        p.PrintBeginLine("=> ").Print(FieldName).Print(" == other.").Print(FieldName).PrintEndLine(";");
                    }
                    else
                    {
                        p.PrintBeginLine("=> ").Print(FieldName).Print(".Equals(other.").Print(FieldName).PrintEndLine(");");
                    }
                }
                p = p.DecreasedIndent();
                p.PrintEndLine();
            }

            if (_writenSpecialMethods.HasFlag(SpecialMethodType.GetHashCode) == false
                && ImplementSpecialMethods.HasFlag(SpecialMethodType.GetHashCode)
                && IsRefStruct == false
            )
            {
                p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine("public ").PrintIf(IsStruct, "readonly ").PrintEndLine("override int GetHashCode()");
                p = p.IncreasedIndent();
                {
                    p.PrintBeginLine("=> ").Print(FieldName).PrintEndLine(".GetHashCode();");
                }
                p = p.DecreasedIndent();
                p.PrintEndLine();
            }

            if (_writenSpecialMethods.HasFlag(SpecialMethodType.ToString) == false
                && ImplementSpecialMethods.HasFlag(SpecialMethodType.ToString)
                && IsRefStruct == false
            )
            {
                p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine("public ").PrintIf(IsStruct, "readonly ").PrintEndLine("override string ToString()");
                p = p.IncreasedIndent();
                {
                    p.PrintBeginLine("=> ").Print(FieldName).PrintEndLine(".ToString();");
                }
                p = p.DecreasedIndent();
                p.PrintEndLine();
            }

            if (IsRefStruct)
            {
                p.PrintLine(OBSOLETE);
                p.PrintLine("public override int GetHashCode() => throw null;");
                p.PrintEndLine();

                p.PrintLine(OBSOLETE);
                p.PrintLine("public override bool Equals(object other) => throw null;");
                p.PrintEndLine();
            }
        }

        private void WriteMethod(ref Printer p, IMethodSymbol method)
        {
            var methodName = method.ToDisplayString(SymbolExtensions.MemberName);
            var returnTypeName = method.ReturnType.ToFullName();
            var hasParams = method.Parameters.Length > 0;
            var isPublic = method.DeclaredAccessibility == Accessibility.Public;

            p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLineIf(isPublic, "public ", "");
            p.PrintIf(method.IsStatic, "static ");
            p.PrintIf(method.IsOverride, "override ");
            p.PrintIf(method.IsReadOnly, "readonly ");
            p.PrintIf(method.RefKind == RefKind.Ref, "ref ");
            p.PrintIf(method.RefKind == RefKind.RefReadOnly, "ref readonly ");
            p.PrintIf(method.ReturnsVoid, "void", returnTypeName);

            p.Print(" ");

            //string explicitTypeName;

            //if (method.ExplicitInterfaceImplementations.Length > 0)
            //{
            //    var explicitImpl = method.ExplicitInterfaceImplementations[0];
            //    explicitTypeName = explicitImpl.ContainingType.ToFullName();
            //    p.Print(explicitTypeName).Print(".");
            //}
            //else
            //{
            //    explicitTypeName = string.Empty;
            //}

            p.Print(methodName);
            WriteTypeParams(ref p, method);
            p.Print("(");

            if (hasParams)
            {
                var propParams = method.Parameters;
                var last = propParams.Length - 1;

                for (var i = 0; i <= last; i++)
                {
                    var param = propParams[i];
                    var paramTypeName = param.Type.ToFullName();

                    WriteInlineAttributes(ref p, param);
                    p.PrintIf(param.RefKind == RefKind.Ref, "ref ");
                    p.PrintIf(param.RefKind == RefKind.Out, "out ");
                    p.PrintIf(param.RefKind == RefKind.In, "in ");
                    p.Print(paramTypeName);
                    p.Print(" ");
                    p.Print(param.Name);
                    p.PrintIf(i < last, ", ");
                }
            }
            else
            {
                switch (methodName)
                {
                    case "GetHashCode":
                        _writenSpecialMethods |= SpecialMethodType.GetHashCode;
                        break;

                    case "ToString":
                        _writenSpecialMethods |= SpecialMethodType.ToString;
                        break;
                }
            }

            p.PrintEndLine(")");
            WriteTypeParamConstraints(ref p, method);
            p = p.IncreasedIndent();
            {
                p.PrintBeginLine("=> ");

                p.PrintIf(method.RefKind == RefKind.Ref, "ref ");
                p.PrintIf(method.RefKind == RefKind.RefReadOnly, "ref readonly ");

                if (method.IsStatic)
                {
                    p.Print(FieldTypeName);
                }
                else
                {
                    p.Print(FieldName);
                }

                p.Print(".").Print(method.Name);

                WriteTypeParams(ref p, method);

                p.Print("(");

                if (hasParams)
                {
                    var propParams = method.Parameters;
                    var last = propParams.Length - 1;

                    for (var i = 0; i <= last; i++)
                    {
                        var param = propParams[i];

                        p.PrintIf(param.RefKind == RefKind.Ref, "ref ");
                        p.PrintIf(param.RefKind == RefKind.Out, "out ");
                        p.PrintIf(param.RefKind == RefKind.In, "in ");
                        p.Print(param.Name);
                        p.PrintIf(i < last, ", ");
                    }
                }

                p.PrintEndLine(");");
            }
            p = p.DecreasedIndent();
            p.PrintEndLine();

            static void WriteTypeParams(ref Printer p, IMethodSymbol method)
            {
                var typeParams = method.TypeParameters;

                if (typeParams.Length < 1)
                {
                    return;
                }

                p.Print("<");

                var last = typeParams.Length - 1;

                for (var i = 0; i <= last; i++)
                {
                    var param = typeParams[i];

                    p.Print(param.Name);
                    p.PrintIf(i < last, ", ");
                }

                p.Print(">");
            }

            static void WriteTypeParamConstraints(ref Printer p, IMethodSymbol method)
            {
                var typeParams = method.TypeParameters;

                if (typeParams.Length < 1)
                {
                    return;
                }

                var last = typeParams.Length - 1;
                var constraints = new List<string>(10);

                for (var i = 0; i <= last; i++)
                {
                    constraints.Clear();

                    var param = typeParams[i];

                    if (param.HasReferenceTypeConstraint)
                    {
                        if (param.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
                        {
                            constraints.Add("class?");
                        }
                        else
                        {
                            constraints.Add("class");
                        }
                    }

                    if (param.HasValueTypeConstraint)
                    {
                        constraints.Add("struct");
                    }

                    if (param.HasUnmanagedTypeConstraint)
                    {
                        constraints.Add("unmanaged");
                    }

                    if (param.HasNotNullConstraint)
                    {
                        constraints.Add("notnull");
                    }

                    var constraintTypes = param.ConstraintTypes;
                    var constraintNullable = param.ConstraintNullableAnnotations;

                    for (var k = 0; k < constraintTypes.Length; k++)
                    {
                        var constraintType = constraintTypes[k];

                        if (constraintNullable[k] == NullableAnnotation.Annotated)
                        {
                            constraints.Add($"{constraintType.ToFullName()}?");
                        }
                        else
                        {
                            constraints.Add(constraintType.ToFullName());
                        }
                    }

                    if (param.HasConstructorConstraint)
                    {
                        constraints.Add("new()");
                    }

                    if (constraints.Count > 0)
                    {
                        p = p.IncreasedIndent();
                        p.PrintBeginLine("where ").Print(param.Name).Print(" : ");

                        var lastConstraint = constraints.Count - 1;

                        for (var k = 0; k <= lastConstraint; k++)
                        {
                            p.Print(constraints[k]);
                            p.PrintIf(k < lastConstraint, ", ");
                        }

                        p.PrintEndLine();
                        p = p.DecreasedIndent();
                    }
                }
            }
        }

        private void WriteInlineAttributes(ref Printer p, ISymbol symbol)
        {
            var attribs = symbol.GetAttributes();

            foreach (var attrib in attribs)
            {
                if (attrib.AttributeClass is INamedTypeSymbol namedType)
                {
                    p.Print("[").Print(namedType.ToFullName()).Print("]");
                }
            }
        }

        private void WriteConversionOperators(ref Printer p)
        {
            if (FieldTypeSymbol.TypeKind == TypeKind.Interface)
            {
                return;
            }

            p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLine("public static ").PrintIf(IsStruct, "implicit", "explicit")
                .Print(" operator ").Print(FullTypeName)
                .Print("(").Print(FieldTypeName).PrintEndLine(" value)");
            p = p.IncreasedIndent();
            {
                p.PrintBeginLine("=> new ").Print(FullTypeName).PrintEndLine("(value);");
            }
            p = p.DecreasedIndent();
            p.PrintEndLine();

            p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLine("public static implicit operator ").Print(FieldTypeName)
                .Print("(").Print(FullTypeName).PrintEndLine(" value)");
            p = p.IncreasedIndent();
            {
                p.PrintBeginLine("=> value.").Print(FieldName).PrintEndLine(";");
            }
            p = p.DecreasedIndent();
            p.PrintEndLine();
        }

        private void WriteOperators(ref Printer p)
        {
            var operatorKinds = s_operatorKinds;
            var ignoreOperators = IgnoreOperators;
            var implementOperators = ImplementOperators;
            var operatorReturnTypeMap = OperatorReturnTypeMap;
            var operatorArgTypesMap = OperatorArgTypesMap;
            var fullTypeName = FullTypeName;
            var fieldTypeSymbol = FieldTypeSymbol;
            var fieldSpecialType = fieldTypeSymbol.SpecialType;
            var fieldUnderlyingSpecialType = SpecialType.None;
            var fieldName = FieldName;

            if (fieldTypeSymbol.EnumUnderlyingType is INamedTypeSymbol underlyingTypeSymbol)
            {
                fieldSpecialType = SpecialType.System_Enum;
                fieldUnderlyingSpecialType = underlyingTypeSymbol.SpecialType;
            }

            foreach (var operatorKind in operatorKinds)
            {
                if (ignoreOperators.HasFlag(operatorKind)
                    || implementOperators.HasFlag(operatorKind) == false
                )
                {
                    continue;
                }

                if (operatorReturnTypeMap.TryGetValue(operatorKind, out var opReturnType) == false)
                {
                    opReturnType = new OpType(DetermineReturnType(operatorKind, fullTypeName), true);
                }

                if (operatorArgTypesMap.TryGetValue(operatorKind, out var opArgTypes) == false)
                {
                    opArgTypes = DetermineArgTypes(operatorKind, fullTypeName, fieldSpecialType, fieldUnderlyingSpecialType);
                }

                WriteOperator(
                      ref p
                    , operatorKind
                    , fullTypeName
                    , fieldName
                    , opReturnType
                    , opArgTypes
                    , fieldSpecialType
                );
            }

            if (fieldSpecialType == SpecialType.System_Enum)
            {
                WriteEnumOperators(ref p, fullTypeName, fieldUnderlyingSpecialType, fieldName);
            }
        }

        private static void WriteOperator(
              ref Printer p
            , OperatorKind kind
            , string fullTypeName
            , string fieldName
            , OpType opReturnType
            , OpArgTypes opArgTypes
            , SpecialType fieldSpecialType
        )
        {
            var (isValid, firstType, firstName, secondType, secondName) = opArgTypes;

            if (isValid == false)
            {
                return;
            }

            var op = GetOp(kind);

            p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintBeginLine("public static ").Print(opReturnType.Value).Print(" operator ").Print(op).Print("(");
            WriteArgs(ref p, opArgTypes);
            p.PrintEndLine(")");
            p.OpenScope();
            {
                switch (kind)
                {
                    case OperatorKind.UnaryPlus:
                    case OperatorKind.UnaryMinus:
                    case OperatorKind.Negation:
                    case OperatorKind.OnesComplement:
                    {
                        p.PrintBeginLine("return ");

                        if (opReturnType.IsWrapper)
                        {
                            p.Print("new ").Print(fullTypeName).Print("(").Print(op).Print("(");
                            WriteParam(ref p, firstType, firstName, fieldName);
                            p.Print("))");
                        }
                        else
                        {
                            p.Print(op).Print("(");
                            WriteParam(ref p, firstType, firstName, fieldName);
                            p.Print(")");
                        }

                        p.PrintEndLine(";");
                        break;
                    }

                    case OperatorKind.Increment:
                    case OperatorKind.Decrement:
                    {
                        p.PrintBeginLine("var tempValue = ");
                        WriteParam(ref p, firstType, firstName, fieldName);
                        p.PrintEndLine(";");

                        p.PrintBeginLine("tempValue ")
                            .PrintIf(kind == OperatorKind.Increment, "++", "--")
                            .PrintEndLine(";");

                        if (opReturnType.IsWrapper)
                        {
                            p.PrintBeginLine("return new ").Print(fullTypeName).PrintEndLine("(tempValue);");
                        }
                        else
                        {
                            p.PrintLine("return tempValue;");
                        }
                        break;
                    }

                    case OperatorKind.True:
                    case OperatorKind.False:
                    {
                        p.PrintBeginLine("return ");
                        WriteParam(ref p, firstType, firstName, fieldName);
                        p.PrintEndLine(";");
                        break;
                    }

                    case OperatorKind.Addition:
                    {
                        if (fieldSpecialType != SpecialType.System_Enum)
                        {
                            goto case OperatorKind.Substraction;
                        }

                        p.PrintBeginLine("return ");

                        if (opReturnType.IsWrapper)
                        {
                            p.Print("new ").Print(fullTypeName).Print("(");
                            WriteParam(ref p, firstType, firstName, fieldName);
                            p.Print(" ").Print(op).Print(" ").Print(secondName)
                                .Print(")");
                        }
                        else
                        {
                            WriteParam(ref p, firstType, firstName, fieldName);
                            p.Print(" ").Print(op).Print(" ")
                                .Print(secondName);
                        }

                        p.PrintEndLine(";");
                        break;
                    }

                    case OperatorKind.Substraction:
                    case OperatorKind.Multiplication:
                    case OperatorKind.Division:
                    case OperatorKind.Remainder:
                    case OperatorKind.BitwiseAnd:
                    case OperatorKind.BitwiseOr:
                    case OperatorKind.BitwiseXor:
                    {
                        p.PrintBeginLine("return ");

                        if (opReturnType.IsWrapper)
                        {
                            p.Print("new ").Print(fullTypeName).Print("(");
                            WriteParam(ref p, firstType, firstName, fieldName);
                            p.Print(" ").Print(op).Print(" ");
                            WriteParam(ref p, secondType, secondName, fieldName);
                            p.Print(")");
                        }
                        else
                        {
                            WriteParam(ref p, firstType, firstName, fieldName);
                            p.Print(" ").Print(op).Print(" ");
                            WriteParam(ref p, secondType, secondName, fieldName);
                        }

                        p.PrintEndLine(";");
                        break;
                    }

                    case OperatorKind.LogicalAnd:
                    case OperatorKind.LogicalOr:
                    case OperatorKind.LogicalXor:
                    case OperatorKind.Equal:
                    case OperatorKind.NotEqual:
                    case OperatorKind.Greater:
                    case OperatorKind.Lesser:
                    case OperatorKind.GreaterEqual:
                    case OperatorKind.LesserEqual:
                    {
                        p.PrintBeginLine("return ");
                        WriteParam(ref p, firstType, firstName, fieldName);
                        p.Print(" ").Print(op).Print(" ");
                        WriteParam(ref p, secondType, secondName, fieldName);
                        p.PrintEndLine(";");
                        break;
                    }

                    case OperatorKind.LeftShift:
                    case OperatorKind.RightShift:
                    case OperatorKind.UnsignedRightShift:
                    {
                        p.PrintBeginLine("return ");

                        if (opReturnType.IsWrapper)
                        {
                            p.Print("new ").Print(fullTypeName).Print("(");
                            WriteParam(ref p, firstType, firstName, fieldName);
                            p.Print(" ").Print(op).Print(" ");
                            WriteParam(ref p, secondType, secondName, fieldName);
                            p.Print(")");
                        }
                        else
                        {
                            WriteParam(ref p, firstType, firstName, fieldName);
                            p.Print(" ").Print(op).Print(" ");
                            WriteParam(ref p, secondType, secondName, fieldName);
                        }

                        p.PrintEndLine(";");
                        break;
                    }
                }
            }
            p.CloseScope();
            p.PrintEndLine();

            static void WriteArgs(ref Printer p, OpArgTypes opArgTypes)
            {
                var (isValid, firstType, firstName, secondType, secondName) = opArgTypes;

                if (isValid == false)
                {
                    return;
                }

                if (string.IsNullOrEmpty(secondName))
                {
                    WriteArg(ref p, firstType, firstName);
                }
                else
                {
                    WriteArg(ref p, firstType, firstName);
                    p.Print(", ");
                    WriteArg(ref p, secondType, secondName);
                }
            }

            static void WriteArg(ref Printer p, OpType type, string name)
            {
                p.Print(type.Value).Print(" ").Print(name);
            }

            static void WriteParam(ref Printer p, OpType type, string name, string fieldName)
            {
                p.Print(name);

                if (type.IsWrapper)
                {
                    p.Print(".").Print(fieldName);
                }
            }
        }

        private void WriteEnumOperators(
              ref Printer p
            , string fullTypeName
            , SpecialType fieldUnderlyingSpecialType
            , string fieldName
        )
        {
            var map = OperatorReturnTypeMap;

            {
                var kind = OperatorKind.Substraction;
                var op = GetOp(kind);

                if (map.TryGetValue(kind, out var opReturnType) == false)
                {
                    opReturnType = new OpType(DetermineReturnType(kind, fullTypeName), true);
                }

                var args = fieldUnderlyingSpecialType switch {
                    SpecialType.System_SByte => $"{fullTypeName} left, sbyte right",
                    SpecialType.System_Byte => $"{fullTypeName} left, byte right",
                    SpecialType.System_Int16 => $"{fullTypeName} left, short right",
                    SpecialType.System_UInt16 => $"{fullTypeName} left, ushort right",
                    SpecialType.System_Int32 => $"{fullTypeName} left, int right",
                    SpecialType.System_UInt32 => $"{fullTypeName} left, uint right",
                    SpecialType.System_Int64 => $"{fullTypeName} left, long right",
                    SpecialType.System_UInt64 => $"{fullTypeName} left, ulong right",
                    _ => $"{fullTypeName} left, sbyte right",
                };

                p.PrintLine(AGGRESSIVE_INLINING).PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintBeginLine("public static ").Print(opReturnType.Value).Print(" operator ").Print(op)
                    .Print("(").Print(args).PrintEndLine(")");
                p.OpenScope();
                {
                    p.PrintBeginLine("return ");

                    if (opReturnType.IsWrapper)
                    {
                        p.Print("new ").Print(fullTypeName).Print("(")
                            .Print("left.").Print(fieldName)
                            .Print(" ").Print(op).Print(" ")
                            .Print("right")
                            .Print(")");
                    }
                    else
                    {
                        p.Print("left.").Print(fieldName)
                        .Print(" ").Print(op).Print(" ")
                        .Print("right");
                    }

                    p.PrintEndLine(";");
                }
                p.CloseScope();
                p.PrintEndLine();
            }
        }

        private static string GetOp(OperatorKind kind)
        {
            return kind switch {
                OperatorKind.UnaryPlus => "+",
                OperatorKind.UnaryMinus => "-",
                OperatorKind.Negation => "!",
                OperatorKind.OnesComplement => "~",
                OperatorKind.Increment => "++",
                OperatorKind.Decrement => "--",
                OperatorKind.True => "true",
                OperatorKind.False => "false",
                OperatorKind.Addition => "+",
                OperatorKind.Substraction => "-",
                OperatorKind.Multiplication => "*",
                OperatorKind.Division => "/",
                OperatorKind.Remainder => "%",
                OperatorKind.LogicalAnd => "&",
                OperatorKind.LogicalOr => "|",
                OperatorKind.LogicalXor => "^",
                OperatorKind.BitwiseAnd => "&",
                OperatorKind.BitwiseOr => "|",
                OperatorKind.BitwiseXor => "^",
                OperatorKind.LeftShift => "<<",
                OperatorKind.RightShift => ">>",
                OperatorKind.UnsignedRightShift => ">>>",
                OperatorKind.Equal => "==",
                OperatorKind.NotEqual => "!=",
                OperatorKind.Greater => ">",
                OperatorKind.Lesser => "<",
                OperatorKind.GreaterEqual => ">=",
                OperatorKind.LesserEqual => "<=",
                _ => string.Empty,
            };
        }

        private void WriteTypeConverter(ref Printer p)
        {
            if (ExcludeConverter || IsRefStruct)
            {
                return;
            }

            p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
            p.PrintLine($"private sealed class {TypeName}TypeConverter : global::System.ComponentModel.TypeConverter");
            p.OpenScope();
            {
                p.PrintLine($"private static readonly global::System.Type s_wrapperType = typeof({FullTypeName});");
                p.PrintLine($"private static readonly global::System.Type s_valueType = typeof({FieldTypeName});");

                p.PrintEndLine();

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public override bool CanConvertFrom(global::System.ComponentModel.ITypeDescriptorContext context, global::System.Type sourceType)");
                p.OpenScope();
                {
                    p.PrintLine("if (sourceType == s_wrapperType || sourceType == s_valueType) return true;");
                    p.PrintLine("return base.CanConvertFrom(context, sourceType);");
                }
                p.CloseScope();

                p.PrintEndLine();

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public override bool CanConvertTo(global::System.ComponentModel.ITypeDescriptorContext context, global::System.Type destinationType)");
                p.OpenScope();
                {
                    p.PrintLine($"if (destinationType == s_wrapperType || destinationType == s_valueType) return true;");
                    p.PrintLine($"return base.CanConvertTo(context, destinationType);");
                }
                p.CloseScope();

                p.PrintEndLine();

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public override object ConvertFrom(global::System.ComponentModel.ITypeDescriptorContext context, global::System.Globalization.CultureInfo culture, object value)");
                p.OpenScope();
                {
                    p.PrintLine("if (value != null)");
                    p.OpenScope();
                    {
                        p.PrintLine("var t = value.GetType();");
                        p.PrintLine($"if (t == typeof({FullTypeName})) return ({FullTypeName})value;");
                        p.PrintLine($"if (t == typeof({FieldTypeName})) return new {FullTypeName}(({FieldTypeName})value);");
                    }
                    p.CloseScope();

                    p.PrintLine("return base.ConvertFrom(context, culture, value);");
                }
                p.CloseScope();

                p.PrintEndLine();

                p.PrintLine(GENERATED_CODE).PrintLine(EXCLUDE_COVERAGE);
                p.PrintLine($"public override object ConvertTo(global::System.ComponentModel.ITypeDescriptorContext context, global::System.Globalization.CultureInfo culture, object value, global::System.Type destinationType)");
                p.OpenScope();
                {
                    p.PrintLine($"if (value is {FullTypeName} wrappedValue)");
                    p.OpenScope();
                    {
                        p.PrintLine("if (destinationType == s_wrapperType) return wrappedValue;");
                        p.PrintLine($"if (destinationType == s_valueType) return wrappedValue.{FieldName};");
                    }
                    p.CloseScope();

                    p.PrintLine("return base.ConvertTo(context, culture, value, destinationType);");
                }
                p.CloseScope();
            }
            p.CloseScope();
            p.PrintEndLine();
        }

        /*
        private void PrintDebug(ref Printer p, INamedTypeSymbol typeSymbol)
        {
            var members = typeSymbol.GetMembers();
            var interfaces = typeSymbol.AllInterfaces;
            var memberName = SymbolExtensions.QualifiedMemberNameWithGlobalPrefix;
            var globalFormat = SymbolExtensions.QualifiedMemberFormatWithGlobalPrefix;
            var memberFormat = SymbolExtensions.QualifiedMemberFormatWithType;
            var typeFormat = SymbolExtensions.QualifiedFormatWithoutGlobalPrefix;

            foreach (var member in members)
            {
                var attribs = member.GetAttributes();

                foreach (var attrib in attribs)
                {
                    if (attrib.AttributeClass is not INamedTypeSymbol attribTypeSymbol) continue;

                    p.PrintBeginLine("//A:: [").Print(attribTypeSymbol.ToDisplayString(typeFormat)).PrintEndLine("]");
                }

                p.PrintBeginLine("//D:: ").PrintEndLine(member.IsImplicitlyDeclared.ToString());

                switch (member)
                {
                    case IFieldSymbol field:
                    {
                        if (field.DeclaredAccessibility == Accessibility.Public)
                        {
                            p.PrintBeginLine("//F:: ").PrintEndLine(field.Name);
                            p.PrintBeginLine("//    ").PrintEndLine(field.ToDisplayString(memberName));
                            p.PrintBeginLine("//    ").PrintEndLine(field.ToDisplayString(globalFormat));
                            p.PrintBeginLine("//    ").PrintEndLine(field.ToDisplayString(memberFormat));
                            p.PrintEndLine();
                        }
                        break;
                    }

                    case IPropertySymbol property:
                    {
                        if (property.DeclaredAccessibility == Accessibility.Public
                            || property.ExplicitInterfaceImplementations.Length > 0
                        )
                        {
                            p.PrintBeginLine("//P:: ").PrintEndLine(property.Name);
                            p.PrintBeginLine("//    ").PrintEndLine(property.ToDisplayString(memberName));
                            p.PrintBeginLine("//    ").PrintEndLine(property.ToDisplayString(globalFormat));
                            p.PrintBeginLine("//    ").PrintEndLine(property.ToDisplayString(memberFormat));

                            var explicitImpls = property.ExplicitInterfaceImplementations;

                            for (var i = 0; i < explicitImpls.Length; i++)
                            {
                                var explicitImpl = explicitImpls[i];

                                p.PrintBeginLine("//~:: ").Print($"{i} :: ").PrintEndLine(explicitImpl.Name);
                                p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(memberName));
                                p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(globalFormat));
                                p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(memberFormat));
                            }

                            var interfaceMembers = property.FindInterfaceMembers(interfaces);

                            for (var i = 0; i < interfaceMembers.Length; i++)
                            {
                                var m = interfaceMembers[i];

                                p.PrintBeginLine("//>:: ").Print($"{i} :: ").PrintEndLine(m.ToDisplayString(memberName));
                                p.PrintBeginLine("//    ").PrintEndLine(m.ToDisplayString(globalFormat));
                                p.PrintBeginLine("//    ").PrintEndLine(m.ToDisplayString(memberFormat));
                                p.PrintBeginLine("//T:: ").PrintEndLine(m.ContainingType.ToDisplayString(typeFormat));
                            }

                            p.PrintEndLine();
                        }
                        break;
                    }

                    case IEventSymbol @event:
                    {
                        if (@event.DeclaredAccessibility == Accessibility.Public
                            || @event.ExplicitInterfaceImplementations.Length > 0
                        )
                        {
                            p.PrintBeginLine("//E:: ").PrintEndLine(@event.Name);
                            p.PrintBeginLine("//    ").PrintEndLine(@event.ToDisplayString(memberName));
                            p.PrintBeginLine("//    ").PrintEndLine(@event.ToDisplayString(globalFormat));
                            p.PrintBeginLine("//    ").PrintEndLine(@event.ToDisplayString(memberFormat));

                            var explicitImpls = @event.ExplicitInterfaceImplementations;

                            for (var i = 0; i < explicitImpls.Length; i++)
                            {
                                var explicitImpl = explicitImpls[i];
                                p.PrintBeginLine("//~:: ").Print($"{i} :: ").PrintEndLine(explicitImpl.Name);
                                p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(memberName));
                                p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(globalFormat));
                                p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(memberFormat));
                            }

                            var interfaceMembers = @event.FindInterfaceMembers(interfaces);

                            for (var i = 0; i < interfaceMembers.Length; i++)
                            {
                                var m = interfaceMembers[i];

                                p.PrintBeginLine("//>:: ").Print($"{i} :: ").PrintEndLine(m.ToDisplayString(memberName));
                                p.PrintBeginLine("//    ").PrintEndLine(m.ToDisplayString(globalFormat));
                                p.PrintBeginLine("//    ").PrintEndLine(m.ToDisplayString(memberFormat));
                                p.PrintBeginLine("//T:: ").PrintEndLine(m.ContainingType.ToDisplayString(typeFormat));
                            }

                            p.PrintEndLine();
                        }
                        break;
                    }

                    case IMethodSymbol method:
                    {
                        if (method.DeclaredAccessibility == Accessibility.Public
                            || method.ExplicitInterfaceImplementations.Length > 0
                        )
                        {
                            if (method.MethodKind == MethodKind.Constructor)
                            {
                                p.PrintBeginLine("//C:: ").PrintEndLine(method.Name);
                                p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(memberName));
                                p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(globalFormat));
                                p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(memberFormat));
                                p.PrintEndLine();
                                continue;
                            }

                            if (NotSupported(method))
                            {
                                continue;
                            }

                            var explicitImpls = method.ExplicitInterfaceImplementations;

                            if (explicitImpls.Length > 0)
                            {
                                for (var i = 0; i < explicitImpls.Length; i++)
                                {
                                    var explicitImpl = explicitImpls[i];

                                    if (NotSupported(explicitImpl))
                                    {
                                        continue;
                                    }

                                    p.PrintBeginLine("//M:: ").Print($"{i} :: ").PrintEndLine(method.Name);
                                    p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(memberName));
                                    p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(globalFormat));
                                    p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(memberFormat));
                                    p.PrintBeginLine("//~:: ").PrintEndLine(explicitImpl.Name);
                                    p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(memberName));
                                    p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(globalFormat));
                                    p.PrintBeginLine("//    ").PrintEndLine(explicitImpl.ToDisplayString(memberFormat));
                                }
                            }
                            else
                            {
                                p.PrintBeginLine("//M:: ").PrintEndLine(method.Name);
                                p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(memberName));
                                p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(globalFormat));
                                p.PrintBeginLine("//    ").PrintEndLine(method.ToDisplayString(memberFormat));

                            }

                            var interfaceMembers = method.FindInterfaceMembers(interfaces);

                            for (var i = 0; i < interfaceMembers.Length; i++)
                            {
                                var m = interfaceMembers[i];

                                p.PrintBeginLine("//>:: ").Print($"{i} :: ").PrintEndLine(m.ToDisplayString(memberName));
                                p.PrintBeginLine("//    ").PrintEndLine(m.ToDisplayString(globalFormat));
                                p.PrintBeginLine("//    ").PrintEndLine(m.ToDisplayString(memberFormat));
                                p.PrintBeginLine("//T:: ").PrintEndLine(m.ContainingType.ToDisplayString(typeFormat));
                            }

                            p.PrintEndLine();
                        }
                        break;
                    }
                }
            }
        }
        */
    }
}

// <copyright file="ReactiveModelGenerator.cs" company="AillieoTech">
// Copyright (c) AillieoTech. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace AillieoUtils.Reactive.SourceGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;

    [Generator]
    public sealed class ReactiveModelGenerator : ISourceGenerator
    {
        private const string AttributeMetadataName = "AillieoUtils.Reactive.ReactiveAttribute";
        private const string ModelAttributeMetadataName = "AillieoUtils.Reactive.ReactiveModelAttribute";
        private const string NonReactiveAttributeMetadataName = "AillieoUtils.Reactive.NonReactiveAttribute";
        private const string StateFieldName = "__reactiveModelState";

        private static readonly DiagnosticDescriptor PartialTypeRequired = new DiagnosticDescriptor(
            "RXSG001",
            "Reactive fields require a partial class",
            "Type '{0}' and all of its containing types must be partial classes",
            "Reactive.SourceGenerator",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor InstanceMutableFieldRequired = new DiagnosticDescriptor(
            "RXSG002",
            "Reactive field must be mutable and instance-based",
            "Field '{0}' must not be static, const, or readonly",
            "Reactive.SourceGenerator",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor InvalidPropertyName = new DiagnosticDescriptor(
            "RXSG003",
            "Invalid generated property name",
            "'{0}' is not a valid generated property name for field '{1}'",
            "Reactive.SourceGenerator",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor MemberCollision = new DiagnosticDescriptor(
            "RXSG004",
            "Generated member name collides with an existing member",
            "Generated member '{0}' for field '{1}' conflicts with another member on '{2}'",
            "Reactive.SourceGenerator",
            DiagnosticSeverity.Error,
            true);

        private static readonly DiagnosticDescriptor ConflictingFieldAttributes = new DiagnosticDescriptor(
            "RXSG005",
            "Reactive field attributes conflict",
            "Field '{0}' cannot use both ReactiveAttribute and NonReactiveAttribute",
            "Reactive.SourceGenerator",
            DiagnosticSeverity.Error,
            true);

        private static readonly SymbolDisplayFormat FullyQualifiedTypeFormat = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new Receiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var receiver = context.SyntaxReceiver as Receiver;
            if (receiver == null || (receiver.Fields.Count == 0 && receiver.Types.Count == 0))
            {
                return;
            }

            var attributeType = context.Compilation.GetTypeByMetadataName(AttributeMetadataName);
            var modelAttributeType = context.Compilation.GetTypeByMetadataName(ModelAttributeMetadataName);
            var nonReactiveAttributeType = context.Compilation.GetTypeByMetadataName(NonReactiveAttributeMetadataName);
            if (attributeType == null || modelAttributeType == null || nonReactiveAttributeType == null)
            {
                return;
            }

            var groups = new Dictionary<INamedTypeSymbol, List<FieldModel>>(SymbolEqualityComparer.Default);
            var processedFields = new HashSet<IFieldSymbol>(SymbolEqualityComparer.Default);
            foreach (var declaration in receiver.Fields)
            {
                var semanticModel = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
                foreach (var variable in declaration.Declaration.Variables)
                {
                    var field = semanticModel.GetDeclaredSymbol(variable, context.CancellationToken) as IFieldSymbol;
                    if (field == null)
                    {
                        continue;
                    }

                    var attribute = FindAttribute(field, attributeType);
                    if (attribute == null)
                    {
                        continue;
                    }

                    processedFields.Add(field);
                    if (FindAttribute(field, nonReactiveAttributeType) != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            ConflictingFieldAttributes,
                            variable.GetLocation(),
                            field.Name));
                        continue;
                    }

                    TryAddField(context, groups, field, attribute, variable.GetLocation());
                }
            }

            var processedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var declaration in receiver.Types)
            {
                var semanticModel = context.Compilation.GetSemanticModel(declaration.SyntaxTree);
                var type = semanticModel.GetDeclaredSymbol(declaration, context.CancellationToken);
                if (type == null ||
                    !processedTypes.Add(type) ||
                    FindAttribute(type, modelAttributeType) == null)
                {
                    continue;
                }

                foreach (var field in type.GetMembers().OfType<IFieldSymbol>())
                {
                    if (!processedFields.Add(field) ||
                        field.IsImplicitlyDeclared ||
                        field.AssociatedSymbol != null)
                    {
                        continue;
                    }

                    var reactiveAttribute = FindAttribute(field, attributeType);
                    var nonReactiveAttribute = FindAttribute(field, nonReactiveAttributeType);
                    if (reactiveAttribute != null && nonReactiveAttribute != null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            ConflictingFieldAttributes,
                            field.Locations.FirstOrDefault(),
                            field.Name));
                        continue;
                    }

                    if (nonReactiveAttribute != null ||
                        field.IsStatic ||
                        field.IsConst ||
                        field.IsReadOnly)
                    {
                        continue;
                    }

                    TryAddField(
                        context,
                        groups,
                        field,
                        reactiveAttribute,
                        field.Locations.FirstOrDefault());
                }
            }

            foreach (var group in groups.OrderBy(item => item.Key.ToDisplayString(), StringComparer.Ordinal))
            {
                GenerateType(context, group.Key, group.Value);
            }
        }

        private static AttributeData FindAttribute(ISymbol symbol, INamedTypeSymbol attributeType)
        {
            return symbol.GetAttributes().FirstOrDefault(candidate =>
                SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, attributeType));
        }

        private static void TryAddField(
            GeneratorExecutionContext context,
            Dictionary<INamedTypeSymbol, List<FieldModel>> groups,
            IFieldSymbol field,
            AttributeData attribute,
            Location location)
        {
            if (!TryCreateFieldModel(context, field, attribute, location, out var model))
            {
                return;
            }

            if (!groups.TryGetValue(field.ContainingType, out var fields))
            {
                fields = new List<FieldModel>();
                groups.Add(field.ContainingType, fields);
            }

            fields.Add(model);
        }

        private static bool TryCreateFieldModel(
            GeneratorExecutionContext context,
            IFieldSymbol field,
            AttributeData attribute,
            Location location,
            out FieldModel model)
        {
            model = null;
            if (field.IsStatic || field.IsConst || field.IsReadOnly)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InstanceMutableFieldRequired,
                    location,
                    field.Name));
                return false;
            }

            if (!IsPartialClassHierarchy(field.ContainingType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    PartialTypeRequired,
                    field.ContainingType.Locations.FirstOrDefault() ?? location,
                    field.ContainingType.ToDisplayString()));
                return false;
            }

            string propertyName = null;
            if (attribute != null &&
                attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Value is string explicitName)
            {
                propertyName = explicitName;
            }

            propertyName = string.IsNullOrWhiteSpace(propertyName)
                ? InferPropertyName(field.Name)
                : propertyName;
            if (!SyntaxFacts.IsValidIdentifier(propertyName) ||
                SyntaxFacts.GetKeywordKind(propertyName) != SyntaxKind.None)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    InvalidPropertyName,
                    location,
                    propertyName ?? string.Empty,
                    field.Name));
                return false;
            }

            model = new FieldModel(
                field,
                propertyName,
                field.Type.ToDisplayString(FullyQualifiedTypeFormat),
                location);
            return true;
        }

        private static void GenerateType(
            GeneratorExecutionContext context,
            INamedTypeSymbol type,
            List<FieldModel> fields)
        {
            fields.Sort((left, right) => CompareLocations(left.Location, right.Location));
            var valid = new List<FieldModel>(fields.Count);
            var generatedNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                string propertyAdapterName = field.PropertyName + "Property";
                bool collides = type.GetMembers(field.PropertyName).Length != 0 ||
                    type.GetMembers(propertyAdapterName).Length != 0 ||
                    !generatedNames.Add(field.PropertyName) ||
                    !generatedNames.Add(propertyAdapterName);
                if (collides)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MemberCollision,
                        field.Location,
                        field.PropertyName,
                        field.Symbol.Name,
                        type.ToDisplayString()));
                    continue;
                }

                valid.Add(field);
            }

            if (valid.Count == 0)
            {
                return;
            }

            if (type.GetMembers(StateFieldName).Length != 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MemberCollision,
                    type.Locations.FirstOrDefault(),
                    StateFieldName,
                    valid[0].Symbol.Name,
                    type.ToDisplayString()));
                return;
            }

            var source = new StringBuilder();
            source.AppendLine("// <auto-generated />");
            source.AppendLine("#nullable disable");

            string namespaceName = type.ContainingNamespace.IsGlobalNamespace
                ? null
                : type.ContainingNamespace.ToDisplayString();
            int indent = 0;
            if (namespaceName != null)
            {
                source.Append("namespace ").Append(namespaceName).AppendLine();
                source.AppendLine("{");
                indent = 1;
            }

            var hierarchy = GetTypeHierarchy(type);
            foreach (var current in hierarchy)
            {
                AppendIndent(source, indent);
                source.Append("partial class ").Append(EscapeIdentifier(current.Name));
                AppendTypeParameters(source, current);
                source.AppendLine();
                AppendIndent(source, indent);
                source.AppendLine("{");
                ++indent;
            }

            AppendIndent(source, indent);
            source.Append("private readonly global::AillieoUtils.Reactive.ReactiveModelState ")
                .Append(StateFieldName)
                .Append(" = new global::AillieoUtils.Reactive.ReactiveModelState(")
                .Append(valid.Count)
                .AppendLine(");");

            for (int index = 0; index < valid.Count; ++index)
            {
                AppendField(source, indent, valid[index], index);
            }

            for (int index = hierarchy.Count - 1; index >= 0; --index)
            {
                --indent;
                AppendIndent(source, indent);
                source.AppendLine("}");
            }

            if (namespaceName != null)
            {
                source.AppendLine("}");
            }

            context.AddSource(CreateHintName(type), SourceText.From(source.ToString(), Encoding.UTF8));
        }

        private static void AppendField(StringBuilder source, int indent, FieldModel field, int slot)
        {
            string escapedFieldName = EscapeIdentifier(field.Symbol.Name);
            string adapterFieldName = "__reactiveProperty" + slot;

            source.AppendLine();
            AppendIndent(source, indent);
            source.Append("private global::AillieoUtils.Reactive.IReadOnlyReactiveProperty<")
                .Append(field.TypeName)
                .Append("> ")
                .Append(adapterFieldName)
                .AppendLine(";");

            source.AppendLine();
            AppendIndent(source, indent);
            source.Append("public ").Append(field.TypeName).Append(' ').Append(field.PropertyName).AppendLine();
            AppendIndent(source, indent);
            source.AppendLine("{");
            AppendIndent(source, indent + 1);
            source.AppendLine("get");
            AppendIndent(source, indent + 1);
            source.AppendLine("{");
            AppendIndent(source, indent + 2);
            source.Append(StateFieldName).Append(".Track(").Append(slot).AppendLine(");");
            AppendIndent(source, indent + 2);
            source.Append("return this.").Append(escapedFieldName).AppendLine(";");
            AppendIndent(source, indent + 1);
            source.AppendLine("}");
            AppendIndent(source, indent + 1);
            source.AppendLine("set");
            AppendIndent(source, indent + 1);
            source.AppendLine("{");
            AppendIndent(source, indent + 2);
            source.Append("if (global::System.Collections.Generic.EqualityComparer<")
                .Append(field.TypeName)
                .Append(">.Default.Equals(this.")
                .Append(escapedFieldName)
                .AppendLine(", value))");
            AppendIndent(source, indent + 2);
            source.AppendLine("{");
            AppendIndent(source, indent + 3);
            source.AppendLine("return;");
            AppendIndent(source, indent + 2);
            source.AppendLine("}");
            source.AppendLine();
            AppendIndent(source, indent + 2);
            source.Append("var oldValue = this.").Append(escapedFieldName).AppendLine(";");
            AppendIndent(source, indent + 2);
            source.Append("this.On").Append(field.PropertyName).AppendLine("Changing(oldValue, value);");
            AppendIndent(source, indent + 2);
            source.Append("this.").Append(escapedFieldName).AppendLine(" = value;");
            AppendIndent(source, indent + 2);
            source.Append("this.On").Append(field.PropertyName).AppendLine("Changed(oldValue, value);");
            AppendIndent(source, indent + 2);
            source.Append(StateFieldName).Append(".Notify(").Append(slot).AppendLine(");");
            AppendIndent(source, indent + 1);
            source.AppendLine("}");
            AppendIndent(source, indent);
            source.AppendLine("}");

            source.AppendLine();
            AppendIndent(source, indent);
            source.Append("public global::AillieoUtils.Reactive.IReadOnlyReactiveProperty<")
                .Append(field.TypeName)
                .Append("> ")
                .Append(field.PropertyName)
                .AppendLine("Property");
            AppendIndent(source, indent);
            source.AppendLine("{");
            AppendIndent(source, indent + 1);
            source.AppendLine("get");
            AppendIndent(source, indent + 1);
            source.AppendLine("{");
            AppendIndent(source, indent + 2);
            source.Append("return this.").Append(adapterFieldName).Append(" ?? (this.")
                .Append(adapterFieldName).Append(" = ").Append(StateFieldName)
                .Append(".GetProperty<").Append(field.TypeName).Append(">(")
                .Append(slot).Append(", () => this.").Append(escapedFieldName).AppendLine("));");
            AppendIndent(source, indent + 1);
            source.AppendLine("}");
            AppendIndent(source, indent);
            source.AppendLine("}");

            source.AppendLine();
            AppendIndent(source, indent);
            source.Append("partial void On").Append(field.PropertyName).Append("Changing(")
                .Append(field.TypeName).Append(" oldValue, ").Append(field.TypeName).AppendLine(" newValue);");
            AppendIndent(source, indent);
            source.Append("partial void On").Append(field.PropertyName).Append("Changed(")
                .Append(field.TypeName).Append(" oldValue, ").Append(field.TypeName).AppendLine(" newValue);");
        }

        private static bool IsPartialClassHierarchy(INamedTypeSymbol type)
        {
            for (var current = type; current != null; current = current.ContainingType)
            {
                if (current.TypeKind != TypeKind.Class || current.DeclaringSyntaxReferences.Length == 0)
                {
                    return false;
                }

                foreach (var reference in current.DeclaringSyntaxReferences)
                {
                    var declaration = reference.GetSyntax() as ClassDeclarationSyntax;
                    if (declaration == null || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static string InferPropertyName(string fieldName)
        {
            string name = fieldName;
            if (name.StartsWith("m_", StringComparison.Ordinal) && name.Length > 2)
            {
                name = name.Substring(2);
            }
            else
            {
                name = name.TrimStart('_');
            }

            if (name.Length == 0)
            {
                return string.Empty;
            }

            if (name.Length == 1)
            {
                return char.ToUpperInvariant(name[0]).ToString(CultureInfo.InvariantCulture);
            }

            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private static List<INamedTypeSymbol> GetTypeHierarchy(INamedTypeSymbol type)
        {
            var hierarchy = new List<INamedTypeSymbol>();
            for (var current = type; current != null; current = current.ContainingType)
            {
                hierarchy.Add(current);
            }

            hierarchy.Reverse();
            return hierarchy;
        }

        private static void AppendTypeParameters(StringBuilder source, INamedTypeSymbol type)
        {
            if (type.TypeParameters.Length == 0)
            {
                return;
            }

            source.Append('<');
            for (int index = 0; index < type.TypeParameters.Length; ++index)
            {
                if (index != 0)
                {
                    source.Append(", ");
                }

                source.Append(EscapeIdentifier(type.TypeParameters[index].Name));
            }

            source.Append('>');
        }

        private static int CompareLocations(Location left, Location right)
        {
            string leftPath = left.SourceTree?.FilePath ?? string.Empty;
            string rightPath = right.SourceTree?.FilePath ?? string.Empty;
            int path = StringComparer.Ordinal.Compare(leftPath, rightPath);
            return path != 0 ? path : left.SourceSpan.Start.CompareTo(right.SourceSpan.Start);
        }

        private static string EscapeIdentifier(string identifier)
        {
            return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
        }

        private static void AppendIndent(StringBuilder source, int indent)
        {
            source.Append(' ', indent * 4);
        }

        private static string CreateHintName(INamedTypeSymbol type)
        {
            string name = type.ToDisplayString(FullyQualifiedTypeFormat);
            var builder = new StringBuilder(name.Length + 32);
            uint hash = 2166136261;
            foreach (char character in name)
            {
                hash = (hash ^ character) * 16777619;
                builder.Append(char.IsLetterOrDigit(character) ? character : '_');
            }

            builder.Append('_').Append(hash.ToString("X8", CultureInfo.InvariantCulture)).Append(".ReactiveModel.g.cs");
            return builder.ToString();
        }

        private sealed class Receiver : ISyntaxReceiver
        {
            internal List<FieldDeclarationSyntax> Fields { get; } = new List<FieldDeclarationSyntax>();

            internal List<ClassDeclarationSyntax> Types { get; } = new List<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is FieldDeclarationSyntax field && field.AttributeLists.Count != 0)
                {
                    this.Fields.Add(field);
                }
                else if (syntaxNode is ClassDeclarationSyntax type && type.AttributeLists.Count != 0)
                {
                    this.Types.Add(type);
                }
            }
        }

        private sealed class FieldModel
        {
            internal FieldModel(IFieldSymbol symbol, string propertyName, string typeName, Location location)
            {
                this.Symbol = symbol;
                this.PropertyName = propertyName;
                this.TypeName = typeName;
                this.Location = location;
            }

            internal IFieldSymbol Symbol { get; }

            internal string PropertyName { get; }

            internal string TypeName { get; }

            internal Location Location { get; }
        }
    }
}

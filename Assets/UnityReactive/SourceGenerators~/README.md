# Reactive source generator

`AillieoUtils.Reactive.SourceGenerators` turns `[Reactive]` instance fields in partial classes into tracked properties. A partial class marked with `[ReactiveModel]` opts all eligible fields in by default; `[NonReactive]` excludes individual fields, while field-level `[Reactive("Name")]` can override a generated property name.

Build the generator with:

```powershell
dotnet build Reactive.SourceGenerators/AillieoUtils.Reactive.SourceGenerators.csproj -c Release
```

The build copies the analyzer DLL into `Editor/Analyzers`, where Unity imports it through the
`RoslynAnalyzer` asset label. The generator targets Roslyn 3.8 so the package remains compatible
with the full Unity 2022.3 line; generated code itself uses only C# 8 syntax.

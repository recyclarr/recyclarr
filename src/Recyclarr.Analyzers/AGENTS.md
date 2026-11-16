# Recyclarr.Analyzers

Repository-specific Roslyn rules, loaded into every project by `Directory.Build.props`.

- MUST NOT add runtime package dependencies. The compiler loads only this DLL; a dependency needs
  its own `Analyzer` item in every consuming project.
- New or stricter rules MUST fix or suppress every existing hit in the same change;
  `TreatWarningsAsErrors` breaks the build otherwise.
- Rule IDs use `REC` plus the next free number. Declare `Warning` and `isEnabledByDefault: true`; do
  not add `.editorconfig` entries to enable rules.
- Test through `tests/Recyclarr.Analyzers.Tests/AnalyzerRunner.cs`, which compiles offline against
  the test runtime. Microsoft.CodeAnalysis.Testing was rejected: it downloads reference assemblies
  at test time.

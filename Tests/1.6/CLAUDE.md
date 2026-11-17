# Test suite (`Tests/1.6/`)

xUnit (net472), run with `dotnet test Tests/1.6/XenogermTraderStock.Tests.csproj`; vstest hosts it
via mono.

- **Headless, pure logic only.** No live game: anything needing `DefDatabase`, `Current.Game` or
  the language database is out of scope. `GeneDef`s are built uninitialized (`TestHelpers.cs`), and
  price rows are asserted by key, placeholders and silver, never `Label()`.
- **To cover a decision, extract it as a pure function** and leave the game-facing method a thin
  adapter. Existing seams: `XenogermIdentity.Infer`, `PreferredXenotypeGate`,
  `XenotypeEligibility.Categorize`, `XenogermTradeability.Normalise`, `XenogermCommonality`,
  `XenogermPricing`. Internals are visible to the suite, which installs its own settings instance.
- **The suite loads the game's real 1.6 runtime assemblies**; Krafs's reference-only stubs cannot
  be loaded at run time. The csproj resolves their folder in order: `RIMWORLD_MANAGED_1_6`, a 1.6
  install's `<Data>/Managed/`, then the frozen copy at `~/.local/share/RimWorld-1.6/Managed` (its
  README.txt records provenance). A 1.7 install has no 1.6 assemblies, so there the frozen copy is
  the only source.
- **Failure triage:** every test failing with `TypeLoadException` on Assembly-CSharp means no
  runtime source was found and the build fell back to Krafs (that run also overwrites any real DLLs
  an earlier build copied into the test bin). A partial `BadImageFormatException` or
  `TypeLoadException` means a DLL is missing from the csproj's copy target: mono resolves field
  types eagerly where the Windows CLR is lazy. The csproj comments list each DLL and why.

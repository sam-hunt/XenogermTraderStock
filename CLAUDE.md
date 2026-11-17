# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## Project Overview

**Xenogerm Trader Stock** is a RimWorld 1.6 and 1.7 mod that puts xenogerms for preset and
player-scenario xenotypes in trader stock, priced by gene content, and makes implanting one yield a
real member of that xenotype (identity, ideology recognition, optionally the germline) instead of a
same-named custom. Requires Harmony and the Biotech DLC. C# (.NET Framework 4.7.2), Harmony, XML
patches; the mod ships no Defs of its own.

### Where documentation lives

Three layers, read inward:

1. **This file:** cross-cutting rules only, plus the pointers below.
2. **Domain guidance**, autoloaded when you work under its folder:
   - `Source/1.6/CLAUDE.md` — C#: patching, xenogerm identity and implantation, stock and
     eligibility, pricing, settings, play-data reload traps.
   - `1.6/Patches/CLAUDE.md` — XML patches: trader kinds, the gene opt-out, mod gating.
   - `1.6/Languages/CLAUDE.md` — localization process, the checker and its sidecar. Also governs
     `Scripts/` and `.steamworkshop/`.
   - `Tests/1.6/CLAUDE.md` — what the headless suite can cover and where its runtime assemblies
     come from.
3. **File headers:** every `.cs` and every XML patch opens with a comment holding its mechanism,
   the decompile-verified vanilla call paths, and what was tried and dropped. Put the *why* there;
   add a line to a guidance file only when it constrains work in other files, and never restate
   values or call paths in guidance (they drift). Where a guidance bullet names a file, that file's
   header is the full story.

## Build Commands

```bash
# Build (outputs to 1.6/Assemblies/ AND atomically redeploys to the RimWorld Mods folder)
dotnet build XenogermTraderStock.sln -c Release

# Stage the mod into an arbitrary folder (used by CI; same manifest as the local deploy)
dotnet build Source/1.6/XenogermTraderStock.csproj -c Release \
  -t:StageMod -p:StageDir=/path/to/output/XenogermTraderStock

# Run the test suite (native; vstest hosts the net472 suite via mono)
dotnet test Tests/1.6/XenogermTraderStock.Tests.csproj
```

The build compiles against the `Krafs.Rimworld.Ref` package pinned to the final 1.6 build, locally
and in CI; the local install is only the deploy target and the decompile target. The install path
(auto-detected, or `RIMWORLD_PATH`; under WSL point it at the Windows install in `~/.bashrc`)
decides only whether the build deploys, gated on the game's `Mods/` folder.

### Deployment

The repo lives outside the Mods folder; only `-c Release` builds deploy, atomically (wipe and
recopy, so renamed or deleted files never linger).

- **One manifest:** the `_ModFiles` ItemGroup in the `StageMod` target of
  `Source/1.6/XenogermTraderStock.csproj`. Local deploy, CI release and the Steam upload all go
  through it; add or remove shipped files only there. Every staged `.xml` is rewritten without its
  comments: repo files are untouched, the checker, probe and tests read the repo, and the smoke
  test boots the stripped copy.
- **Stop hook** (`.claude/hooks/sync-mod.sh`, wired by the tracked `.claude/settings.json`)
  rebuilds and redeploys after any turn that changed mod files, logging to
  `$TMPDIR/XenogermTraderStock-build.log`. It is byte-identical across the mod family: change it in
  the template and copy it verbatim. Its `find` watch list must cover every content root `StageMod`
  ships, or edits under a missed root silently stop redeploying.
- **`.claude/` is only partly gitignored:** `skills/` (`release`, `translate`, `rimworld-logs`),
  `hooks/` and `settings.json` are tracked; `settings.local.json` stays local. A skill is coupled
  to what it automates: `/release` encodes the CHANGELOG layout and the version scheme (`X.Y.Z-rc.N`
  candidates are CHANGELOG-less and Workshop-less, with the suffix only in `modVersion` and
  `AssemblyInformationalVersion`; `release.yml` treats any suffixed tag as a prerelease). Change
  the thing and the skill together.

## Conventions

- **Naming:** root C# namespace `XenogermTraderStock`, patch classes under `.Patches` and named
  `<Type>_<Method>_Patch`. Keyed strings carry the `XTS_` prefix.
- **Warnings are build errors** (`TreatWarningsAsErrors`, with Roslynator on every build).
  Severities are pinned in `.editorconfig`, whose comments explain each exception: `warning`
  blocks, `suggestion` is IDE-only. Plain `//` comments only, no XML doc comments. Formatting-only
  sweeps are registered in `.git-blame-ignore-revs`.
- **Every user-facing string is localized** through `.Translate()` against
  `1.6/Languages/English/Keyed/XTS_UI.xml`, except where vanilla already localizes the exact string
  (reuse its key) and names of game content, which come from their defs.
- **Player-facing text follows `l10n/languages/English.md`** (`Keyed/`, `About.xml`,
  `.steamworkshop/`): read it before writing or changing any. Its mechanical rules (no dashes,
  semicolons, "Whether" openers or parenthetical asides) are checked by
  `Scripts/check-translations.py` as `[player text]` warnings, which `--strict` turns into release
  errors. Guidance files, code comments and patch comments are unaffected.
- **Translation generation passes run only on explicit request** (token-expensive). Infra and
  tooling changes are always fine.
- **Logging:** `Log.Message("[Xenogerm Trader Stock] ...")`, the mod's display name.

## Feature map

Owning files; their headers and `Source/1.6/CLAUDE.md` carry the rules.

- **Trader stock** (`Traders/StockGenerator_Xenogerms.cs`, injected by `1.6/Patches/`): weighted
  picks from the sellable pool, per-trader counts overridable in settings.
- **What is sellable** (`Xenotypes/XenotypeEligibility.cs`): candidacy, categories and the
  per-xenotype sold ledger behind the settings grid (`UI/XenotypeGridUI.cs`).
- **Implant identity** (`Patches/GeneUtility_ImplantXenogermItem_Patch.cs`,
  `Xenotypes/XenogermIdentity.cs`): stamps the xenotype the germ yields, resolved by comp or by
  gene set.
- **Germline retarget** (`Xenotypes/GeneLayerRebuild.cs`): an inheritable xenotype's genes replace
  the pawn's endogenes; the Baseliner germ is the conversion item.
- **Metabolism pre-check** (`Patches/GeneUtility_MetabolismAfterImplanting_Patch.cs`): vanilla's
  implant refusal and confirmation read the metabolism a germline retarget will really leave.
- **Deathrest-capacity carryover** (`Patches/DeathrestCapacityCarryover.cs`): any implant or
  reimplant, not only trader germs.
- **Ideology preferred-xenotype gate** (`Patches/Xenogerm_PawnIdeoDisallowsImplanting_Patch.cs`,
  `Patches/PreferredXenotypeGate.cs`).
- **Pricing** (`Stats/`): market value and sell factor as StatParts over a settings-driven formula.
- **Tradeability guard** (`Traders/XenogermTradeability.cs`): undoes GTG Trader Core making
  xenogerms unsellable by traders.
- **Dev-mode tools** (`Debug/XenogermDebugActions.cs`, debug menu category "Xenogerm Trader
  Stock"): spawn any xenotype's trader germ; reset a pawn's genes to a named scenario xenotype.

## Testing

- **Unit suite:** `dotnet test Tests/1.6/XenogermTraderStock.Tests.csproj`, headless xUnit over the
  pure logic. CI builds it but does not run it; the release skill runs it and a Release build as
  its first gate.
- **Startup smoke test (pre-release):** `python3 Scripts/integration-smoke-test.py` (game closed)
  boots the *deployed* copy on its pinned minimal list (Biotech plus VEF core and Vanilla Races
  Expanded - Android, so `Patches_VREAndroid.xml`'s `FindMod` branch executes), classifies
  Player.log errors by origin and fails on anything attributed to this mod. It does not build, so
  run the Release build first or it silently tests the previous DLL. Wired into the release skill;
  thin shim over `l10n/smoke/`.

## Debugging

Use the `rimworld-logs` skill for Player.log locations and API disassembly.

- **Log:** on 1.7 it is `Logs/Player.log` with timestamps, levels and stack frames; the 1.6
  `Player.log` one level up is left in place, stale.
- **Inspect the API:** `ilspycmd`/`monodis` against the live install's `Assembly-CSharp.dll` (flat
  in the install root on 1.7 with `UnityStubs.dll` beside it, under `RimWorldWin64_Data/Managed/`
  on 1.6), preferred over the Krafs reference package. The local install is now 1.7, so a call path
  in a header that was verified on 1.6 should say so if re-verified on 1.7 and different.

# C# guidance (`Source/1.6/`)

Rules that constrain more than one file. Each file's header is the full story for its own
mechanism; a pointer here says which header. Cross-cutting conventions (namespaces, warnings as
errors, localization, logging) are in the root `CLAUDE.md`.

One folder per concern: `Core/` (entry point, settings), `Comps/`, `Patches/`, `Stats/`,
`Traders/`, `Xenotypes/`, `UI/`, `Debug/`.

## Patching

- `PatchAll()` in `Core/XenogermTraderStockMod.cs` picks up every `[HarmonyPatch]` class under
  `Patches/`.
- **Patch-timing hazard (foreign targets):** `PatchAll()` runs from the `Mod` ctor, before any defs
  load. Applying a detour JIT-compiles the target and runs its declaring type's static ctor, so a
  patch on ANOTHER MOD's method can permanently break that mod when its cctor resolves defs (the
  BetterTradersGuild v1.1.0 CWTL incident). Every current target is vanilla (safe). Before adding a
  foreign-target patch, defer its application until after defs load; worked example:
  BetterTradersGuild's `Core/DeferredModPatches.cs`.
- **The implant prefix exists to snapshot.** Vanilla's `ImplantXenogermItem` destroys the pawn's
  xenogenes and identity fields before the postfix runs, so anything the postfix must restore is
  captured in the prefix.

## Xenogerm identity and implantation

The target state is always *what `PawnGenerator` leaves for a generated member of the xenotype*:
tracker identity fields, `hybrid`, and which layer the genes sit on. Judge any change against that.

- **Two readers, never crossed.** The implant stamp and the ideology gate ask
  `XenogermIdentity.Resolve`, never `CompXenotypeSource` directly, so a comp-less copy (ReSplice
  Core's duplicator) or a gene-for-gene hand assembly still implants as the xenotype. Pricing reads
  the raw comp (`IsTraderSold`), never the resolver: pricing a copy as trader stock makes
  buy-one-duplicate-many a silver printer.
- **Resolution order** (`Xenotypes/XenogermIdentity.cs`): comp def, else gene-set inference with
  the player's templates in `customXenotypeDatabase` claiming the germ before any preset, name only
  as tie-breaker, an unbreakable tie resolving to `None` (vanilla behaviour). Gene-less presets are
  never inferred. Name-first matching was rejected (header). Also rejected, recorded only here: an
  on-load migration stamping comps onto matching germs (identity already resolves without one, and
  it would turn every duplicate into trader-priced stock), and patching the duplicator itself (a
  foreign-target patch that fixes nothing already sitting in saves).
- **`WillRetargetGermline` is the one retarget decision**, shared by the implant prefix, postfix,
  `PreferredXenotypeGate` and the metabolism prediction. A new condition goes there, or the gates
  and the stamp disagree.
  `Inheritable`, not the source kind, decides it; `implantGermlineAsEndogenes` is deliberately
  independent of the sold ledger (it acts on items the player already owns).
- **`GeneLayerRebuild.Apply` is the only writer of a whole gene layer** (retarget and the dev reset
  tool). It replaces, never merges, and its step order is load-bearing (header).
- **`GeneLayerRebuild.Predict` is `Apply`'s dry run**, read by the metabolism pre-check
  (`Patches/GeneUtility_MetabolismAfterImplanting_Patch.cs`). Change what `Apply` leaves on either
  layer and `Predict` changes with it, or implants are refused or allowed on a wrong number.
- **Identity follows the layer that owned it:** a xenogene-keyed identity whose xenogenes survive a
  retarget is restored, not stamped over (implant patch header).
- **Baseliner** is the one gene-less candidate: it gates as inheritable (`GatesAsInheritable`),
  its germ always retargets regardless of the setting, and it is pinned first in every pool
  (`InDisplayOrder`).
- **Android guard:** the retarget is skipped for a pawn whose endogenes carry an
  `excludeFromXenogermStock` gene. VREA's own gates already close every shipped entry point; this
  is defence in depth, so keep it.
- **Deathrest carryover is capacity only**, with max semantics, and is not gated on trader-sold.
  Do not generalise it to arbitrary gene state: `ExposeData` round-trips outside a load pass are a
  save-corruption hazard (`Patches/DeathrestCapacityCarryover.cs`).
- **The ideology gate lifts only the non-preferred-xenotype refusal.** The precept's bloodfeeder
  refusal is re-derived and left in force.
- **A custom template has no save-safe reference** (neither a Def nor `ILoadReferenceable`), so
  the comp stores its *name* (`Comps/CompXenotypeSource.cs`). Resolve to the live template; never
  deep-copy one.

## Stock and eligibility

- **`XenotypeEligibility` is the only place that decides sellability.** Generator, grid and debug
  spawner read through it; never read the settings ledger directly. Any new read path calls
  `SeedUnseen` first (custom xenotypes are per-game, so startup seeding cannot cover them).
  Categories are disjoint, and dormant ledger entries are never pruned.
- **Trader germs are made only by `XenogermFactory`:** a fresh `Xenogerm`'s `geneSet` is null until
  `Initialize`, and the factory stamps the comp pricing depends on.
- **Counts are never written onto defs.** The per-trader settings override falls back to the
  generator's XML `countRange` at roll time, so changes need no restart
  (`Core/Settings/Settings_Quantities.cs`).
- **Commonality strategies are stateless and pool-relative** (`Traders/XenogermCommonality.cs`);
  weights are computed in one pass over the whole sellable pool.
- **`XenogermTradeability.EnsureTraderCanSell` runs at startup and on every stock roll**; see the
  reload rule below.

## Play-data reload

A mid-session play-data reload (a language change) replaces every def instance and re-applies XML
patches.

- State living on a def *instance* must not hide behind `[StaticConstructorOnStartup]` alone;
  name-keyed state may (`Xenotypes/XenotypeLedgerStartup.cs` explains the distinction). Don't cache
  def lists per process.
- The reload destroys mod-shipped textures (`ModContentPack.ClearDestroy`). A static field holding
  one goes dead, so re-resolve through a Unity-null `== null` getter, never `??`/`??=`.
  Vanilla-path textures persist and are safe as statics (`UI/CompactIntRange.cs`'s `HandleTex`).

## Pricing

```
MarketValue     = 20 (base) + basePresetValue + |metabolism| × valuePerMetabolism
                  + complexity × valuePerComplexity + archites × valuePerArchite
SellPriceFactor = 0.05 (base, XML) + archites × per-archite bonus
```

Defaults, ranges and snap steps are the constants in `Core/Settings/Settings_Pricing.cs` and
`Stats/StatPart_XenogermSellFactor.cs`; read them there.

- `Stats/XenogermPricing.cs` is the one calculation, used by the StatParts and by spawn weighting.
- `Stats/XenogermPriceRows.cs` is the one wording, shared by the grid tooltip and the info-card
  explanations; the StatParts own no English literals.
- The settings grid previews the *shelf price* (market value × `VanillaBuyMarkup`), because players
  read a shop preview as what they will pay; the info card keeps raw market value. The tooltip's
  closing rows reuse vanilla's trade-tooltip keys.
- A bare `Translate()` riding a `+` chain converts through `StripTags`; resolve each fragment.

## Settings window

`XenogermTraderStockSettings` is a partial class: the frame in `Core/`, one file per section under
`Core/Settings/` owning its fields, scribe entries, defaults and draw method. The frame's header
has the add-a-setting recipe. Help text is hover-tooltip only, no always-visible tiny-font
sub-labels. Scribed fields stay camelCase (they are the save's XML element names; `.editorconfig`
exempts fields from PascalCase).

# XML patches (`1.6/Patches/`)

The mod ships no Defs, only patches onto vanilla and third-party defs. Each file's opening comment
explains its own targets and choices. Comments are stripped from the deployed copy, so write them
freely.

- **Adding a trader is XML only.** Add `StockGenerator_Xenogerms` to the `TraderKindDef`; its
  `countRange` is that trader's *shipped default* and lives nowhere else. The settings row and the
  per-trader override appear on their own (`Source/1.6/Core/Settings/Settings_Quantities.cs`).
- **Patching a kind also makes it buy xenogerms** (`TraderKindDef.WillTrade` is "any generator
  handles the def"), which is why outlander settlements carry the generator at `0~0`.
- **Settlement kinds** are a faction's `baseTraderKinds`, restocked monthly. Not patched: Odyssey's
  Traders Guild, whose `baseTraderKinds` is empty. Better Traders Guild gives its settlements the
  *orbital* kinds in rotation, so the orbital rows already cover them; a settlement row there would
  need a faction-keyed override, not a def patch.
- **`PatchOperationFindMod` matches display names, not packageIds:** a mod's `<name>`, or an
  expansion's `ExpansionDef` label ("Royalty"). Wrap any patch whose target def exists only with
  that mod or DLC, since an unmatched xpath is a patch error.
- **Gene opt-out:** a gene carrying `GeneExtension.excludeFromXenogermStock` bars every xenotype
  containing it, preset or custom, from stock. The snippet and the reasoning are in
  `Source/1.6/Xenotypes/GeneExtension.cs`; `Patches_VREAndroid.xml` is the shipped use, flagging
  abstract gene bases so derived genes inherit it.
- **`Patches_Xenogerm.xml` and `Patches_Stats.xml` wire the C# side** (`CompXenotypeSource`, the
  StatParts, the base sell factor). A rename in C# must move with them; nothing at build time
  checks the class names in XML.

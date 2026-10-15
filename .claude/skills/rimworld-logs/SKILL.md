---
name: rimworld-logs
description: Find and read RimWorld's Player.log, or disassemble the RimWorld API to inspect vanilla types and method signatures. Use when debugging runtime behaviour, chasing an exception, or checking what a vanilla method actually does.
---

# RimWorld Logs and API Inspection

## Reading the log

1. **Enable RimWorld Dev Mode:** Settings → Dev Mode → Logging
2. **Log locations** (the folder is the same on both game versions; the file differs):
   - **Windows:** `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\`
   - **WSL:** `/mnt/c/Users/*/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/`
   - **Linux (Steam):** `~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/`
   - **RimWorld 1.7:** `Logs/Player.log` under that folder. Every entry is
     `YYYY-MM-DD HH:MM:SS.mmm  LEVEL [Category] message` followed by indented `at ...`
     stack frames, so filter by level: `grep -E '^\S+ \S+ +(ERROR|WARN)' | grep -v '\[Vulkan\]'`.
   - **RimWorld 1.6:** `Player.log` directly in that folder, unstructured. After a 1.7 boot this
     file is stale 1.6 output; check which game wrote the file you are reading.

Our own messages are prefixed with the mod's display name, `[Xenogerm Trader Stock]` — the same
prefix RimWorld itself uses when it reports XML patch or def-load failures *about* the mod
(`[Xenogerm Trader Stock] Patch operation ... failed`), and the convention across the sibling
mods — so a single grep for `Xenogerm Trader Stock` isolates both our output and the game's
complaints about us. Note the 1.6 Player.log carries no stack frames or `(Filename:` locators: a
`Log.Error` is just its message line(s), so don't wait for a trace that will never appear (the 1.7
log does carry them).

## Inspecting the RimWorld API

```bash
# 1.7 (flat install root; UnityStubs.dll beside it carries the Unity-facing types)
monodis "$RIMWORLD_PATH/Assembly-CSharp.dll"
monodis --typedef "$RIMWORLD_PATH/UnityStubs.dll"
ilspycmd "$RIMWORLD_PATH/Assembly-CSharp.dll" -t Verse.ModContentPack
# 1.6 (Unity layout)
monodis "$RIMWORLD_PATH/RimWorldWin64_Data/Managed/Assembly-CSharp.dll"
```

The local RimWorld installation is the source of truth for API shapes — prefer it over the
`Krafs.Rimworld.Ref` NuGet package the build compiles against. Which layout you have depends on
the installed Steam branch (`cat "$RIMWORLD_PATH/Version.txt"`), so note the version a call path
was verified on. `ilspycmd` works too when you need method bodies rather than signatures, and
10.1+ decompiles the .NET 10 assembly.

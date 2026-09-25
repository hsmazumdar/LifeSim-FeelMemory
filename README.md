# LifeSim (V16.1) â€” reviewer mirror

Frozen champion snapshot of **success-weighted unique path-unit promotion** of **source-cell feel n-grams** for procedural terrain navigation (Terrain-A..D).

Clean copy of `LifeSim_V16.1` for GitHub / artifact sharing. Lab-only notes, patch scripts, Word drafts, and runtime dumps were removed. The only code change vs the lab freeze is a portable `EvalLogs` fallback (no hard-coded `K:\` path).

## Claim (for the accompanying paper)

Success-weighted unique path-unit promotion of source-cell feel n-grams yields honest leave-one-out goal-known energy gains on procedural Terrain-A..D; dest-cell feel, frequency-ranked TwoColumn LTM, and SuccessWeighted+TwoColumn append do not improve that transfer.

**Locked result (goal-known LOO `energy_gain_pct`, mean Â± std, n=5 seeds):**

| World | GK LOO |
|-------|--------|
| A | 75.2 Â± 5.1 |
| B | 80.8 Â± 4.9 |
| C | 71.5 Â± 6.7 |
| D | 69.3 Â± 4.0 |

Dest-hidden LOO (stated limit): A 19.7, B 46.3, **C âˆ’24.5**, D âˆ’5.0. Feel-only memory does not transfer under dest-hidden on Terrain-C; that is scoped as a limit, not a silent miss.

Canonical numbers and protocol: `EvalLogs/review_v16_1_20260925/SUMMARY.md` and `VERDICT.md`.

## Requirements

- Windows
- .NET Framework 4.7.2 (Visual Studio 2017+ / Build Tools with MSBuild)
- WinForms (desktop)


## Quick start (reviewers)

Double-click **`run_reviewer.bat`** for a menu (build / full Fix2 eval / GK LOO / smoke / teacher-diag / GUI).

Or one-shot paper reproduction:

`at
run_full_eval.bat
`

Frozen paper numbers are already under `EvalLogs\review_v16_1_20260925\` (no rebuild required to read results).
## Build
```bat
msbuild LifeSim_V16.1.sln /p:Configuration=Debug
```

Output: `EvolutionApp\bin\Debug\LifeSim_V16.1.exe`

## Reproduce the Fix2 honest eval

```bat
EvolutionApp\bin\Debug\LifeSim_V16.1.exe --eval --out EvalLogs\review_rerun
```

Other headless modes (see `Program.cs`):

```bat
LifeSim_V16.1.exe --smoke --out EvalLogs\smoke
LifeSim_V16.1.exe --gk-loo --out EvalLogs\gk_loo
LifeSim_V16.1.exe --teacher-diag --out EvalLogs\teacher_diag
```

Honesty protocol: ClearStm / CloneFrozenLtmOnly; EvalFrozenPaired; teacher success â†’ LTM only; Primary worlds Terrain-A..D.

## Layout

```
GitHub_mirror/
  LifeSim_V16.1.sln
  EvolutionApp/           # C# sources
  EvalLogs/review_v16_1_20260925/   # frozen CSV + SUMMARY/VERDICT
  FREEZE_V16.1.txt
  README.md
  CITATION.md
  .gitignore
```

## Ablations (sibling lab trees; not in this mirror)

| Tree | Role |
|------|------|
| LifeSim_V16 | Dest-cell feel (negative) |
| LifeSim_V16_TwoColumn | Frequency-ranked replace (negative) |
| LifeSim_V20 | SuccessWeighted + TwoColumn append (neutral GK, no DH win) |

## License

Research artifact for manuscript review. Contact the corresponding author before redistribution beyond review.
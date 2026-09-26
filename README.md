# LifeSim — reviewer mirror

Publication snapshot of **LifeSim**: success-weighted unique path-unit promotion of **source-cell feel n-grams** for procedural terrain navigation (Terrain-A..D).

This repository is a self-contained artifact for manuscript reviewers. It is not a multi-version lab archive; please evaluate the method from the code and frozen results here only.

## Claim (accompanying paper)

Success-weighted unique path-unit promotion of source-cell feel n-grams yields honest leave-one-out goal-known energy gains on procedural Terrain-A..D. Dest-cell feel anchoring, frequency-ranked LTM fill, and append-only frequency overlays do not improve that transfer.

**Locked result (goal-known LOO `energy_gain_pct`, mean ± std, n=5 seeds):**

| World | GK LOO |
|-------|--------|
| A | 75.2 ± 5.1 |
| B | 80.8 ± 4.9 |
| C | 71.5 ± 6.7 |
| D | 69.3 ± 4.0 |

Dest-hidden LOO (stated limit): A 19.7, B 46.3, **C −24.5**, D −5.0. Feel-only memory does not transfer under dest-hidden on Terrain-C; that is scoped as a limit, not a silent miss.

Canonical numbers and protocol: `EvalLogs/paper_results/SUMMARY.md` and `VERDICT.md`.

Manuscript for review: `LifeSim_Paper_v6.docx` (figures in `paper_figures/`).

## Quick start (reviewers)

Double-click **`run_reviewer.bat`** for a menu (build / full honest eval / GK LOO / smoke / teacher-diag / GUI).

Or one-shot paper reproduction:

```bat
run_full_eval.bat
```

Frozen paper numbers are already under `EvalLogs\paper_results\` (no rebuild required to read results).

## Requirements

- Windows
- .NET Framework 4.7.2 (Visual Studio 2017+ / Build Tools with MSBuild)
- WinForms (desktop)

## Build

```bat
msbuild LifeSim.sln /p:Configuration=Debug
```

Output: `EvolutionApp\bin\Debug\LifeSim.exe`

## Reproduce the honest eval

```bat
EvolutionApp\bin\Debug\LifeSim.exe --eval --out EvalLogs\review_rerun
```

Other headless modes:

```bat
LifeSim.exe --smoke --out EvalLogs\smoke
LifeSim.exe --gk-loo --out EvalLogs\gk_loo
LifeSim.exe --teacher-diag --out EvalLogs\teacher_diag
```

Honesty protocol: ClearStm / CloneFrozenLtmOnly; EvalFrozenPaired; teacher success → LTM only; Primary worlds Terrain-A..D.

## Layout

```
GitHub_mirror/
  LifeSim.sln
  EvolutionApp/              # C# sources
  EvalLogs/paper_results/    # frozen CSV + SUMMARY/VERDICT
  FREEZE.txt
  run_reviewer.bat
  run_full_eval.bat
  README.md
  CITATION.md
  .gitignore
```

## Ablations (described in the paper; not competing releases)

The manuscript reports negative / neutral controls for dest-cell feel, frequency-ranked LTM replacement, and success-weighted + frequency append. Those controls are **not** alternate versions of this repository.

## License

Research artifact for manuscript review. Contact the corresponding author before redistribution beyond review.
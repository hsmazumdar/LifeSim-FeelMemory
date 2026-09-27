# LifeSim — reviewer mirror

Publication snapshot of **LifeSim**: success-weighted unique path-unit promotion of **source-cell feel n-grams** for procedural terrain navigation, with a tabular n-gram behavior-cloning (**TabularNgramBC**) baseline under the same teacher trajectories and honest leave-one-out protocol.

This repository is a self-contained **code and frozen-results** artifact for manuscript reviewers. It is not a multi-version lab archive; please evaluate the method from the code and frozen results here only. The manuscript PDF/DOCX is **not** hosted in this repository; it is available from the corresponding author upon request or after acceptance.

## Claim (accompanying paper)

On leave-one-out goal-known evaluation, **SuccessWeighted** feel-atlas LTM beats **TabularNgramBC** (flat majority n-gram→action) and EmptyLTM reactive control. Dest-hidden LOO remains a stated limit for feel-only memory on some terrains (see locked EmptyLTM / dest-hidden tables).

### Headline baseline (Phase C — paper source of record)

Frozen under `EvalLogs/paper_results/baseline_phaseC_n25/` (n=25 seeds × 20 worlds: Terrain-A..D + 16 procedural, baseSeed 9001; 0 seed failures):

| Metric | Value |
|--------|-------|
| Worlds where SW GainPct > BC | **20/20** |
| Mean SW GainPct vs EmptyLTM | **74.82 +/- 6.72** |
| Mean BC GainPct vs EmptyLTM | **19.71 +/- 21.49** |

### Phase A / B (central four-world continuity)

| Phase | Seeds | Worlds | SW > BC | Notes / frozen pack |
|-------|-------|--------|---------|---------------------|
| A | 5 | A–D | **4/4** | `EvalLogs/paper_results/baseline_phaseA/` |
| B | 25 | A–D | **4/4** | `EvalLogs/paper_results/baseline_phaseB/` |

Phase A per-world SW GainPct (mean +/- std) matches the locked goal-known LOO EmptyLTM comparison below (A 75.15+/-5.08, B 80.84+/-4.87, C 71.53+/-6.69, D 69.29+/-3.96).

### Locked SW vs EmptyLTM (goal-known LOO, n=5) — still valid

| World | GK LOO energy_gain_pct |
|-------|------------------------|
| A | 75.2 +/- 5.1 |
| B | 80.8 +/- 4.9 |
| C | 71.5 +/- 6.7 |
| D | 69.3 +/- 4.0 |

Dest-hidden LOO (stated limit): A 19.7, B 46.3, **C −24.5**, D −5.0. Feel-only memory does not transfer under dest-hidden on Terrain-C; that is scoped as a limit, not a silent miss.

Canonical CSV + VERDICT: `EvalLogs/paper_results/` (EmptyLTM / dest-hidden suite) and `EvalLogs/paper_results/baseline_phase*/` (SW vs BC).

**Manuscript:** available from the corresponding author (Himanshu S. Mazumdar, hsmazumdar@ddu.ac.in) upon request or after acceptance. Result figures for verification are under `paper_figures/`.

## Quick start (reviewers)

Double-click **`run_reviewer.bat`** for a menu:

- Build / full honest `--eval` / GK LOO / **baseline short check** / Phase B paper-scale / smoke / teacher-diag / GUI

Short baseline check (recommended first):

```bat
EvolutionApp\bin\Debug\LifeSim.exe --eval-baseline --seeds 5 --worlds 4 --out EvalLogs\baseline_short
```

Or one-shot SW EmptyLTM suite:

```bat
run_full_eval.bat
```

Frozen paper numbers are already under `EvalLogs\paper_results\` (no rebuild required to read results).

### Paper-scale baseline commands (optional long runs)

```bat
:: Phase B — n=25 on Terrain-A..D (~10–20 min)
LifeSim.exe --eval-baseline --seeds 25 --out EvalLogs\baseline_phaseB_rerun

:: Phase C — n=25 × 20 worlds (overnight; matches paper headline)
LifeSim.exe --eval-baseline --seeds 25 --worlds 20 --procedural 16 --base-seed 9001 --out EvalLogs\baseline_phaseC_rerun
```

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
LifeSim.exe --eval-baseline --seeds 5 --worlds 4 --out EvalLogs\baseline_short
```

Honesty protocol: ClearStm / CloneFrozenLtmOnly; EvalFrozenPaired; teacher success → LTM only (SW) or flat BC counts from the **same** TeacherSuccess trajectories; Primary worlds Terrain-A..D (plus procedural for Phase C).

## Layout

```
  LifeSim.sln
  EvolutionApp/                 # C# sources (incl. TabularNgramBC)
  EvalLogs/paper_results/       # frozen EmptyLTM / dest-hidden CSV + SUMMARY/VERDICT
  EvalLogs/paper_results/baseline_phaseA/
  EvalLogs/paper_results/baseline_phaseB/
  EvalLogs/paper_results/baseline_phaseC_n25/
  paper_figures/                # result figures (not the manuscript file)
  FREEZE.txt
  run_reviewer.bat
  run_full_eval.bat
  README.md
  CITATION.md
  .gitignore
```

## Data and code availability

Reviewer mirror (code + frozen results): https://github.com/hsmazumdar/LifeSim-FeelMemory

Manuscript PDF/DOCX: corresponding author upon request / after acceptance (not in this repository).

## Ablations (described in the paper; not competing releases)

The manuscript reports negative / neutral controls for dest-cell feel, frequency-ranked LTM replacement, and success-weighted + frequency append. Those controls are **not** alternate versions of this repository. TabularNgramBC is the simple learned baseline under `--eval-baseline`, not a separate product line.

## License

Research artifact for manuscript review. Contact the corresponding author before redistribution beyond review.
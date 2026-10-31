# LifeSim-FeelMemory

Code, evaluation harnesses, logs and statistics for the paper

> **What Drives Generalization in Feel-Sequence Memory? Goal Conditioning, Not Success Weighting, on Procedural Terrain**
> Kanak Patel and Himanshu S. Mazumdar (corresponding author), R&D Centre, Dharmsinh Desai University, Nadiad, India.
> *Under submission.*

The paper is not included in this repository.

## What this is

LifeSim is a C# (.NET Framework, WinForms) grid simulator. In it, an agent crosses procedural rough terrain from a start cell to a destination cell, and every step costs energy (0.2 + 0.8 × roughness; 2.0 for an obstacle).

**The memory under study.** The agent quantizes the cells it crosses into "feels" (Smooth / Medium / Rough / Obstacle). It stores feel n-grams (lengths 3–5) together with the action that followed, plus goal-direction and progress-bin context. This store is a success-weighted (SW) long-term memory built from teacher trajectories.

**The question.** The paper asks which components of this memory actually drive leave-one-out (LOO) generalization:
- success weighting;
- the goal context;
- the lookup method.

It compares SW with ablations of itself and with feel-only, tuned and goal-conditioned (GC) baselines:
- tabular n-gram behaviour cloning (BC);
- kNN;
- offline episodic control;
- offline tabular Q.

All policies act through the same reactive controller. Gains are reported against the no-memory reactive controller (EmptyLTM).

## Key findings (20 held-out worlds × 10 seeds; paired exact Wilcoxon, Holm correction, bootstrap 95% CI)

**1. The original result reproduces.** SW gains 74.8 ± 6.7% over EmptyLTM, against 19.7 ± 21.5% for feel-only BC (n = 25 seeds; `revision_r1/01_Audit_Report.md`).

**2. Goal context, not success weighting, carries the gain.**
- Removing goal context from retrieval drops SW from 75.1% to 2.9%.
- With DirectionWeight = 0, SW is worse than no memory (−12.6%).
- Success weighting adds only +0.9 pp [0.5, 1.3].

**3. Equally informed baselines beat SW.** In the goal-known setting:

| Policy | Gain | Margin over SW |
|---|---:|---:|
| kNN_GC | 89.6% | +14–15 pp, on 20/20 worlds |
| BC_GC | 89.1% | +14–15 pp, on 20/20 worlds |
| EC_GC | 80.4% | +5 pp |
| Q_GC | 80.6% | +5 pp |
| SW | 75.1% | — |

Capping memory capacity does not close the gap (`03_Sensitivity_Report.md`).

**4. Destination hidden.** SW gains 16.6%, while tuned feel-only kNN and BC gain 61–62%.

**5. SW as a fallback for BC_GC.** Handing BC_GC's no-match steps to SW gives a small, consistent gain on 20/20 worlds:
- +0.9 pp with the full data;
- +3.6 pp with k = 2 training worlds.

It does not help kNN_GC, and it hurts when the goal signal drops out (`05_PositiveGain_Report.md`).

**Correction:** in the low-data experiment, `k` is the **number of training worlds** (a random subset of the LOO training worlds; see `PosGainHarness.cs`), not the number of teacher paths.

**Disclosed issues:**
- In the original code, the test-time progress bin was step-based, while training used a distance-based bin. The `SW_ProgDist` variant fixes this (+1.0 pp).
- Several tuned picks lie at the edge of the grid.
- EC and Q are offline variants.

## Build

Visual Studio 2022 (or Build Tools) with the .NET desktop workload:

```
msbuild LifeSim.sln /p:Configuration=Release
```

The output is `EvolutionApp\bin\Release\LifeSim.exe`. `run_menu.bat` builds the solution and offers the common runs. Running the exe with no arguments opens the GUI.

## Reproduce

All runs are headless and write CSV, SUMMARY and stats files under `--out`.

| Purpose | Command |
|---|---|
| Smoke test | `LifeSim.exe --smoke --out EvalLogs\smoke` |
| SW / EmptyLTM suite (LOO GK, DH, teacher in-sample) | `LifeSim.exe --eval --out EvalLogs\eval` |
| SW vs feel-only BC (20 worlds, n=25) | `LifeSim.exe --eval-baseline --seeds 25 --worlds 20 --procedural 16 --base-seed 9001 --out EvalLogs\baseline` |
| Audit: leakage, reproduction, shuffled-action and no-goal-context controls | `LifeSim.exe --audit-r1 --out EvalLogs\audit` |
| Baselines (kNN, EC, Q) and ablations | `LifeSim.exe --eval-revision --seeds 10 [--hide-dest] [--ngram L] --out ...` |
| Main study: SW, ablations, feel-only / tuned / GC baselines, GK and DH | `LifeSim.exe --eval-stageb --seed-list 101,202,... --world-filter ... --out ...` (driver: `revision_r1\run_stageB.ps1`) |
| Capacity and DirectionWeight sensitivity | `LifeSim.exe --eval-sens --out ...` |
| SW-fallback hybrids (low data, goal noise/drop, generator shift, DH) | `LifeSim.exe --eval-posgain --seed-list ... --world-filter ... --out ...` (driver: `revision_r1\run_posgain.ps1`) |
| Teacher diagnostic | `LifeSim.exe --teacher-diag --out ...` |

The PowerShell drivers split seeds and worlds into parallel processes. Edit `$root` and the exe path at the top of each driver to match your checkout.

**Statistics scripts (Python 3: numpy, scipy, pandas):**
- `revision_r1\revision_stats.py`
- `revision_r1\stageb_stats.ps1`
- `revision_r1\sens_summary.py`
- `revision_r1\posgain_stats.py`

## Where results live

| Path | Contents |
|---|---|
| `revision_r1/01_Audit_Report.md` | Reproduction, leakage audit, fairness matrix, controls |
| `revision_r1/02_StageB_Report.md`, `stats_all10.md` | Main GK and DH tables and all paired tests |
| `revision_r1/03_Sensitivity_Report.md`, `sens_raw/` | Capacity and DirectionWeight sweeps |
| `revision_r1/05_PositiveGain_Report.md`, `posgain_full/stats_full.md` | Fallback-hybrid study |
| `revision_r1/stageA_logs/` | Audit raw logs |
| `EvalLogs/paper_results/` | Original SW / EmptyLTM / BC result packs (20-world n=25 pack in `baseline_phaseC_n25/`) |
| `paper_figures/Terrain-*.png` | The four hand-designed terrains |

## Citation

See `CITATION.md`.

## Contact

Himanshu S. Mazumdar, hsmazumdar@ddu.ac.in

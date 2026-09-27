# Phase B - VERDICT

Date: 2026-09-25 23:13 IST
Wall clock: 12.9 min

## Mean+/-std GainPct vs EmptyLTM (energy)

| World | SuccessWeighted | TabularNgramBC | SW-BC (pp) | SW>BC? |
|-------|-----------------|----------------|------------|--------|
| Terrain-A | 75.40 +/- 5.38 | 7.55 +/- 24.32 | 67.84 | YES |
| Terrain-B | 79.20 +/- 6.85 | 33.54 +/- 12.88 | 45.66 | YES |
| Terrain-C | 69.45 +/- 8.08 | -2.50 +/- 20.58 | 71.96 | YES |
| Terrain-D | 69.23 +/- 5.02 | -0.07 +/- 17.26 | 69.31 | YES |

## Verdict
- Worlds where SuccessWeighted GainPct > TabularNgramBC: **4/4**
- Does SW still beat tabular BC overall? **YES**
- EmptyLTM is the reactive baseline; GainPct is vs EmptyLTM.
- Seed count n=25; seed failures: **0**
- Continuity: first 5 seeds {101,202,303,404,505} match Phase A; remaining via BuildLooSeedList(25).

## Build
- Harness: wired `--seeds` / `--loo-seeds` in EvalHarness; uses BuildLooSeedList when n!=5.
- `LifeSim.sln` -> `EvolutionApp\bin\Debug\LifeSim.exe --eval-baseline --seeds 25 --out EvalLogs\paper_results\baseline_phaseB`
- Paper-scale Phase B (n=25) on Terrain-A..D.

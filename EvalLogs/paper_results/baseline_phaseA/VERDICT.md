# Phase A — VERDICT

Date: 2026-09-25 22:56 IST
Wall clock: 5.2 min

## Mean+/-std GainPct vs EmptyLTM (energy)

| World | SuccessWeighted | TabularNgramBC | SW-BC (pp) | SW>BC? |
|-------|-----------------|----------------|------------|--------|
| Terrain-A | 75.15 +/- 5.08 | 5.42 +/- 20.34 | 69.73 | YES |
| Terrain-B | 80.84 +/- 4.87 | 29.60 +/- 19.41 | 51.24 | YES |
| Terrain-C | 71.53 +/- 6.69 | -5.93 +/- 22.64 | 77.46 | YES |
| Terrain-D | 69.29 +/- 3.96 | -2.63 +/- 11.56 | 71.91 | YES |

## Verdict
- Worlds where SuccessWeighted GainPct > TabularNgramBC: **4/4**
- Does SW still beat tabular BC overall? **YES**
- EmptyLTM is the reactive baseline; GainPct is vs EmptyLTM.
- TabularNgramBC (flat majority, exact match) does **not** close the gap to SuccessWeighted under this LOO protocol.

## Build
- LifeSim.sln -> EvolutionApp\bin\Debug\LifeSim.exe --eval-baseline
- Outputs: EvalLogs\paper_results\baseline_phaseA\
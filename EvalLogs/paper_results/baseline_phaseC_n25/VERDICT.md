# Phase C - VERDICT

Date: 2026-09-27 01:11 IST
Wall clock: 645.7 min

## Mean±std GainPct vs EmptyLTM (energy)

| World | SuccessWeighted | TabularNgramBC | SW−BC (pp) | SW>BC? |
|-------|-----------------|----------------|------------|--------|
| Terrain-A | 74.816 ± 5.5206 | 21.5398 ± 17.843 | 53.2762 | YES |
| Terrain-B | 79.1181 ± 7.0684 | 32.9997 ± 13.5928 | 46.1185 | YES |
| Terrain-C | 69.5459 ± 7.9916 | 4.9733 ± 15.1952 | 64.5726 | YES |
| Terrain-D | 69.4292 ± 4.9618 | 9.6722 ± 15.9414 | 59.7569 | YES |
| Terrain-01 | 66.8164 ± 7.0134 | -9.5005 ± 27.6304 | 76.3169 | YES |
| Terrain-02 | 76.3158 ± 6.4831 | 28.1981 ± 16.6261 | 48.1176 | YES |
| Terrain-03 | 71.9228 ± 5.2284 | 0.5524 ± 19.0393 | 71.3704 | YES |
| Terrain-04 | 65.6563 ± 8.1934 | 0.7601 ± 21.1745 | 64.8961 | YES |
| Terrain-05 | 71.5675 ± 4.829 | 1.6814 ± 19.2424 | 69.8861 | YES |
| Terrain-06 | 71.4735 ± 5.9291 | 8.2241 ± 20.0044 | 63.2494 | YES |
| Terrain-07 | 80.3189 ± 6.5613 | 38.4177 ± 12.4313 | 41.9012 | YES |
| Terrain-08 | 68.1725 ± 5.0204 | 2.7084 ± 20.6422 | 65.4641 | YES |
| Terrain-09 | 72.3914 ± 4.8137 | 2.9937 ± 21.6996 | 69.3978 | YES |
| Terrain-10 | 74.1926 ± 4.9907 | 17.0972 ± 17.7548 | 57.0954 | YES |
| Terrain-11 | 87.4551 ± 2.7525 | 62.2309 ± 6.7915 | 25.2242 | YES |
| Terrain-12 | 76.042 ± 3.7302 | 21.9614 ± 15.7688 | 54.0806 | YES |
| Terrain-13 | 69.5724 ± 6.8772 | 6.221 ± 18.1232 | 63.3513 | YES |
| Terrain-14 | 81.4981 ± 5.3444 | 43.8748 ± 12.5817 | 37.6233 | YES |
| Terrain-15 | 78.9153 ± 5.2065 | 29.0519 ± 15.9125 | 49.8634 | YES |
| Terrain-16 | 91.2712 ± 2.7604 | 70.495 ± 8.0363 | 20.7762 | YES |

## Verdict
- Worlds where SuccessWeighted GainPct > TabularNgramBC: **20/20**
- Mean across worlds SW GainPct: **74.82 +/- 6.72**
- Mean across worlds BC GainPct: **19.71 +/- 21.49**
- Does SW still beat tabular BC overall? **YES**
- EmptyLTM is the reactive baseline; GainPct is vs EmptyLTM.
- World count: **20** (procedural=16, baseSeed=9001)
- Seed count n=25; seed failures: **0**
- Note: n=25 full LOO on 20 worlds is overnight follow-up; this run is n=25.

## Build
- Harness: `--worlds` / `--procedural` / `--base-seed` / `--seeds` wired in EvalHarness.
- `LifeSim.sln` -> `EvolutionApp\bin\Debug\LifeSim.exe --eval-baseline --seeds 25 --worlds 20 --procedural 16`

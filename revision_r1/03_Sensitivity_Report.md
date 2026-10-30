# 03 Sensitivity Report — LifeSim Stage B (GK mode)
Generated 2026-10-30 16:38 IST. Worlds: Terrain-A..D held out (LOO over 20), seeds 101/202/303, so 12 runs per cell. Raw data: EvalLogs\sens\*\sens_raw.csv; summary script: sens_summary.py.
Gain = 100*(EmptyLTM - P)/EmptyLTM energy, averaged per world and then across the 4 worlds. Capped GC baselines keep the first N keys and use fixed configs (BC boost 8; kNN boost 8, K 3, Wg 1). SW cap sweep uses dirw = 2.5; dirw sweep uses cap = 512.

## Summary
| cap (keys) | SW | SW_NoSW | BC_GC capped | kNN_GC capped |
|---:|---:|---:|---:|---:|
| 64 | 55.1 | 55.1 | 60.7 | 81.2 |
| 128 | 63.6 | 62.6 | 68.8 | 84.2 |
| 256 | 70.1 | 67.5 | 76.0 | 86.7 |
| 512 | 72.4 | 70.2 | 81.5 | 87.7 |
| 1024 | 71.5 | 69.9 | 85.4 | 88.1 |
Uncapped at 512 run: BC_GC 87.5 (~2850 keys), kNN_GC 88.2 (~1600 keys).

| DirectionWeight | 0 | 0.5 | 1 | 2.5 (default) | 5 |
|---|---:|---:|---:|---:|---:|
| SW | -12.6 | 52.7 | 68.7 | 72.4 | 74.1 |
| SW_NoSW | -12.7 | 51.1 | 65.3 | 70.2 | 72.5 |

## Findings
1. **The capacity confound does not explain the gap.** At equal key budgets, both GC baselines beat SW at every cap. kNN_GC leads by 16-26 pp, and BC_GC by 6-14 pp. SW plateaus at 512-1024 keys (72.4 vs 71.5), so giving SW more memory does not close the gap.
2. **The goal-direction term is the whole effect.** With DirectionWeight 0, SW loses to EmptyLTM (-13%). Gain rises steadily with weight, and 5 is slightly better than the default 2.5 (+1.7 pp). The default is not at an optimum, but the change is small.
3. **Success weighting adds 0 to 3.4 pp** (mean ~1.6 pp over caps and weights). It adds 0 at cap 64. This is consistent with the ~+1 pp in Stage B.
4. Caveat: only 4 worlds and 3 seeds, and the capped baselines used fixed (not re-tuned) configs. These are descriptive means, with no significance tests.

## Full table
| sweep | value | policy | gain % (mean over worlds) | SD over worlds | n | mean keys |
|---|---|---|---:|---:|---:|---:|
| cap | 64 | KnnFeel_GC_capped | 81.2 | 3.2 | 12 | 64 |
| cap | 128 | KnnFeel_GC_capped | 84.2 | 3.0 | 12 | 128 |
| cap | 256 | KnnFeel_GC_capped | 86.7 | 2.8 | 12 | 256 |
| cap | 512 | KnnFeel_GC_capped | 87.7 | 2.6 | 12 | 512 |
| cap | 1024 | KnnFeel_GC_capped | 88.1 | 2.7 | 12 | 1024 |
| cap | 512 | KnnFeel_GC_uncapped | 88.2 | 2.7 | 12 | 1599 |
| cap | 64 | SW_NoSuccessWeight | 55.1 | 8.1 | 12 | 64 |
| cap | 128 | SW_NoSuccessWeight | 62.6 | 6.3 | 12 | 128 |
| cap | 256 | SW_NoSuccessWeight | 67.5 | 5.0 | 12 | 256 |
| cap | 512 | SW_NoSuccessWeight | 70.2 | 5.5 | 12 | 512 |
| cap | 1024 | SW_NoSuccessWeight | 69.9 | 4.1 | 12 | 1024 |
| cap | 64 | SuccessWeighted | 55.1 | 4.4 | 12 | 64 |
| cap | 128 | SuccessWeighted | 63.6 | 6.4 | 12 | 128 |
| cap | 256 | SuccessWeighted | 70.1 | 3.9 | 12 | 256 |
| cap | 512 | SuccessWeighted | 72.4 | 4.6 | 12 | 512 |
| cap | 1024 | SuccessWeighted | 71.5 | 5.2 | 12 | 1024 |
| cap | 64 | TabularNgramBC_GC_capped | 60.7 | 7.3 | 12 | 64 |
| cap | 128 | TabularNgramBC_GC_capped | 68.8 | 6.7 | 12 | 128 |
| cap | 256 | TabularNgramBC_GC_capped | 76.0 | 4.1 | 12 | 256 |
| cap | 512 | TabularNgramBC_GC_capped | 81.5 | 4.2 | 12 | 512 |
| cap | 1024 | TabularNgramBC_GC_capped | 85.4 | 3.4 | 12 | 1024 |
| cap | 512 | TabularNgramBC_GC_uncapped | 87.5 | 3.2 | 12 | 2851 |
| dirw | 0 | SW_NoSuccessWeight | -12.7 | 24.5 | 12 | 512 |
| dirw | 0.5 | SW_NoSuccessWeight | 51.1 | 7.3 | 12 | 512 |
| dirw | 1 | SW_NoSuccessWeight | 65.3 | 5.9 | 12 | 512 |
| dirw | 2.5 | SW_NoSuccessWeight | 70.2 | 5.5 | 12 | 512 |
| dirw | 5 | SW_NoSuccessWeight | 72.5 | 4.6 | 12 | 512 |
| dirw | 0 | SuccessWeighted | -12.6 | 20.7 | 12 | 512 |
| dirw | 0.5 | SuccessWeighted | 52.7 | 9.8 | 12 | 512 |
| dirw | 1 | SuccessWeighted | 68.7 | 6.0 | 12 | 512 |
| dirw | 2.5 | SuccessWeighted | 72.4 | 4.6 | 12 | 512 |
| dirw | 5 | SuccessWeighted | 74.1 | 5.6 | 12 | 512 |

# Stage B Report — LifeSim
Generated 2026-10-30 16:06 IST. Data: pilot (seeds 101/202/303) + full (404..1010) = 10 seeds x 20 LOO worlds, 5800 rows. Full stats: EvalLogs\stageB\all10\stats_all10.md. Smoke and repro_A404 runs are excluded.

Protocol: LOO over 20 worlds (A-D plus 16 procedural ones); 250 noisy teacher attempts + 1 Dijkstra path; 30 eval runs; 2000 max steps. Gain = 100*(Empty-P)/Empty energy. Tuning was done on 3 inner training worlds only. The +/- is the SD across the per-world means.

## Main table, goal-known (GK)
| Policy | Gain % | SD |
|---|---:|---:|
| KnnFeel_GC (tuned) | 89.6 | 3.1 |
| BC_GC (tuned) | 89.1 | 3.7 |
| Q_GC | 80.6 | 5.3 |
| EC_GC | 80.4 | 5.9 |
| SW_ProgDist | 76.1 | 6.6 |
| SW_NoSW_ProgDist | 75.4 | 6.9 |
| SuccessWeighted (SW) | 75.1 | 6.9 |
| SW_NoSuccessWeight | 74.2 | 7.3 |
| kNN / BC feel-only, tuned | 25.9 / 24.3 | ~20 |
| SW_NoGoalCtx | 2.9 | 25.7 |
| EC / Q feel-only | -5.9 / -26.7 | |

Key paired comparisons (per world, Holm-corrected Wilcoxon, 95% bootstrap CI):
- SW - BC_GC = -14.1 pp [-15.6, -12.3], 0/20 worlds; SW - kNN_GC = -14.6 pp; SW - EC_GC = -5.3; SW - Q_GC = -5.5 (all Holm p=0.003).
- SW - SW_NoSW = +0.9 pp [0.5, 1.3], 16/20, Holm p=0.007; ProgDist variants +0.7 pp [0.4, 0.9], 18/20.
- ProgDist fix (SW_ProgDist - SW) = +1.0 pp [0.6, 1.4], Holm p=0.005.
- SW - SW_NoGoalCtx = +72.2 pp; each GC baseline beats its feel-only version by 64-97 pp.

## Destination-hidden (DH)
kNN_Tuned 62.4, BC_Tuned 61.2, BC 50.6, kNN 50.2, EC_Tuned 32.8, EC 29.0, **SW 16.6**, SW_NoSW 15.9, Q -10 to -13.
SW - BC_Tuned = -44.5 pp (0/20); SW - SW_NoSW = +0.8 pp [-1.6, 3.0], Holm p=0.45 (n.s.).

## Verdict
1. **Goal conditioning drives the gain, not the SW memory.** Removing goal context from SW drops its gain from 75% to 3%. Adding the same goal context to simple baselines lifts them from ~25% to ~90%.
2. **SW does NOT beat equally informed baselines.** The GC baselines beat SW by 5-15 pp in GK on 20/20 worlds. In DH, simple tuned BC/kNN beat SW by ~45 pp.
3. **Success weighting adds a tiny but statistically significant amount in GK (~+1 pp).** It adds nothing significant in DH. That is not enough to support the headline claim.
4. The ProgDist bin fix helps by +1 pp (significant, but small).

## Caveats
- **Capacity confound:** SW LTM saturates at 512 keys, while the GC baselines store ~1.6k-2.8k keys. This is being tested in 03_Sensitivity_Report.md (cap sweep with matched-cap baselines).
- Tuning picks sit at the grid edges (boost=8 for most arms, Wg=1 for kNN_GC/EC_GC), so the baselines may be under-tuned. This would make their advantage conservative.
- All goal rates are ~1.0 in GK, so energy is the only discriminating metric.

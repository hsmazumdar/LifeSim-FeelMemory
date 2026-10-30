# Positive-Gain Report - LifeSim ([redacted]) revision
Generated 2026-10-30 23:25 IST. Data: full grid, 10 seeds (101..1010) x 20 LOO worlds = 200 chunks, all finished (runner done 21:52 IST), 15400 raw rows. Full stats: EvalLogs\posgain\full\stats_full.md (copy in code\revision_r1\posgain_full\). Pilot (A-D x 3 seeds) is excluded.

Protocol as in Stage B (report 02). Gain = 100*(Empty-P)/Empty energy, relative to EmptyLTM under the same condition. Per-world mean over seeds; paired exact Wilcoxon (two-sided) over the 20 worlds; Holm over the pre-specified family of 38 comparisons; bootstrap 95% CI (10000, worlds resampled). Experiments: E1 hybrid at full data (base) and with the destination hidden (dh); E2 low data (k = 2/5/10 teacher paths); E3 robustness (goal-signal noise 25/50%, goal-signal drop 25/50%, generator shift).

Hybrid = goal-conditioned (GC) baseline that hands a decision step to the SW feel memory when its own match is weak (tau = confidence threshold, miss = no match). "pp" = percentage points of gain.

## What is supported (Holm p <= 0.05)
1. Hybrid tabular BC beats BC_GC in every setting except goal drop:
   - Full data: +0.85 pp [0.61, 1.11] (tau), +0.90 pp [0.65, 1.16] (miss), 20/20 worlds, Holm p = 0.0001.
   - Low data (miss): k=2 +3.64 pp [2.97, 4.30]; k=5 +1.74 pp [1.39, 2.10]; k=10 +1.15 pp [0.87, 1.44]; all 20/20, Holm p = 0.0001. The gain is largest when data is scarcest.
   - Goal noise (tau): 25% +1.20 pp [0.85, 1.56] 20/20; 50% +1.25 pp [0.86, 1.63] 19/20; Holm p = 0.0001.
   - Generator shift (tau): +0.80 pp [0.63, 0.99], 20/20, Holm p = 0.0001. Note the shift barely moves the baselines (BC_GC 88.9 vs 89.2 base), so this is a weak test of shift robustness.
   - In absolute terms the hybrid lifts BC to about the level of KnnFeel_GC (e.g. 90.1 vs 89.7 at full data), but at k=2 it still trails KnnFeel_GC (88.1 vs 89.2). Hyb_BC vs KnnFeel_GC was not in the pre-specified family and is not tested.
   - SW takes 13-19% of decision steps in base/shift/noise, 30% at k=2.

## What is not supported
2. Hybrid kNN adds nothing: vs KnnFeel_GC the differences are -0.07 to +0.11 pp, none significant after Holm at base, k=2/5/10, noise25, shift; at noise50 it is significantly worse (-0.30 pp [-0.46, -0.13], Holm p = 0.03).
3. Destination-hidden hybrids (DH): HybDH_kNN +0.31 pp [-0.41, 0.99], 13/20, Holm p = 0.99; HybDH_BC +0.01 pp, Holm p = 0.99. The pilot's +1.7 pp for HybDH_kNN did not replicate. SW is handed under 2% of steps here. With the destination hidden, SW alone gains 16.6% vs 63.2-63.3% for the feel-only baselines.

## Negatives, stated plainly
4. Pure SW is below both GC baselines in every condition, 0/20 worlds each time, all Holm p = 0.0001: by 11-18 pp at low data, 14-15 pp under shift, 17-23 pp under goal noise, 20-28 pp under goal drop.
5. The hybrid hurts under goal drop: drop25 kNN -1.46 pp, BC -0.58 pp; drop50 kNN -3.91 pp, BC -2.99 pp (0-1/20 worlds, Holm p = 0.0001). When the goal signal is missing, SW is handed 35-59% of steps and drags the baseline down.
6. Success weighting adds little: SW vs SW_NoSuccessWeight is +0.9 pp at full data, +0.5 to +1.1 pp at k=2/5/10 and +0.5 pp under shift (descriptive; this pair is not in this family; Stage B tested it at +0.9 pp [0.5, 1.3]).

## Mean gain % over worlds
| cond | SW | SW_NoSW | BC_GC | kNN_GC | Hyb_BC tau | Hyb_kNN tau | Hyb_BC miss | Hyb_kNN miss |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| base | 75.1 | 74.2 | 89.2 | 89.7 | 90.1 | 89.6 | 90.1 | 89.7 |
| k2 | 73.5 | 73.0 | 84.5 | 89.2 | | | 88.1 | 89.3 |
| k5 | 72.5 | 71.4 | 87.5 | 89.6 | | | 89.3 | 89.6 |
| k10 | 71.9 | 71.2 | 88.7 | 89.7 | | | 89.8 | 89.7 |
| noise25 | 68.9 | | 85.7 | 86.6 | 86.9 | 86.5 | 87.0 | 86.6 |
| noise50 | 58.1 | | 79.5 | 80.7 | 80.8 | 80.4 | 80.9 | 80.8 |
| drop25 | 68.4 | | 88.3 | 88.8 | 87.8 | 87.3 | 87.8 | 87.5 |
| drop50 | 58.1 | | 86.2 | 86.6 | 83.3 | 82.7 | 83.4 | 83.0 |
| shift | 74.6 | 74.1 | 88.9 | 89.5 | 89.7 | 89.4 | 89.7 | 89.5 |
| dh (feel-only) | 16.6 | | 63.2 (BC) | 63.3 (kNN) | | | 63.2 (HybDH_BC) | 63.6 (HybDH_kNN) |

## Bottom line for the paper
The only positive result that survives is narrow: used as a fallback when a goal-conditioned tabular BC has no confident match, the SW feel memory gives a small but consistent gain (about +1 pp, up to +3.6 pp with 2 teacher paths), on all 20 held-out worlds. It does not help kNN, does not help without the destination, and hurts when the goal signal drops out. This supports the paper's analysis framing (goal conditioning does the work) and should be reported as a modest, complementary-fallback effect, not as a rescue of SW.

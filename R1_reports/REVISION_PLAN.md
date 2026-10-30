# Revision R1 plan ([redacted] -> CSR / RAS)

Branch `revision-r1` (local only; push URL disabled). Original folders were not changed.

## Sanity
`--eval-baseline --seeds 5` rebuilt from the copy reproduces Phase A exactly (gain SW: A 75.15, B 80.84, C 71.53, D 69.29).

## Pilot (4 LOO terrains x 3 seeds, n=12 pairs, 6.5 min): see EvalLogs/pilot_primary/revision_stats.md
| policy | gain % vs EmptyLTM | 95% CI | p Holm (vs SW) |
|---|---|---|---|
| SuccessWeighted | 71.5 | [68.3, 75.0] | - |
| SW_NoSuccessWeight (ablation) | 70.2 | [66.2, 74.3] | 0.34 (n.s.) |
| SW_DestAnchor (ablation) | 65.4 | [57.9, 71.7] | 0.22 (n.s.) |
| KnnFeel (Hamming<=2, K=5) | 12.0 | [3.1, 20.7] | 0.0034 |
| TabularNgramBC | 3.2 | [-8.2, 14.9] | 0.0034 |
| EpisodicControl | -26.1 | [-46.3, -6.9] | 0.0034 |
| TabularQ (offline batch) | -53.3 | [-78.2, -27.3] | 0.0034 |

## Honest verdict
* SW clearly beats all the new baselines: kNN, episodic control and tabular Q. This is the paper's strongest new evidence.
* **Success weighting itself is NOT supported**: turning it off gives about the same result (70.2 vs 71.5, n.s.). In the 1-seed smoke run, no-weighting was even better on Terrain A. The title claim "Success-Weighted ... Improves" must be reworded. The gain comes from path-unit memory with source anchoring. Alternatively, wait for the 10-seed and procedural results to see if the effect shows up.
* Source vs dest anchoring: source is better by about 6 pp, but it is not yet significant.
* The value baselines (EC, Q) are weak partly by design: they only get offline teacher data and the robot only uses their action as a x4 boost. Say this in the paper, and do not present them as tuned RL.
* The 4-terrain set is small (n=12 pairs in the pilot), so the procedural-16 arm is essential.

## Not done
* Frequency-ranked replace ablation and the SW n-sweep both need changes to BrainMemory internals (n<=5 is hardcoded). The n-sweep is only run for the new baselines.
* DQN/PPO: design note only (see DQN_PPO_DESIGN.md).

## Full run
`F:\PaperSubmissions2026\EvolutionLife\Revision_R1\run_revision_eval.bat`. Estimated ~3 h at ~33 s per world-seed: primary ~22 min, procedural16 ~90 min, n-sweep ~45 min, hide-dest ~22 min.

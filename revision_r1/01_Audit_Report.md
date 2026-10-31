# LifeSim R1, Stage A audit report

Method audited: success-weighted (SW) feel-sequence memory in LifeSim.
Date: 30 Oct 2026 (IST). Machine: office PC, SSD at K:. Branch: revision-r1, based on 5139829.
Build: msbuild Release, `code\EvolutionApp\bin\Release\LifeSim.exe`. Run logs are in `Revision_R1\EvalLogs\stageA\`.

## Summary of verdicts

| # | Item | Verdict |
|---|------|---------|
| 1 | Reproduce the headline and DH numbers | **REPRODUCED, bit for bit** |
| 2 | Leakage (world ID, held-out data, DH goal info) | **No leak of held-out world or goal location. MAJOR CONFOUND:** in goal-known (GK) mode, SW retrieval uses goal-direction labels and the baselines do not. Remove that and the SW gain goes to about 0. |
| 3 | Baseline fairness | **Not fair as currently described.** SW gets goal direction, Hamming-loose matching and its own blend. Baselines get feel-only exact or kNN keys plus a fixed x4 vote. |
| 4 | Shuffled-action control | **Mostly collapses:** 71.5 → 15.3 % (EmptyLTM = 0) |
| 5 | Energy accounting and success rates | Formula confirmed. Goal rates are near 1.0 for all methods. The fail-penalty definition is odd (see below). |
| 6 | Reporting checks | **Mostly not present in the submitted docx.** Table 4 is complete but split across a page in the proof. Back-matter headings are misplaced in the proof. No "10^4" found. 251 vs 250 is consistent (250 noisy attempts + 1 Dijkstra path). |

## 1. Reproduction

- **20-world headline.** Recomputed from the original locked raw data (`code\EvalLogs\paper_results\baseline_phaseC_n25`, 1500 rows, n=25):
  - SW 74.82 ± 6.72
  - BC 19.71 ± 21.49
  - Note: the ± is the SD **across the 20 per-world means**, not across seeds. The paper should say so.
- **Fresh rerun.** Seed 101, all 20 worlds, `--eval-baseline --seeds 1 --worlds 20 --procedural 16` (`stageA\repro20_seed101`). All 60 rows (20 worlds × 3 policies) match the original seed-101 energy scores exactly (0 diffs). For this seed alone: SW 74.79 %, BC 26.47 %.
- **DH.** Rerun with `--eval-revision --hide-dest --seeds 5` (`stageA\dh_rev`):

  | World | SW gain % (mean ± SD) |
  |-------|-----------------------|
  | A | 19.7 ± 25.6 |
  | B | 46.3 ± 14.5 |
  | C | −24.5 ± 32.3 |
  | D | −5.0 ± 26.8 |

  This is identical to the paper's Table 5, Table 8 and Table 12.
- **Surprise in DH.** The simple baselines beat SW on every terrain:

  | Method | A | B | C | D |
  |--------|---|---|---|---|
  | TabularNgramBC | 29.4 | 67.4 | 31.9 | 27.5 |
  | kNN | 48.8 | 64.2 | 28.3 | 37.9 |

## 2. Leakage audit

**Dynamic checks:** `--audit-r1` (`EvolutionApp\AuditR1.cs`), output `stageA\audit_r1_v2\audit_r1.txt`. **ALL CHECKS PASSED** for the A–D hold-outs:
- The held-out world is not in the training list.
- Zero LTM units carry the held-out WorldId.
- STM is empty at episode start.
- LTM count and strength are unchanged during testing (frozen).
- The next episode starts clean.
- DH trajectories are identical when the goal is moved to the opposite corner. The GK control diverges at step 1–3, so goal info really is removed in DH.
  - Caveat: on B and D the DH comparison covers only 14 and 17 steps before a goal cell is touched.

**Static review:**
- **WorldId.** It is in `FeelAtlasKey` (`BrainMemory.cs:89-92`) and in retrieval pass 0 (`DirectionWeight`, which prefers the active-world shard). In LOO the held-out shard is stripped (`StripWorldShard`) and none ever exists, so pass 0 finds nothing and only the cross-shard pass 1 is used. **Not a leak.**
- **ProgressBin.** At test time it is step-based (`ProgressBinFromStep`, `BrainMemory.cs:737`). In training tours it is overwritten by a distance-to-goal fraction (`BackfillGoalDirectionsForTour`, `BrainMemory.cs:1171-1191`, which runs only when the goal is reached and only while learning). So there is no goal info at test, **but train and test use different definitions of the bin.** That is a methodological flaw to fix or disclose.
- **GoalDirection (the main issue).** Stored units carry the goal direction relative to the robot's heading, backfilled from the true goal. At GK test time `Robot.cs:462-463` passes `CurrentGoalDirection(world)`. `DirectionWeight` then weights matches by 2.5 × direction agreement versus 1.0 × progress (`BrainMemory.cs`, "situ" score). In DH both values are −1, so the info is correctly removed.
- **Control: SW_NoGoalCtx.** This masks the goal context from LTM retrieval only and keeps the reactive goal bias. Result (`stageA\shuffled_control`, 4 terrains × 3 seeds): **SW 71.5 % → −1.5 %** (range −35.0 to 31.6).
- **Conclusion.** This is not leakage of held-out data. It is privileged goal input that GK mode legitimately has. However, the whole GK gain comes from the goal-direction-conditioned retrieval, not from "local feel sequences" alone, and the baselines never get that input. The paper's framing must change, or the baselines must get the same goal context. Per the instruction, this is reported as a critical confound but not as a leak, so work continued.

## 3. Baseline fairness matrix

| Method | Training data | Observation at test | Capacity (keys, pilot mean) | Matching | Tuning | Online/offline | How it acts |
|---|---|---|---|---|---|---|---|
| SW (ours) | the same teacher successes (250 noisy attempts + 1 Dijkstra path per train world) | feel n-gram 3–5 **+ goal direction + progress bin** | 512 (LTM cap, full) | Hamming ≤1–2, suffix/window, cross-shard 0.55 | hand-tuned over the V13–V16 history | offline build, frozen at test | its own confidence-gated multiplicative blend |
| SW_NoSuccessWeight / SW_DestAnchor | same | same | 512 | same | none | offline | same |
| EmptyLTM | none | reactive terrain + goal bias | 0 | – | – | – | reactive |
| TabularNgramBC (n=25 seeds) | same successes, flat counts | feel n-gram only | 237 | exact, longest first | none | offline | x4 weight on the matched action |
| kNN | same | feel n-gram L=5 | 148 | K=5, Hamming ≤2 | defaults | offline | x4 |
| Episodic control | same, includes the fail penalty | feel L=5 | 148 | K=5, Hamming ≤2 | defaults | **offline only** | x4 |
| Tabular Q | all teacher transitions, 30 batch sweeps, α 0.2, γ 0.99 | feel L=5 | 161 | exact | defaults | **offline only** | x4 |

Fairness gaps to fix or disclose:
- (a) Only SW gets goal-direction and progress context.
- (b) SW uses loose matching; BC uses exact matching.
- (c) SW has its own blending rule; the baselines use a fixed x4 vote.
- (d) EC and Q are offline-only with an x4 boost, so they are not tuned RL (`REVISION_PLAN.md` already says this).
- (e) No baseline was tuned.

Fix: give the baselines the same goal-dir and progress context, or report SW_NoGoalCtx as the like-for-like comparison.

## 4. Shuffled-action control

**What it does:** permutes MoveDir across all used LTM units. Keys stay intact. Implemented as `BrainMemory.ShuffleLtmMoves` and harness policy `SW_ShuffledActions`.
**Setup:** 4 terrains × 3 seeds (101/202/303), 30 runs each.

| Policy | Mean gain % | Range |
|---|---|---|
| SuccessWeighted | 71.5 | 64.0 to 85.3 |
| SW_ShuffledActions | 15.3 | −26.6 to 60.4 |
| SW_NoGoalCtx | −1.5 | −35.0 to 31.6 |
| EmptyLTM | 0 | – |

Verdict: performance largely collapses, so the stored actions matter. The residual +15 % with high variance suggests that part of the effect is only the memory-confidence gating of the reactive policy.

## 5. Energy accounting and success

- Cell cost = 0.2 + 0.8 × roughness, or 2.0 on an obstacle (`World.cs:108`).
- EnergyScore = (energy-to-goal sum + timeouts × mean decision steps per run) / runs (`Evaluator.cs`).
- Gain % = 100 × (Empty − Policy) / Empty, paired by seed and world.
- **Odd:** the fail penalty is set to the mean decision steps (a step count, not an energy), so it can be smaller than the true energy of a failed run. Its impact is small because timeouts are rare, but it should be stated or fixed.
- **Goal rates, original n=25 over 20 worlds:**
  - SW 1.000 (minimum 1.0)
  - BC 1.000
  - EmptyLTM 0.990 (minimum 0.733)
- **Goal rates, DH:**
  - SW 0.96–0.99
  - BC 0.98–1.00
  - kNN 1.00
  - Empty 0.74–0.98
- The gains are pure path efficiency, not goal reaching. Report the goal rates in the paper.

## 6. Reporting checks (earlier draft)

Checked in the submitted docx (`LifeSim_Paper_SUBMIT.docx`, 15 tables) and in the proof PDF:
- **Table 4:** full in the docx (A–D rows: 75.2, 80.8, 71.5, 69.3). In the proof the header sits at the bottom of page 8 and the rows are on page 9. It *looks* empty: this is a layout defect, not missing data.
- **Tables 8, 9 and 12:** structurally valid in the docx (5×9, 5×3, 9×7, no empty cells). In the proof, narrow columns wrap the mechanism and sensing labels across many lines, which looks malformed. Fix by using short labels or landscape layout.
- **"10^4":** not found in the docx or the proof text. It may be in an equation object or image; could not confirm.
- **251 vs 250:** not a contradiction. The text says 250 noisy attempts plus 1 Dijkstra path, which gives 251 successes. Add one clarifying sentence.
- **Back-matter:** in the proof, each heading (Ethics Approval, Data Availability, Funding, Conflict of Interest) is printed *after* its text, and there is also a "Statements and Declarations" heading. The data/code availability text appears twice (once in the body section and once under Data Availability). Real duplication.

## Code changes in this commit (audit only; no change to paper-method behaviour)

- `BrainMemory.cs`: audit helpers (`LtmCountForWorld`, `LtmWorldIds`, `LtmStrengthSum`, `ShuffleLtmMoves`).
- `Robot.cs`: `MaskBrainGoalContext` flag. Default is off.
- `RevisionHarness.cs` and `RevisionBaselines.cs`: new policies `SW_ShuffledActions` and `SW_NoGoalCtx`.
- `Program.cs` and `EvolutionLife.csproj`: the `--audit-r1` entry point.
- New file `AuditR1.cs`.
- The default paths are unchanged, and the seed-101 20-world rerun is identical to the locked logs.

## Recommendation before Stage B

The goal-direction confound decides the paper's story. Either:
- (a) reframe the method as "goal-conditioned feel memory" and give every baseline the same goal context; or
- (b) drop the goal context from SW. Then SW is about the same as EmptyLTM in GK, and kNN and BC beat it in DH.

Resolve this with the author before the 3-hour full run.

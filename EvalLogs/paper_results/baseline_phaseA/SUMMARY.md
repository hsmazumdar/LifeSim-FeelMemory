# Phase A — Baseline LOO (SuccessWeighted vs TabularNgramBC vs EmptyLTM)

Date: 2026-09-25 22:56 IST
Wall clock: 5.2 min

## Protocol
- LOO goal-known on WorldLibrary.Primary = Terrain-A..D
- Seeds: LooSeedList n=5 = {101,202,303,404,505}
- TeacherAttempts=250, EvalRuns=30, MaxSteps=2000
- Policies: **SuccessWeighted** (feel-atlas LTM), **TabularNgramBC** (flat majority n-gram to action), **EmptyLTM** (reactive / no-memory)
- BC trained from the **same** TeacherSuccess trajectories as SW (joint train); BC uses flat counts, no success-weight promotion.
- Feel encoding: source cell; Smooth<=0.30, Rough>=0.65; n-gram len 3..5; action = relative move at window end.
- BC test: exact key lookup (prefer longest); miss -> EmptyLTM reactive.
- GainPct = 100*(EmptyEnergyScore - PolicyEnergyScore)/EmptyEnergyScore (lower energy_score is better).

## NOTES
- Mechanism names in logs: SuccessWeighted, TabularNgramBC, EmptyLTM.
- No PPO/DQN. Simple learned baseline only.
- Reviewer artifact: LifeSim.exe --eval-baseline.

### Optimal reference
```
world,start_x,start_y,goal_x,goal_y,opt_energy,opt_steps,greedy_energy,greedy_steps,greedy_over_opt_pct
Terrain-A,5,19,13,0,5.6267,21,10.3169,19,83.36
Terrain-B,0,3,19,17,6.7033,19,7.1852,19,7.19
Terrain-C,3,0,16,19,6.6059,21,9.0786,19,37.43
Terrain-D,17,5,0,13,5.2675,18,9.742,17,84.94
```

### LOO baseline summary (GainPct vs EmptyLTM)
```
world,n,SW_energy_mean,SW_energy_std,BC_energy_mean,BC_energy_std,Empty_energy_mean,Empty_energy_std,SW_gain_pct_vs_Empty_mean,SW_gain_pct_vs_Empty_std,BC_gain_pct_vs_Empty_mean,BC_gain_pct_vs_Empty_std,SW_minus_BC_gain_pp_mean,SW_goal_mean,BC_goal_mean,Empty_goal_mean
Terrain-A,5,30.0858,4.4812,114.8386,20.1226,122.6493,14.8352,75.1505,5.0783,5.4223,20.3386,69.7283,1,1,1
Terrain-B,5,25.8377,4.7168,95.1649,19.9817,138.746,26.8339,80.838,4.8691,29.603,19.4121,51.235,1,1,0.9933
Terrain-C,5,25.5969,3.7894,95.2688,9.9033,92.2084,13.686,71.5314,6.6912,-5.9261,22.6438,77.4574,1,1,1
Terrain-D,5,28.4408,3.2827,95.7539,16.5779,93.2702,11.7802,69.2862,3.9649,-2.6267,11.5636,71.9129,1,1,1
```

## Result table (GainPct mean+/-std vs EmptyLTM)

| World | SuccessWeighted | TabularNgramBC | Empty energy (mean) | SW beats BC? |
|-------|-----------------|----------------|---------------------|--------------|
| Terrain-A | 75.15 +/- 5.08 | 5.42 +/- 20.34 | 122.65 | YES |
| Terrain-B | 80.84 +/- 4.87 | 29.60 +/- 19.41 | 138.75 | YES |
| Terrain-C | 71.53 +/- 6.69 | -5.93 +/- 22.64 | 92.21 | YES |
| Terrain-D | 69.29 +/- 3.96 | -2.63 +/- 11.56 | 93.27 | YES |

## Verdict (short)
SuccessWeighted beats TabularNgramBC on **4/4** worlds (SW-BC gap ~51–77 pp). BC is near-zero or slightly negative vs Empty on C/D; modest positive on A/B.
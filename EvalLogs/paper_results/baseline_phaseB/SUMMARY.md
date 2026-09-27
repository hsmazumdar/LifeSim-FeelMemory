# Phase B - Baseline LOO (SuccessWeighted vs TabularNgramBC vs EmptyLTM)

Date: 2026-09-25 23:13 IST
Wall clock: 12.9 min

## Protocol
- LOO goal-known on WorldLibrary.Primary = Terrain-A..D
- Seeds: BuildLooSeedList n=25 = {101,202,303,404,505,606,707,808,909,1010,1111,1212,1313,1414,1515,1616,1717,1818,1919,2020,2121,2222,2323,2424,2525}
- TeacherAttempts=250, EvalRuns=30, MaxSteps=2000
- Policies: **SuccessWeighted** (feel-atlas LTM), **TabularNgramBC** (flat majority n-gram->action), **EmptyLTM** (reactive / no-memory)
- BC trained from the **same** TeacherSuccess trajectories as SW (joint train); BC uses flat counts, no success-weight promotion.
- Feel encoding: source cell; Smooth<=0.30, Rough>=0.65; n-gram len 3..5; action = relative move at window end.
- BC test: exact key lookup (prefer longest); miss -> EmptyLTM reactive.
- GainPct = 100*(EmptyEnergyScore - PolicyEnergyScore)/EmptyEnergyScore (lower energy_score is better).

## NOTES
- Mechanism names in logs: SuccessWeighted, TabularNgramBC, EmptyLTM.
- No PPO/DQN. Simple learned baseline only.
- Reviewer artifact: LifeSim.exe --eval-baseline.
- Seed failures: 0 / 100 seed-world cells. Raw CSV rows: 300 (4 worlds x 25 seeds x 3 policies).

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
Terrain-A,25,31.8301,4.9227,119.703,23.5542,132.6725,21.4448,75.3986,5.3785,7.5546,24.3231,67.844,1,1,1
Terrain-B,25,28.4515,8.0627,91.7012,14.7922,140.3867,21.1617,79.2026,6.8534,33.5443,12.8789,45.6584,1,1,0.9933
Terrain-C,25,27.1068,5.7365,91.2957,12.2554,90.6204,11.784,69.454,8.0752,-2.5014,20.582,71.9555,1,1,1
Terrain-D,25,29.1317,4.4079,94.9585,17.373,95.6185,12.4173,69.2305,5.0214,-0.0748,17.2612,69.3054,1,1,1
```
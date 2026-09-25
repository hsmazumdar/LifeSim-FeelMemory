# LifeSim V16 Review Evaluation

Wall clock: 14.3 min

## Protocol
- TeacherSuccessâ†’LTM (goal-known teacher; failures discarded).
- Fix2 honesty: ClearStm / CloneFrozenLtmOnly; EvalFrozenPaired; temp mem.
- Arms: in-sample + LOO Ã— {goal-known, dest-hidden}; transfer A; empty-LTM ablation.
- Worlds: WorldLibrary.Primary = Terrain-A..D. Sense: local 8-neighbor.

## Files
- ablation_empty_ltm_raw.csv
- ablation_empty_ltm_summary.csv
- loo_desthidden_raw.csv
- loo_desthidden_raw_Terrain-A_101.log
- loo_desthidden_raw_Terrain-A_202.log
- loo_desthidden_raw_Terrain-A_303.log
- loo_desthidden_raw_Terrain-A_404.log
- loo_desthidden_raw_Terrain-A_505.log
- loo_desthidden_raw_Terrain-B_101.log
- loo_desthidden_raw_Terrain-B_202.log
- loo_desthidden_raw_Terrain-B_303.log
- loo_desthidden_raw_Terrain-B_404.log
- loo_desthidden_raw_Terrain-B_505.log
- loo_desthidden_raw_Terrain-C_101.log
- loo_desthidden_raw_Terrain-C_202.log
- loo_desthidden_raw_Terrain-C_303.log
- loo_desthidden_raw_Terrain-C_404.log
- loo_desthidden_raw_Terrain-C_505.log
- loo_desthidden_raw_Terrain-D_101.log
- loo_desthidden_raw_Terrain-D_202.log
- loo_desthidden_raw_Terrain-D_303.log
- loo_desthidden_raw_Terrain-D_404.log
- loo_desthidden_raw_Terrain-D_505.log
- loo_desthidden_summary.csv
- loo_goalknown_raw.csv
- loo_goalknown_raw_Terrain-A_101.log
- loo_goalknown_raw_Terrain-A_202.log
- loo_goalknown_raw_Terrain-A_303.log
- loo_goalknown_raw_Terrain-A_404.log
- loo_goalknown_raw_Terrain-A_505.log
- loo_goalknown_raw_Terrain-B_101.log
- loo_goalknown_raw_Terrain-B_202.log
- loo_goalknown_raw_Terrain-B_303.log
- loo_goalknown_raw_Terrain-B_404.log
- loo_goalknown_raw_Terrain-B_505.log
- loo_goalknown_raw_Terrain-C_101.log
- loo_goalknown_raw_Terrain-C_202.log
- loo_goalknown_raw_Terrain-C_303.log
- loo_goalknown_raw_Terrain-C_404.log
- loo_goalknown_raw_Terrain-C_505.log
- loo_goalknown_raw_Terrain-D_101.log
- loo_goalknown_raw_Terrain-D_202.log
- loo_goalknown_raw_Terrain-D_303.log
- loo_goalknown_raw_Terrain-D_404.log
- loo_goalknown_raw_Terrain-D_505.log
- loo_goalknown_summary.csv
- optimal_reference.csv
- teacher_insample_desthidden_raw.csv
- teacher_insample_desthidden_raw_seed_11.log
- teacher_insample_desthidden_raw_seed_22.log
- teacher_insample_desthidden_raw_seed_33.log
- teacher_insample_desthidden_raw_seed_44.log
- teacher_insample_desthidden_raw_seed_55.log
- teacher_insample_desthidden_summary.csv
- teacher_insample_raw.csv
- teacher_insample_raw_seed_11.log
- teacher_insample_raw_seed_22.log
- teacher_insample_raw_seed_33.log
- teacher_insample_raw_seed_44.log
- teacher_insample_raw_seed_55.log
- teacher_insample_summary.csv
- transfer_baseline_raw.csv
- transfer_baseline_summary.csv
- transfer_raw_seed_17.log
- transfer_raw_seed_27.log
- transfer_raw_seed_7.log

### Optimal reference
```
world,start_x,start_y,goal_x,goal_y,opt_energy,opt_steps,greedy_energy,greedy_steps,greedy_over_opt_pct
Terrain-A,5,19,13,0,5.6267,21,10.3169,19,83.36
Terrain-B,0,3,19,17,6.7033,19,7.1852,19,7.19
Terrain-C,3,0,16,19,6.6059,21,9.0786,19,37.43
Terrain-D,17,5,0,13,5.2675,18,9.742,17,84.94
```

### Teacher in-sample (goal-known)
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,5,78.6323,4.2822,0,0,4.4894,0.9899,21.084,3.1726
Terrain-B,5,78.0599,2.3886,0,0,4.5299,0.9236,20.5721,2.9239
Terrain-C,5,61.3334,11.4259,0,0,5.2579,1.1191,13.9141,2.398
Terrain-D,5,60.9844,6.3898,0,0,7.4157,1.1409,19.0499,0.9219
```

### Teacher in-sample (dest-hidden)
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,5,11.9402,14.5779,4.6667,1.8257,46.553,2.2792,52.5472,7.2832
Terrain-B,5,57.821,1.9195,19.3333,6.4118,30.6198,2.1666,58.2405,1.2251
Terrain-C,5,-5.3334,29.7729,1.3333,5.0553,35.5855,7.4079,34.0485,4.823
Terrain-D,5,-31.6066,25.2678,-0.6667,1.4907,54.2281,8.6072,40.8747,3.4471
```

### LOO goal-known
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,5,75.1505,5.0783,0,0,5.347,0.7964,21.7979,2.6366
Terrain-B,5,80.838,4.8691,0.6667,1.4907,3.8545,0.7037,20.8481,4.2374
Terrain-C,5,71.5314,6.6912,0,0,3.8749,0.5736,13.9585,2.0718
Terrain-D,5,69.2862,3.9649,0,0,5.3993,0.6232,17.7067,2.2364
```

### LOO dest-hidden
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,5,19.6527,25.6157,5.3333,8.6923,46.4124,13.3085,57.0654,7.9044
Terrain-B,5,46.2888,14.5,22.6667,13.6219,41.1275,5.407,59.0468,8.992
Terrain-C,5,-24.5274,32.3117,-0.6667,7.6012,40.9045,3.4236,33.69,6.6101
Terrain-D,5,-5.0082,26.8043,-2,6.4979,47.0783,7.837,43.0873,6.006
```

### Transfer train A
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,3,83.947,4.0132,0,0,3.4815,0.591,22.0227,2.0103
Terrain-B,3,81.7142,2.2114,1.1111,1.9245,3.9663,0.5615,22.0874,1.1944
Terrain-C,3,73.7992,1.5126,0,0,4.029,0.7145,15.3949,2.8202
Terrain-D,3,63.1629,1.7517,0,0,6.0636,0.1627,16.4967,1.1706
```

### Ablation empty LTM
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,5,0,0,0,0,21.9282,2.8946,21.9282,2.8946
Terrain-B,5,0,0,0,0,19.9132,1.0921,19.9132,1.0921
Terrain-C,5,0,0,0,0,13.3681,0.9422,13.3681,0.9422
Terrain-D,5,0,0,0,0,18.7896,3.4843,18.7896,3.4843
```


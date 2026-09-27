# Phase C - Baseline LOO (SuccessWeighted vs TabularNgramBC vs EmptyLTM)

Date: 2026-09-27 01:11 IST
Wall clock: 645.7 min

## Protocol
- LOO goal-known on 20 worlds (procedural=16, baseSeed=9001): Terrain-A,Terrain-B,Terrain-C,Terrain-D,Terrain-01,Terrain-02,Terrain-03,Terrain-04,Terrain-05,Terrain-06,Terrain-07,Terrain-08,Terrain-09,Terrain-10,Terrain-11,Terrain-12,Terrain-13,Terrain-14,Terrain-15,Terrain-16
- Seeds: BuildLooSeedList/LooSeedList n=25 = {101,202,303,404,505,606,707,808,909,1010,1111,1212,1313,1414,1515,1616,1717,1818,1919,2020,2121,2222,2323,2424,2525}
- Phase C note: n=25 full LOO is overnight follow-up; this run uses n=25.
- TeacherAttempts=250, EvalRuns=30, MaxSteps=2000
- Policies: **SuccessWeighted** (feel-atlas LTM), **TabularNgramBC** (flat majority n-gram→action), **EmptyLTM** (reactive / no-memory)
- BC trained from the **same** TeacherSuccess trajectories as SW (joint train); BC uses flat counts, no success-weight promotion.
- Feel encoding: source cell; Smooth≤0.30, Rough≥0.65; n-gram len 3..5; action = relative move at window end.
- BC test: exact key lookup (prefer longest); miss → EmptyLTM reactive.
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
Terrain-01,5,19,13,0,6.1146,19,7.815,19,27.81
Terrain-02,0,3,19,17,6.295,23,10.0675,19,59.93
Terrain-03,3,0,16,19,5.0047,20,5.3369,19,6.64
Terrain-04,17,5,0,13,6.3931,22,7.891,17,23.43
Terrain-05,5,19,13,0,5.6626,22,8.468,19,49.54
Terrain-06,0,3,19,17,6.0779,25,7.9637,19,31.03
Terrain-07,3,0,16,19,5.953,21,7.4147,19,24.55
Terrain-08,17,5,0,13,6.3607,17,8.1644,17,28.36
Terrain-09,5,19,13,0,6.0182,19,9.1968,19,52.82
Terrain-10,0,3,19,17,6.2009,23,9.5746,19,54.41
Terrain-11,3,0,16,19,7.2104,24,11.8916,19,64.92
Terrain-12,17,5,0,13,5.7957,18,8.0365,17,38.66
Terrain-13,5,19,13,0,7.9201,26,11.981,19,51.27
Terrain-14,0,3,19,17,7.3825,19,8.5379,19,15.65
Terrain-15,3,0,16,19,6.7826,19,9.0136,19,32.89
Terrain-16,17,5,0,13,6.3385,17,7.5511,17,19.13
```

### LOO baseline summary (GainPct vs EmptyLTM)
```
world,n,SW_energy_mean,SW_energy_std,BC_energy_mean,BC_energy_std,Empty_energy_mean,Empty_energy_std,SW_gain_pct_vs_Empty_mean,SW_gain_pct_vs_Empty_std,BC_gain_pct_vs_Empty_mean,BC_gain_pct_vs_Empty_std,SW_minus_BC_gain_pp_mean,SW_goal_mean,BC_goal_mean,Empty_goal_mean
Terrain-A,25,32.6184,5.2249,101.0984,13.9089,132.6725,21.4448,74.816,5.5206,21.5398,17.843,53.2762,1,1,1
Terrain-B,25,28.5492,8.2869,92.3445,14.5708,140.3867,21.1617,79.1181,7.0684,32.9997,13.5928,46.1185,1,1,0.9933
Terrain-C,25,27.0475,5.7555,85.3643,13.4466,90.6204,11.784,69.5459,7.9916,4.9733,15.1952,64.5726,1,1,1
Terrain-D,25,28.9539,4.4198,85.2068,11.7985,95.6185,12.4173,69.4292,4.9618,9.6722,15.9414,59.7569,1,1,1
Terrain-01,25,26.7153,4.5681,88.0709,18.5252,82.0497,12.8244,66.8164,7.0134,-9.5005,27.6304,76.3169,1,1,1
Terrain-02,25,29.7965,6.4616,90.3828,16.1429,128.5902,19.335,76.3158,6.4831,28.1981,16.6261,48.1176,1,1,1
Terrain-03,25,24.68,4.8209,86.5852,11.6975,88.5183,10.9795,71.9228,5.2284,0.5524,19.0393,71.3704,1,1,1
Terrain-04,25,29.9122,5.7547,87.0646,16.9801,88.9892,13.0523,65.6563,8.1934,0.7601,21.1745,64.8961,1,1,1
Terrain-05,25,27.3074,4.2009,93.8745,14.177,96.7452,10.8102,71.5675,4.829,1.6814,19.2424,69.8861,1,1,1
Terrain-06,25,27.7231,5.7768,88.2979,12.863,98.0864,12.3435,71.4735,5.9291,8.2241,20.0044,63.2494,1,1,1
Terrain-07,25,27.1683,8.5451,84.5632,11.37,140.4486,22.037,80.3189,6.5613,38.4177,12.4313,41.9012,1,1,0.996
Terrain-08,25,26.9554,4.44,81.948,14.1967,85.3638,10.6376,68.1725,5.0204,2.7084,20.6422,65.4641,1,1,1
Terrain-09,25,27.3086,4.8246,94.6653,12.7054,99.8942,13.8927,72.3914,4.8137,2.9937,21.6996,69.3978,1,1,1
Terrain-10,25,30.2432,4.6582,97.0184,16.1554,119.4666,18.4076,74.1926,4.9907,17.0972,17.7548,57.0954,1,1,1
Terrain-11,25,32.2845,5.823,97.556,12.4762,263.9236,45.4761,87.4551,2.7525,62.2309,6.7915,25.2242,1,1,0.9427
Terrain-12,25,27.9289,3.3903,91.477,19.0157,118.2591,16.4103,76.042,3.7302,21.9614,15.7688,54.0806,1,1,1
Terrain-13,25,31.815,5.2733,98.2822,14.8238,106.2947,13.3221,69.5724,6.8772,6.221,18.1232,63.3513,1,1,1
Terrain-14,25,31.5872,8.8278,95.0552,16.0594,173.0913,26.5369,81.4981,5.3444,43.8748,12.5817,37.6233,1,1,0.9907
Terrain-15,25,29.5151,6.2689,99.0342,14.5593,142.7824,20.7864,78.9153,5.2065,29.0519,15.9125,49.8634,1,1,0.996
Terrain-16,25,27.7074,5.4929,93.6827,14.2948,332.1576,73.1403,91.2712,2.7604,70.495,8.0363,20.7762,1,1,0.8867
```


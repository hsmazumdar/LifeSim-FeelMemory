# LifeSim V16 â€” Verdict

Date: 2026-09-25 13:01 IST
Wall clock: 14.3 min

## Design
- TeacherSuccessâ†’LTM; policy `brain-feel-atlas`; feel n-gram atlas (no 8-neigh primary).
- Fix2 honesty preserved. Terrain Primary; Canvas retained in All.

## Fix2 LOO bar (Prefâ†’LTM, goal-known)
| World | Fix2 LOO % | â‰¥25%? |
|-------|------------|-------|
| A | 27.1 Â± 12.1 | YES |
| B | 4.0 Â± 4.8 | NO |
| C | 30.1 Â± 16.8 | YES |
| D | 13.5 Â± 6.1 | NO |
Bar (â‰¥25% on â‰¥2/4): **MET on Fix2** (A+C).

## V16 LOO goal-known
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,5,75.1505,5.0783,0,0,5.347,0.7964,21.7979,2.6366
Terrain-B,5,80.838,4.8691,0.6667,1.4907,3.8545,0.7037,20.8481,4.2374
Terrain-C,5,71.5314,6.6912,0,0,3.8749,0.5736,13.9585,2.0718
Terrain-D,5,69.2862,3.9649,0,0,5.3993,0.6232,17.7067,2.2364
```

## V16 LOO dest-hidden
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,5,19.6527,25.6157,5.3333,8.6923,46.4124,13.3085,57.0654,7.9044
Terrain-B,5,46.2888,14.5,22.6667,13.6219,41.1275,5.407,59.0468,8.992
Terrain-C,5,-24.5274,32.3117,-0.6667,7.6012,40.9045,3.4236,33.69,6.6101
Terrain-D,5,-5.0082,26.8043,-2,6.4979,47.0783,7.837,43.0873,6.006
```

## Ablation
```
world,n,energy_gain_pct_mean,energy_gain_pct_std,goal_rate_gain_pp_mean,goal_rate_gain_pp_std,brain_gap_to_opt_mean,brain_gap_to_opt_std,nobrain_gap_to_opt_mean,nobrain_gap_to_opt_std
Terrain-A,5,0,0,0,0,21.9282,2.8946,21.9282,2.8946
Terrain-B,5,0,0,0,0,19.9132,1.0921,19.9132,1.0921
Terrain-C,5,0,0,0,0,13.3681,0.9422,13.3681,0.9422
Terrain-D,5,0,0,0,0,18.7896,3.4843,18.7896,3.4843
```


## Honest verdict
Compare V16 LOO means above to Fix2 bar. Dest-hidden tests whether success-path features alone guide without goal coordinates.

## Build
- `LifeSim.sln` â†’ `EvolutionApp\bin\Debug\LifeSim.exe`

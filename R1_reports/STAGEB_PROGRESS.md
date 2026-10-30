# Stage B progress (2026-10-30 10:16 IST)
- Code committed locally as WIP: ccdf0da (branch revision-r1). Not pushed. docx deletions left unstaged.
- Pilot (seeds 101,202,303 x 20 worlds): runner.ps1 PID 23812 alive; 14 worlds running (2/3 jobs done each at 10:15), 6 worlds (Terrain-11..16) queued.
- Under 14-way contention one job (1 world x 1 seed) takes ~20-25 min, so 3-seed chunks are ~70 min (> 40 min target). Full grid plan: reuse pilot seeds, run seeds 404..1010 as 1-seed chunks (~20-25 min each): run_stageB.ps1 -Tag full -SeedGroups 404,505,... -MaxPar 14. Runner skips finished chunks, so re-launching resumes.
- Stats: stageb_stats.ps1 (bootstrap loop-var bug fixed: $b shadowed $B).

- 10:54 IST: pilot 14/20 worlds complete, 6 finishing; partial pilot stats in EvalLogs\stageB\pilot\stats_partial.md (GC kNN/BC ~89% gain > SW ~75% in GK; SW last-ish in DH). Full grid launched 10:53 IST as 1-seed chunks, seeds 404..1010 (pilot supplies 101,202,303), MaxPar 8, EvalLogs\stageB\full. To resume: re-run the same run_stageB.ps1 command (finished chunks skipped).

- 11:06 IST: pilot 20/20 done. Full runner alive (PID 17668), 16/140 chunks done, ~5 min/chunk at 8-par; ETA ~12:30 IST.
- 11:06 IST: full 16/140 chunks done, runner alive=True
- 11:19 IST: full 32/140 chunks done, runner alive=True
- 11:32 IST: full 40/140 chunks done, runner alive=True
- 11:45 IST: full 48/140 chunks done, runner alive=True
- 11:56 IST: full 58/140 chunks done, runner alive=True
- 12:09 IST: full 72/140 chunks done, runner alive=True
- 12:22 IST: full 80/140 chunks done, runner alive=True
- 12:35 IST: full 88/140 chunks done, runner alive=True
- 12:44 IST: PAUSED by user (moving SSD to home PC). Runner 17668 and all LifeSim jobs killed. About 88-96/140 chunks done; the 8 in-flight chunks were killed mid-run, so check each full\ chunk for a complete stageb_raw.csv (and delete incomplete ones) before relaunching run_stageB.ps1 -Tag full. 

- 15:17 IST: moved to Vivobook (E:). Exe predates/matches ccdf0da source. Root path now $PSScriptRoot. Deleted 8 unfinished chunks. Runner relaunched MaxPar 10 (22 logical CPUs). Repro test of Terrain-A s404 in EvalLogs\stageB\repro_A404.
- 15:36 IST: repro Terrain-A s404 on Vivobook is identical to office-PC CSV (29/29 rows, excluding timing cols). full 106/140.
- 15:47 IST: full 126/140
- 15:53 IST: full 126/140
- 15:57 IST: stageB full 136/140. Sensitivity sweep (item b) code added (SensHarness.cs, GoalDirWeight, LtmCap, GC MaxKeys), built to bin\Sens\ (Release exe untouched), 12 jobs launched -> EvalLogs\sens\.
- 16:05 IST: stageB full 140/140
- 16:06 IST: Stage B 140/140 done; stats_all10.md + 02_StageB_Report.md written.
- 16:07 IST: (a) docx paper lists updated (ASOC + LifeSim row + 30 Oct line). Commits 6d06831, e1fe57c.
- 16:07 IST: (c) 04_Paper_Fix_List.md written. Sens jobs at cap=256.
- 16:38 IST: (b) sensitivity sweeps done (12/12); 03_Sensitivity_Report.md written. All tasks done; laptop idle.

- 18:13 IST: POSITIVE-GAIN experiments. Lid action set to 0 (saved: power_lid_before_posgain.txt; restore lid=1 AC/DC). Code: Robot.cs hybrid+goal-noise hooks, GoalCondBaselines IConfPolicy, PosGainHarness.cs (--eval-posgain), built to bin\PosGain (Release untouched). Backup of pre-change source: backup_posgain_pre\. Pilot launched (A-D x 101/202/303): run_posgain.ps1 -Tag pilot -SeedGroups '101;202;303' -MaxPar 12 -Only Terrain-A,Terrain-B,Terrain-C,Terrain-D (re-run same command to resume; finished chunks skipped).

- 2026-10-30 18:32 IST pos-gain pilot done (A-D x 101/202/303, 13.5 min). Promising: Hyb_BC_GC (+1.0..+5.3pp vs BC_GC, 4/4 in base/k2/k5/k10/shift/noise25), HybDH_kNN (+1.7pp). SW < GC baselines everywhere (-9..-31pp). Hybrid hurts under goal drop. Launching full grid: run_posgain.ps1 -Tag full (10 seeds x 20 worlds).

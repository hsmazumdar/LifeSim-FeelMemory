@echo off
REM Revision R1 full evaluation. Usage: run_revision_eval.bat [pilot]
set R=F:\PaperSubmissions2026\EvolutionLife\Revision_R1
set EXE=%R%\code\EvolutionApp\bin\Release\LifeSim.exe
set PY=C:\Python312\python.exe
if /I "%1"=="pilot" (
  "%EXE%" --eval-revision --seeds 3 --out "%R%\EvalLogs\pilot_primary"
  "%PY%" "%R%\analysis\revision_stats.py" "%R%\EvalLogs\pilot_primary"
  goto :eof
)
"%EXE%" --eval-revision --seeds 10 --out "%R%\EvalLogs\primary_L5"
"%EXE%" --eval-revision --seeds 10 --procedural --worlds 16 --base-seed 9001 --out "%R%\EvalLogs\procedural16_L5"
for %%L in (3 8 10) do "%EXE%" --eval-revision --seeds 10 --no-ablations --ngram %%L --policies KnnFeel,EpisodicControl,TabularQ,EmptyLTM --out "%R%\EvalLogs\primary_ngram%%L"
"%EXE%" --eval-revision --seeds 10 --hide-dest --out "%R%\EvalLogs\primary_hidedest"
for /D %%D in ("%R%\EvalLogs\primary_*" "%R%\EvalLogs\procedural16_*") do "%PY%" "%R%\analysis\revision_stats.py" "%%D"

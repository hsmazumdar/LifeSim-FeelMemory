param([string]$Tag, [string[]]$SeedGroups, [int]$MaxPar = 10, [string]$Only)
# Bounded-chunk Stage B runner: one LifeSim process per (held-out world, seed group); each chunk ~<=25 min.
$root = $PSScriptRoot
$exe = Join-Path $root 'code\EvolutionApp\bin\PosGain\LifeSim.exe'
$worlds = @('Terrain-A','Terrain-B','Terrain-C','Terrain-D') + (1..16 | ForEach-Object { 'Terrain-{0:00}' -f $_ })
if ($Only) { $worlds = $Only.Split(",") }
$SeedGroups = @($SeedGroups | % { $_.Split(";") } | ? { $_ })
$queue = New-Object System.Collections.Queue
foreach ($w in $worlds) { foreach ($g in $SeedGroups) { $queue.Enqueue(@($w, $g)) } }
$running = @()
$log = Join-Path $root "EvalLogs\posgain\$Tag\runner.log"
New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
"start $(Get-Date -Format s) chunks=$($queue.Count) maxpar=$MaxPar" | Add-Content $log
while ($queue.Count -gt 0 -or $running.Count -gt 0) {
  $running = @($running | Where-Object { -not $_.HasExited })
  while ($queue.Count -gt 0 -and $running.Count -lt $MaxPar) {
    $j = $queue.Dequeue(); $w = $j[0]; $g = $j[1]
    $out = Join-Path $root ("EvalLogs\posgain\$Tag\" + $w + '_s' + ($g -replace ',', '-'))
    if (Test-Path (Join-Path $out 'run_meta.txt')) { if ((Get-Content (Join-Path $out 'run_meta.txt') -Raw) -match 'finished=') { "skip $w $g" | Add-Content $log; continue } else { Remove-Item -Recurse -Force $out } }
    New-Item -ItemType Directory -Force $out | Out-Null
    $p = Start-Process -FilePath $exe -ArgumentList @('--eval-posgain','--seed-list',$g,'--world-filter',$w,'--out',$out) -RedirectStandardOutput (Join-Path $out 'console.txt') -WindowStyle Hidden -PassThru
    $running += $p
    "launch $(Get-Date -Format s) $w $g pid=$($p.Id)" | Add-Content $log
  }
  Start-Sleep -Seconds 15
}
"done $(Get-Date -Format s)" | Add-Content $log



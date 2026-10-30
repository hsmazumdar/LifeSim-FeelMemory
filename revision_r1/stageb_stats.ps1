param([string]$InDir, [string]$OutMd, [int]$Boot = 10000)
# R1 Stage B stats (PowerShell only). Unit of analysis = held-out world (gain averaged over seeds), n = #worlds.
# Gain% = 100*(EmptyLTM - policy)/EmptyLTM on energy_score, paired by seed x world.
# Paired two-sided Wilcoxon signed-rank (normal approx., tie + continuity correction, zeros dropped), Holm within each mode.
# 95% percentile bootstrap CI of the mean paired difference in gain (resampling worlds, fixed RNG seed 12345).
$ErrorActionPreference = 'Stop'
$rows = Get-ChildItem $InDir -Recurse -Filter stageb_raw.csv | ForEach-Object { Import-Csv $_.FullName }
$inv = [Globalization.CultureInfo]::InvariantCulture
function D($x) { [double]::Parse($x, $inv) }
function F($x, $f='0.0') { if ($x -eq $null -or [double]::IsNaN($x)) { 'n/a' } else { ([double]$x).ToString($f, $inv) } }
$sb = New-Object Text.StringBuilder
$seeds = ($rows | Select-Object -ExpandProperty seed -Unique | Sort-Object { [int]$_ })
[void]$sb.AppendLine("Input rows: $($rows.Count); seeds: $($seeds -join ','); worlds: $(($rows | Select-Object -ExpandProperty held_out -Unique).Count)")
[void]$sb.AppendLine("")

function Wilcoxon([double[]]$d) {
  $nz = @($d | Where-Object { [math]::Abs($_) -gt 1e-12 })
  $n = $nz.Count
  if ($n -eq 0) { return @{ W = 0; p = 1.0; n = 0 } }
  $abs = $nz | ForEach-Object { [math]::Abs($_) } | Sort-Object
  $ranks = @{}; $tieAdj = 0.0
  $i = 0
  while ($i -lt $n) {
    $j = $i; while ($j + 1 -lt $n -and [math]::Abs($abs[$j+1] - $abs[$i]) -lt 1e-12) { $j++ }
    $r = ($i + $j) / 2.0 + 1; $t = $j - $i + 1; $tieAdj += ($t*$t*$t - $t)
    for ($k = $i; $k -le $j; $k++) { $ranks["$($abs[$k])"] = $r }
    $i = $j + 1
  }
  $wp = 0.0; foreach ($x in $nz) { if ($x -gt 0) { $wp += $ranks["$([math]::Abs($x))"] } }
  $mu = $n * ($n + 1) / 4.0
  $sd = [math]::Sqrt($n * ($n + 1) * (2*$n + 1) / 24.0 - $tieAdj / 48.0)
  if ($sd -le 0) { return @{ W = $wp; p = 1.0; n = $n } }
  $z = ([math]::Abs($wp - $mu) - 0.5) / $sd; if ($z -lt 0) { $z = 0 }
  # two-sided normal tail via erfc approximation (Abramowitz-Stegun 7.1.26)
  $x = $z / [math]::Sqrt(2); $t = 1.0 / (1.0 + 0.3275911 * $x)
  $erfc = $t * (0.254829592 + $t * (-0.284496736 + $t * (1.421413741 + $t * (-1.453152027 + $t * 1.061405429)))) * [math]::Exp(-$x*$x)
  return @{ W = $wp; p = [math]::Min(1.0, $erfc); n = $n }
}
function BootCI([double[]]$d, [int]$B) {
  $rng = New-Object Random 12345; $n = $d.Count; $m = New-Object double[] $B
  for ($bi = 0; $bi -lt $B; $bi++) { $s = 0.0; for ($i = 0; $i -lt $n; $i++) { $s += $d[$rng.Next($n)] }; $m[$bi] = $s / $n }
  [Array]::Sort($m); return @($m[[int][math]::Floor(0.025*$B)], $m[[int][math]::Floor(0.975*$B) - 1])
}

foreach ($mode in @('gk','dh')) {
  $mr = @($rows | Where-Object { $_.mode -eq $mode }); if ($mr.Count -eq 0) { continue }
  $empty = @{}; foreach ($r in ($mr | Where-Object { $_.policy -eq 'EmptyLTM' })) { $empty["$($r.seed)|$($r.held_out)"] = D $r.energy_score }
  $worlds = @($mr | Select-Object -ExpandProperty held_out -Unique)
  $policies = @($mr | Select-Object -ExpandProperty policy -Unique)
  # per-world mean gain per policy
  $pw = @{}; $gr = @{}
  foreach ($p in $policies) {
    $pw[$p] = @{}; $gs = @()
    foreach ($w in $worlds) {
      $g = @($mr | Where-Object { $_.policy -eq $p -and $_.held_out -eq $w } | ForEach-Object { $e = $empty["$($_.seed)|$w"]; 100.0 * ($e - (D $_.energy_score)) / $e })
      if ($g.Count -gt 0) { $pw[$p][$w] = ($g | Measure-Object -Average).Average }
    }
    $gr[$p] = (@($mr | Where-Object { $_.policy -eq $p } | ForEach-Object { D $_.goal_rate }) | Measure-Object -Average).Average
  }
  $label = if ($mode -eq 'gk') { 'Goal-known (GK)' } else { 'Destination-hidden (DH)' }
  [void]$sb.AppendLine("### ${label}: gain vs EmptyLTM (mean over $($worlds.Count) held-out worlds; +/- = SD across per-world means)")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("| Policy | Gain % | SD | min world | max world | goal rate |")
  [void]$sb.AppendLine("|---|---:|---:|---:|---:|---:|")
  $order = $policies | Sort-Object { -(($pw[$_].Values | Measure-Object -Average).Average) }
  foreach ($p in $order) {
    $v = @($pw[$p].Values); $st = $v | Measure-Object -Average -Minimum -Maximum
    $sd = if ($v.Count -gt 1) { [math]::Sqrt((($v | ForEach-Object { ($_ - $st.Average) * ($_ - $st.Average) }) | Measure-Object -Sum).Sum / ($v.Count - 1)) } else { 0 }
    [void]$sb.AppendLine("| $p | $(F $st.Average) | $(F $sd) | $(F $st.Minimum) | $(F $st.Maximum) | $(F $gr[$p] '0.000') |")
  }
  [void]$sb.AppendLine("")
  # comparisons
  $cmp = @()
  $swRefs = @('SuccessWeighted'); if ($policies -contains 'SW_ProgDist') { $swRefs += 'SW_ProgDist' }
  foreach ($ref in $swRefs) {
    foreach ($p in $policies) { if ($p -like 'SW*' -or $p -eq 'EmptyLTM' -or $p -eq 'SuccessWeighted') { continue }; $cmp += ,@($ref, $p) }
  }
  $cmp += ,@('SuccessWeighted','SW_NoSuccessWeight')
  if ($policies -contains 'SW_ProgDist') { $cmp += ,@('SW_ProgDist','SW_NoSuccessWeight_ProgDist'); $cmp += ,@('SW_ProgDist','SuccessWeighted'); $cmp += ,@('SuccessWeighted','SW_NoGoalCtx') }
  foreach ($a in @('TabularNgramBC','KnnFeel','EpisodicControl','TabularQ')) {
    if ($policies -contains ($a + '_GC')) { $cmp += ,@(($a + '_GC'), ($a + '_Tuned')) }
    $cmp += ,@(($a + '_Tuned'), $a)
  }
  $res = @()
  foreach ($c in $cmp) {
    $a = $c[0]; $b = $c[1]; if (-not $pw.ContainsKey($a) -or -not $pw.ContainsKey($b)) { continue }
    $d = [double[]]@($worlds | Where-Object { $pw[$a].ContainsKey($_) -and $pw[$b].ContainsKey($_) } | ForEach-Object { $pw[$a][$_] - $pw[$b][$_] })
    $wx = Wilcoxon $d; $ci = BootCI $d $Boot
    $res += [pscustomobject]@{ A = $a; B = $b; n = $d.Count; Mean = ($d | Measure-Object -Average).Average; Lo = $ci[0]; Hi = $ci[1]; Wins = @($d | Where-Object { $_ -gt 0 }).Count; W = $wx.W; p = $wx.p; pHolm = 1.0 }
  }
  # Holm within mode
  $sorted = @($res | Sort-Object p); $m = $sorted.Count; $run = 0.0
  for ($i = 0; $i -lt $m; $i++) { $adj = [math]::Min(1.0, ($m - $i) * $sorted[$i].p); if ($adj -lt $run) { $adj = $run }; $run = $adj; $sorted[$i].pHolm = $adj }
  [void]$sb.AppendLine("Paired comparisons (${label}). Diff = gain(A) - gain(B) in percentage points, per world; Holm over the $m comparisons in this mode.")
  [void]$sb.AppendLine("")
  [void]$sb.AppendLine("| A | B | n | mean diff (pp) | 95% boot CI | A better in | W+ | p (Wilcoxon) | p (Holm) |")
  [void]$sb.AppendLine("|---|---|---:|---:|---|---:|---:|---:|---:|")
  foreach ($r in $res) { [void]$sb.AppendLine("| $($r.A) | $($r.B) | $($r.n) | $(F $r.Mean) | [$(F $r.Lo), $(F $r.Hi)] | $($r.Wins)/$($r.n) | $(F $r.W '0.0') | $(F $r.p '0.0000') | $(F $r.pHolm '0.0000') |") }
  [void]$sb.AppendLine("")
}
# tuning picks summary
$tune = Get-ChildItem $InDir -Recurse -Filter stageb_tuning.csv | ForEach-Object { Import-Csv $_.FullName } | Where-Object { $_.chosen -eq '1' }
[void]$sb.AppendLine("### Tuning picks (count of folds choosing each config; chosen on inner validation worlds only)")
[void]$sb.AppendLine("")
foreach ($g in ($tune | Group-Object mode, arm)) {
  $top = ($g.Group | Group-Object cfg | Sort-Object Count -Descending | Select-Object -First 3 | ForEach-Object { "$($_.Name) x$($_.Count)" }) -join '; '
  [void]$sb.AppendLine("- $($g.Name): $top")
}
Set-Content -Path $OutMd -Value $sb.ToString() -Encoding UTF8
Write-Output $sb.ToString()



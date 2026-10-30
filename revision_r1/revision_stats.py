"""Paired stats for revision_raw.csv: gain %, Wilcoxon (SW vs each), Holm, 95% bootstrap CI."""
import sys, os, csv, random, statistics as st
from collections import defaultdict
d = sys.argv[1]
rows = list(csv.DictReader(open(os.path.join(d, "revision_raw.csv"))))
E = defaultdict(dict)
for r in rows: E[(r["seed"], r["held_out"])][r["policy"]] = float(r["energy_score"])
gain = defaultdict(dict)
for k, v in E.items():
    if "EmptyLTM" not in v or v["EmptyLTM"] == 0: continue
    for p, x in v.items(): gain[p][k] = 100 * (v["EmptyLTM"] - x) / v["EmptyLTM"]
def boot(xs, B=10000, rng=random.Random(1)):
    m = sorted(st.mean(rng.choices(xs, k=len(xs))) for _ in range(B))
    return m[int(.025 * B)], m[int(.975 * B)]
try:
    from scipy.stats import wilcoxon
except ImportError:
    wilcoxon = None
ref = "SuccessWeighted"; out = []; tests = []
for p, g in sorted(gain.items()):
    xs = list(g.values()); lo, hi = boot(xs)
    row = dict(policy=p, n=len(xs), mean_gain=st.mean(xs), ci_lo=lo, ci_hi=hi, p_raw="", p_holm="")
    if p != ref and ref in gain and wilcoxon:
        ks = [k for k in g if k in gain[ref]]
        diff = [gain[ref][k] - g[k] for k in ks]
        row["mean_diff_SW_minus"] = st.mean(diff)
        row["p_raw"] = wilcoxon(diff).pvalue if any(diff) else 1.0
        tests.append(row)
    out.append(row)
tests.sort(key=lambda r: r["p_raw"]); m = len(tests); prev = 0
for i, r in enumerate(tests):
    prev = max(prev, min(1, (m - i) * r["p_raw"])); r["p_holm"] = prev
cols = ["policy", "n", "mean_gain", "ci_lo", "ci_hi", "mean_diff_SW_minus", "p_raw", "p_holm"]
with open(os.path.join(d, "revision_stats.csv"), "w", newline="") as f:
    w = csv.DictWriter(f, cols, extrasaction="ignore"); w.writeheader(); w.writerows(out)
md = ["| policy | n | gain % | 95% CI | SW-minus | p Holm |", "|---|---|---|---|---|---|"]
for r in out:
    md.append(f"| {r['policy']} | {r['n']} | {r['mean_gain']:.1f} | [{r['ci_lo']:.1f}, {r['ci_hi']:.1f}] | "
              f"{r.get('mean_diff_SW_minus', float('nan')):.1f} | {r['p_holm'] if r['p_holm']=='' else format(r['p_holm'],'.3g')} |")
open(os.path.join(d, "revision_stats.md"), "w").write("\n".join(md) + "\n"); print("\n".join(md))

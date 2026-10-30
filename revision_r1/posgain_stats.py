"""Pos-gain stats: per-world mean gain over seeds; paired exact Wilcoxon (two-sided), Holm over the pre-specified family; bootstrap 95% CI (10000, worlds resampled)."""
import csv, glob, sys, random, math, os
from collections import defaultdict
tag = sys.argv[1]
root = os.path.join('EvalLogs', 'posgain', tag)
rows = []
for f in glob.glob(os.path.join(root, '*', 'posgain_raw.csv')):
    meta = open(os.path.join(os.path.dirname(f), 'run_meta.txt')).read()
    if 'finished=' not in meta: continue
    rows += list(csv.DictReader(open(f)))
E = {}; P = defaultdict(dict); hf = defaultdict(list)
for r in rows:
    key = (r['exp'], r['cond'], r['seed'], r['held_out'])
    if r['policy'] == 'EmptyLTM': E[key] = float(r['energy_score'])
    else: P[key][r['policy']] = float(r['energy_score']); hf[(r['cond'], r['policy'])].append(float(r['hyb_frac']))
gain = defaultdict(lambda: defaultdict(list))  # (exp,cond,policy) -> world -> [gains]
for key, d in P.items():
    e = E.get(key)
    if not e: continue
    for pol, v in d.items(): gain[(key[0], key[1], pol)][key[3]].append(100 * (e - v) / e)
def wm(exp, cond, pol): return {w: sum(g)/len(g) for w, g in gain[(exp, cond, pol)].items()}
def wilcoxon(d):
    d = [x for x in d if abs(x) > 1e-12]; n = len(d)
    if n == 0: return 1.0
    a = sorted(range(n), key=lambda i: abs(d[i])); ranks = [0]*n; i = 0
    while i < n:
        j = i
        while j+1 < n and abs(abs(d[a[j+1]]) - abs(d[a[i]])) < 1e-12: j += 1
        for k in range(i, j+1): ranks[a[k]] = (i + j + 2) / 2
        i = j + 1
    r2 = [int(round(2*r)) for r in ranks]; tot = sum(r2)
    wp = sum(r2[i] for i in range(n) if d[i] > 0)
    dist = {0: 1}
    for r in r2:
        nd = defaultdict(int)
        for s, c in dist.items(): nd[s] += c; nd[s+r] += c
        dist = nd
    allc = 2**n; lo = min(wp, tot - wp)
    p = 2 * sum(c for s, c in dist.items() if s <= lo) / allc
    return min(1.0, p)
def boot(d, B=10000):
    rnd = random.Random(12345); n = len(d); m = []
    for _ in range(B): m.append(sum(d[rnd.randrange(n)] for _ in range(n)) / n)
    m.sort(); return m[int(0.025*B)], m[int(0.975*B)-1]
comps = []
for a, b in [('Hyb_kNN_GC_tau','KnnFeel_GC'),('Hyb_BC_GC_tau','TabularNgramBC_GC'),('Hyb_kNN_GC_miss','KnnFeel_GC'),('Hyb_BC_GC_miss','TabularNgramBC_GC')]: comps.append(('E1','base',a,b))
for a, b in [('HybDH_kNN_miss','KnnFeel'),('HybDH_BC_miss','TabularNgramBC')]: comps.append(('E1','dh',a,b))
for k in ('k2','k5','k10'):
    for a, b in [('SuccessWeighted','KnnFeel_GC'),('SuccessWeighted','TabularNgramBC_GC'),('Hyb_kNN_GC_miss','KnnFeel_GC'),('Hyb_BC_GC_miss','TabularNgramBC_GC')]: comps.append(('E2',k,a,b))
for c in ('noise25','noise50','drop25','drop50','shift'):
    for a, b in [('SuccessWeighted','KnnFeel_GC'),('SuccessWeighted','TabularNgramBC_GC'),('Hyb_kNN_GC_tau','KnnFeel_GC'),('Hyb_BC_GC_tau','TabularNgramBC_GC')]: comps.append(('E3',c,a,b))
res = []
for exp, cond, a, b in comps:
    A, Bm = wm(exp, cond, a), wm(exp, cond, b); ws = sorted(set(A) & set(Bm))
    if not ws: continue
    d = [A[w] - Bm[w] for w in ws]
    res.append([exp, cond, a, b, len(ws), sum(d)/len(d), boot(d), sum(1 for x in d if x > 0), wilcoxon(d)])
order = sorted(range(len(res)), key=lambda i: res[i][8]); m = len(res); run = 0
for rank, i in enumerate(order):
    run = max(run, min(1.0, (m - rank) * res[i][8])); res[i].append(run)
out = []
out.append('# Pos-gain stats (%s): %d raw rows, %d pre-specified comparisons (Holm family)\n' % (tag, len(rows), m))
out.append('## Mean gain %% over worlds (gain vs EmptyLTM of the same condition)\n')
conds = sorted(set((k[0], k[1]) for k in gain))
pols = ['SuccessWeighted','SW_NoSuccessWeight','TabularNgramBC_GC','KnnFeel_GC','Hyb_BC_GC_tau','Hyb_kNN_GC_tau','Hyb_BC_GC_miss','Hyb_kNN_GC_miss','TabularNgramBC','KnnFeel','HybDH_BC_miss','HybDH_kNN_miss']
out.append('| exp | cond | ' + ' | '.join(pols) + ' |'); out.append('|' + '---|'*(len(pols)+2))
for exp, cond in conds:
    cells = []
    for p in pols:
        g = wm(exp, cond, p); cells.append('%.1f' % (sum(g.values())/len(g)) if g else '')
    out.append('| %s | %s | %s |' % (exp, cond, ' | '.join(cells)))
out.append('\n## Hybrid: fraction of decision steps handed to SW\n')
for (c, p), v in sorted(hf.items()):
    if 'Hyb' in p: out.append('- %s %s: %.3f' % (c, p, sum(v)/len(v)))
out.append('\n## Paired comparisons (A - B, pp of gain; + means A better)\n')
out.append('| exp | cond | A | B | n worlds | mean diff | 95% CI | A wins | p | Holm p |'); out.append('|---|---|---|---|---:|---:|---|---:|---:|---:|')
for r in res: out.append('| %s | %s | %s | %s | %d | %+.2f | [%.2f, %.2f] | %d/%d | %.4f | %.4f |' % (r[0], r[1], r[2], r[3], r[4], r[5], r[6][0], r[6][1], r[7], r[4], r[8], r[9]))
open(os.path.join(root, 'stats_%s.md' % tag), 'w').write('\n'.join(out) + '\n'); print('\n'.join(out))

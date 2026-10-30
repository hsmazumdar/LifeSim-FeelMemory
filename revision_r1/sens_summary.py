import csv,glob,collections,statistics as st,sys
rows=[r for f in glob.glob(r"E:\PaperSubmissions2026\EvolutionLife\Revision_R1\EvalLogs\sens\*\sens_raw.csv") for r in csv.DictReader(open(f))]
emp={(r['seed'],r['held_out']):float(r['energy_score']) for r in rows if r['policy']=='EmptyLTM'}
g=collections.defaultdict(lambda:collections.defaultdict(list)); keys=collections.defaultdict(list)
for r in rows:
    if r['sweep']=='ref':continue
    k=(r['seed'],r['held_out']); e=emp[k]
    g[(r['sweep'],r['policy'],r['value'])][r['held_out']].append(100*(e-float(r['energy_score']))/e)
    keys[(r['sweep'],r['policy'],r['value'])].append(float(r['keys']))
out=["| sweep | value | policy | gain % (mean over worlds) | SD over worlds | n | mean keys |","|---|---|---|---:|---:|---:|---:|"]
def num(v):
    try:return float(v)
    except:return 1e9
for k in sorted(g,key=lambda k:(k[0],k[1],num(k[2]))):
    w=[st.mean(v) for v in g[k].values()]; n=sum(len(v) for v in g[k].values())
    out.append(f"| {k[0]} | {k[2]} | {k[1]} | {st.mean(w):.1f} | {st.pstdev(w):.1f} | {n} | {st.mean(keys[k]):.0f} |")
print("\n".join(out))

import re,io
p=r'code\EvolutionApp\Robot.cs'
s=open(p,encoding='utf-8-sig').read()
def rep(a,b):
    global s
    assert s.count(a)==1,(a,s.count(a)); s=s.replace(a,b)
rep("public IFeelPolicy FeelPolicy { get; set; }","public IFeelPolicy FeelPolicy { get; set; }\n        /// <summary>R1 pos-gain: hybrid = primary FeelPolicy, SW brain branch when primary misses or conf below HybridTau.</summary>\n        public bool HybridBrain { get; set; } = false;\n        public double HybridTau { get; set; } = 0.0;\n        public int HybridBrainSteps;\n        /// <summary>R1 pos-gain robustness: corrupt goal-direction signal given to memories (not the shared heuristic).</summary>\n        public double GoalNoiseP { get; set; } = 0.0;\n        public double GoalDropP { get; set; } = 0.0;\n        public Random NoiseRng;")
rep("""            IFeelPolicy bcPolicy = TabularBC != null ? (IFeelPolicy)TabularBC : FeelPolicy;""","""            int ctxDir = -1; bool ctxDrop = false;
            if (!HideDestination)
            {
                ctxDir = BrainMemory.ComputeGoalDirection(Cell, world.GoalCell, Facing);
                if (NoiseRng != null)
                {
                    if (GoalDropP > 0 && NoiseRng.NextDouble() < GoalDropP) ctxDrop = true;
                    else if (GoalNoiseP > 0 && NoiseRng.NextDouble() < GoalNoiseP) ctxDir = NoiseRng.Next(8);
                }
            }
            IFeelPolicy bcPolicy = TabularBC != null ? (IFeelPolicy)TabularBC : FeelPolicy;""")
rep("else gcp.SetContext(BrainMemory.ComputeGoalDirection(Cell, world.GoalCell, Facing), BrainMemory","else if (ctxDrop) gcp.SetContext(-1, -1);\n                    else gcp.SetContext(ctxDir, BrainMemory")
rep("""            double[] weights = new double[8];""","""            double bcConf = 1.0; var confP = bcPolicy as IConfPolicy;
            if (bcHit && confP != null) bcConf = confP.LastConf;
            bool hybHere = bcActive && HybridBrain && UseBrain && sense != null && (!bcHit || bcConf < HybridTau);
            if (hybHere) HybridBrainSteps++;
            double[] weights = new double[8];""")
rep("                if (bcActive)\n","                if (bcActive && !hybHere)\n")
rep("int gNear = (HideDestination || MaskBrainGoalContext) ? -1","int gNear = (HideDestination || MaskBrainGoalContext || ctxDrop) ? -1")
rep("int gDir = (HideDestination || MaskBrainGoalContext) ? -1 : CurrentGoalDirection(world);","int gDir = (HideDestination || MaskBrainGoalContext || ctxDrop) ? -1 : (NoiseRng != null ? ctxDir : CurrentGoalDirection(world));")
open(p,'w',encoding='utf-8-sig').write(s)
p=r'code\EvolutionApp\GoalCondBaselines.cs'
s=open(p,encoding='utf-8-sig').read()
rep("public interface IGoalCtxPolicy { void SetContext(int goalDir, int progBin); }","public interface IGoalCtxPolicy { void SetContext(int goalDir, int progBin); }\n    /// <summary>R1 pos-gain: vote share of the chosen action on a goal-conditioned hit (0 when the GC part missed).</summary>\n    public interface IConfPolicy { double LastConf { get; } }")
rep("public sealed class GcTabularBC : IFeelPolicy, IGoalCtxPolicy\n    {","public sealed class GcTabularBC : IFeelPolicy, IGoalCtxPolicy, IConfPolicy\n    {\n        public double LastConf { get; private set; }")
rep("public sealed class GcKnn : IFeelPolicy, IGoalCtxPolicy\n    {","public sealed class GcKnn : IFeelPolicy, IGoalCtxPolicy, IConfPolicy\n    {\n        public double LastConf { get; private set; }")
rep("if (r[best] > 0) { rel = best; support = r[best]; mk = k; return true; }","if (r[best] > 0) { int tot = 0; for (int a = 0; a < 8; a++) tot += r[a]; LastConf = (double)r[best] / tot; rel = best; support = r[best]; mk = k; return true; }")
rep('if (score[best] > 0) { rel = best; mk = "gknn"; return true; }','if (score[best] > 0) { double tot = 0; for (int a = 0; a < 8; a++) tot += score[a]; LastConf = score[best] / tot; rel = best; mk = "gknn"; return true; }')
# reset LastConf at start of GC TryLookups (both BC and kNN start with this line followed by GC checks)
s=s.replace("            rel = -1; support = 0; mk = null;\n            if (_gd >= 0 && h != null)\n","            rel = -1; support = 0; mk = null; LastConf = 0;\n            if (_gd >= 0 && h != null)\n",1)
rep("            rel = -1; support = 0; mk = null;\n            var q = _gd >= 0 ? FeelUtil.Tail(h, L) : null;\n            if (q != null && _keys.Count > 0)\n            {\n                var cand","            rel = -1; support = 0; mk = null; LastConf = 0;\n            var q = _gd >= 0 ? FeelUtil.Tail(h, L) : null;\n            if (q != null && _keys.Count > 0)\n            {\n                var cand")
open(p,'w',encoding='utf-8-sig').write(s)
print('ok', s.count('LastConf = 0'))

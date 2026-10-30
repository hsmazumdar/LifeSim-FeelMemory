import re,sys,io
root=r"F:\PaperSubmissions2026\EvolutionLife\Revision_R1\code\EvolutionApp\\"
def rw(fn,pairs):
    p=root+fn; s=io.open(p,encoding='utf-8-sig').read()
    for a,b in pairs:
        assert s.count(a)==1,(fn,a,s.count(a)); s=s.replace(a,b)
    io.open(p,'w',encoding='utf-8-sig').write(s)
rw("Robot.cs",[
 ("public TabularNgramBC TabularBC { get; set; }","public TabularNgramBC TabularBC { get; set; }\n        /// <summary>R1: any IFeelPolicy (kNN / episodic control / tabular Q) through the same BC action wrapper. Null = unused.</summary>\n        public IFeelPolicy FeelPolicy { get; set; }"),
 ("bool bcActive = TabularBC != null;","IFeelPolicy bcPolicy = TabularBC != null ? (IFeelPolicy)TabularBC : FeelPolicy;\n            bool bcActive = bcPolicy != null;"),
 ("if (TabularBC.TryLookup(_bcFeelHist,","if (bcPolicy.TryLookup(_bcFeelHist,"),
])
rw("TabularNgramBC.cs",[
 ("public sealed class TabularNgramBC\n","public sealed class TabularNgramBC : IFeelPolicy\n"),
 ("public int KeyCount { get { return _votes.Count; } }","public int KeyCount { get { return _votes.Count; } }\n        public string Name { get { return MechanismName; } }"),
])
rw("Program.cs",[
 ('string.Equals(args[i], "--eval-baseline", StringComparison.OrdinalIgnoreCase) ||','string.Equals(args[i], "--eval-baseline", StringComparison.OrdinalIgnoreCase) ||\n                        string.Equals(args[i], "--eval-revision", StringComparison.OrdinalIgnoreCase) ||'),
 ("return EvalHarness.Run(args);","foreach (var ra in args) if (string.Equals(ra, \"--eval-revision\", StringComparison.OrdinalIgnoreCase)) return RevisionHarness.Run(args);\n                        return EvalHarness.Run(args);"),
])
p=root+"EvolutionLife.csproj"; s=io.open(p,encoding='utf-8-sig').read()
a='<Compile Include="TabularNgramBC.cs" />'
assert a in s, "csproj"
s=s.replace(a,a+'\n    <Compile Include="RevisionBaselines.cs" />\n    <Compile Include="RevisionHarness.cs" />')
io.open(p,'w',encoding='utf-8-sig').write(s)
print("patched")

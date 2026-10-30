Input rows: 5800; seeds: 101,202,303,404,505,606,707,808,909,1010; worlds: 20

### Goal-known (GK): gain vs EmptyLTM (mean over 20 held-out worlds; +/- = SD across per-world means)

| Policy | Gain % | SD | min world | max world | goal rate |
|---|---:|---:|---:|---:|---:|
| KnnFeel_GC | 89.6 | 3.1 | 85.4 | 96.7 | 1.000 |
| TabularNgramBC_GC | 89.1 | 3.7 | 83.6 | 96.6 | 1.000 |
| TabularQ_GC | 80.6 | 5.3 | 74.2 | 94.1 | 1.000 |
| EpisodicControl_GC | 80.4 | 5.9 | 72.3 | 93.8 | 1.000 |
| SW_ProgDist | 76.1 | 6.6 | 66.7 | 91.4 | 1.000 |
| SW_NoSuccessWeight_ProgDist | 75.4 | 6.9 | 65.6 | 91.1 | 1.000 |
| SuccessWeighted | 75.1 | 6.9 | 65.5 | 91.6 | 1.000 |
| SW_NoSuccessWeight | 74.2 | 7.3 | 62.8 | 91.4 | 1.000 |
| KnnFeel_Tuned | 25.9 | 20.5 | -4.0 | 72.5 | 1.000 |
| TabularNgramBC_Tuned | 24.3 | 20.7 | -4.1 | 71.3 | 1.000 |
| KnnFeel | 20.4 | 21.7 | -10.1 | 70.6 | 1.000 |
| TabularNgramBC | 19.6 | 22.3 | -6.8 | 71.7 | 1.000 |
| SW_NoGoalCtx | 2.9 | 25.7 | -31.3 | 60.4 | 1.000 |
| EmptyLTM | 0.0 | 0.0 | 0.0 | 0.0 | 0.990 |
| EpisodicControl_Tuned | -4.4 | 20.8 | -34.4 | 45.5 | 0.999 |
| EpisodicControl | -5.9 | 19.1 | -35.1 | 39.0 | 0.998 |
| TabularQ_Tuned | -15.9 | 31.9 | -57.3 | 50.8 | 0.999 |
| TabularQ | -26.7 | 36.9 | -72.1 | 48.8 | 0.999 |

Paired comparisons (Goal-known (GK)). Diff = gain(A) - gain(B) in percentage points, per world; Holm over the 36 comparisons in this mode.

| A | B | n | mean diff (pp) | 95% boot CI | A better in | W+ | p (Wilcoxon) | p (Holm) |
|---|---|---:|---:|---|---:|---:|---:|---:|
| SuccessWeighted | TabularNgramBC | 20 | 55.5 | [48.3, 61.8] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SuccessWeighted | KnnFeel | 20 | 54.6 | [47.9, 60.7] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SuccessWeighted | EpisodicControl | 20 | 80.9 | [75.2, 86.1] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SuccessWeighted | TabularQ | 20 | 101.7 | [88.3, 114.5] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SuccessWeighted | TabularNgramBC_Tuned | 20 | 50.8 | [44.3, 56.6] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SuccessWeighted | KnnFeel_Tuned | 20 | 49.2 | [43.1, 54.9] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SuccessWeighted | EpisodicControl_Tuned | 20 | 79.4 | [72.5, 85.5] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SuccessWeighted | TabularQ_Tuned | 20 | 91.0 | [79.4, 101.8] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SuccessWeighted | TabularNgramBC_GC | 20 | -14.1 | [-15.6, -12.3] | 0/20 | 0.0 | 0.0001 | 0.0034 |
| SuccessWeighted | KnnFeel_GC | 20 | -14.6 | [-16.2, -12.8] | 0/20 | 0.0 | 0.0001 | 0.0034 |
| SuccessWeighted | EpisodicControl_GC | 20 | -5.3 | [-6.1, -4.5] | 0/20 | 0.0 | 0.0001 | 0.0034 |
| SuccessWeighted | TabularQ_GC | 20 | -5.5 | [-6.7, -4.3] | 0/20 | 0.0 | 0.0001 | 0.0034 |
| SW_ProgDist | TabularNgramBC | 20 | 56.5 | [49.1, 62.9] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SW_ProgDist | KnnFeel | 20 | 55.6 | [48.7, 61.8] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SW_ProgDist | EpisodicControl | 20 | 81.9 | [76.1, 87.3] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SW_ProgDist | TabularQ | 20 | 102.7 | [89.2, 115.8] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SW_ProgDist | TabularNgramBC_Tuned | 20 | 51.7 | [45.2, 57.7] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SW_ProgDist | KnnFeel_Tuned | 20 | 50.2 | [43.9, 56.1] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SW_ProgDist | EpisodicControl_Tuned | 20 | 80.4 | [73.3, 86.8] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SW_ProgDist | TabularQ_Tuned | 20 | 92.0 | [80.3, 102.9] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| SW_ProgDist | TabularNgramBC_GC | 20 | -13.1 | [-14.5, -11.5] | 0/20 | 0.0 | 0.0001 | 0.0034 |
| SW_ProgDist | KnnFeel_GC | 20 | -13.6 | [-15.1, -11.9] | 0/20 | 0.0 | 0.0001 | 0.0034 |
| SW_ProgDist | EpisodicControl_GC | 20 | -4.4 | [-5.1, -3.6] | 0/20 | 0.0 | 0.0001 | 0.0034 |
| SW_ProgDist | TabularQ_GC | 20 | -4.5 | [-5.5, -3.5] | 1/20 | 1.0 | 0.0001 | 0.0034 |
| SuccessWeighted | SW_NoSuccessWeight | 20 | 0.9 | [0.5, 1.3] | 16/20 | 187.0 | 0.0023 | 0.0070 |
| SW_ProgDist | SW_NoSuccessWeight_ProgDist | 20 | 0.7 | [0.4, 0.9] | 18/20 | 201.0 | 0.0004 | 0.0034 |
| SW_ProgDist | SuccessWeighted | 20 | 1.0 | [0.6, 1.4] | 17/20 | 192.0 | 0.0012 | 0.0054 |
| SuccessWeighted | SW_NoGoalCtx | 20 | 72.2 | [63.7, 80.0] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| TabularNgramBC_GC | TabularNgramBC_Tuned | 20 | 64.8 | [56.9, 71.9] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| TabularNgramBC_Tuned | TabularNgramBC | 20 | 4.7 | [2.3, 7.1] | 17/20 | 185.0 | 0.0030 | 0.0070 |
| KnnFeel_GC | KnnFeel_Tuned | 20 | 63.8 | [56.0, 71.1] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| KnnFeel_Tuned | KnnFeel | 20 | 5.4 | [2.9, 7.7] | 17/20 | 193.0 | 0.0011 | 0.0054 |
| EpisodicControl_GC | EpisodicControl_Tuned | 20 | 84.8 | [77.4, 91.3] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| EpisodicControl_Tuned | EpisodicControl | 20 | 1.5 | [-2.3, 5.1] | 11/20 | 127.0 | 0.4222 | 0.4222 |
| TabularQ_GC | TabularQ_Tuned | 20 | 96.5 | [84.3, 107.9] | 20/20 | 210.0 | 0.0001 | 0.0034 |
| TabularQ_Tuned | TabularQ | 20 | 10.8 | [7.3, 14.7] | 20/20 | 210.0 | 0.0001 | 0.0034 |

### Destination-hidden (DH): gain vs EmptyLTM (mean over 20 held-out worlds; +/- = SD across per-world means)

| Policy | Gain % | SD | min world | max world | goal rate |
|---|---:|---:|---:|---:|---:|
| KnnFeel_Tuned | 62.4 | 13.8 | 45.9 | 88.7 | 1.000 |
| TabularNgramBC_Tuned | 61.2 | 13.6 | 43.8 | 88.8 | 0.999 |
| TabularNgramBC | 50.6 | 17.6 | 29.4 | 85.6 | 0.998 |
| KnnFeel | 50.2 | 17.6 | 31.3 | 85.7 | 0.998 |
| EpisodicControl_Tuned | 32.8 | 16.4 | 5.8 | 56.3 | 0.966 |
| EpisodicControl | 29.0 | 17.9 | 1.3 | 60.6 | 0.970 |
| SuccessWeighted | 16.6 | 28.9 | -21.3 | 71.9 | 0.967 |
| SW_NoSuccessWeight | 15.9 | 28.3 | -18.7 | 72.2 | 0.968 |
| EmptyLTM | 0.0 | 0.0 | 0.0 | 0.0 | 0.865 |
| TabularQ_Tuned | -10.5 | 35.6 | -53.5 | 65.6 | 0.919 |
| TabularQ | -13.2 | 38.8 | -60.3 | 64.1 | 0.912 |

Paired comparisons (Destination-hidden (DH)). Diff = gain(A) - gain(B) in percentage points, per world; Holm over the 13 comparisons in this mode.

| A | B | n | mean diff (pp) | 95% boot CI | A better in | W+ | p (Wilcoxon) | p (Holm) |
|---|---|---:|---:|---|---:|---:|---:|---:|
| SuccessWeighted | TabularNgramBC | 20 | -34.0 | [-39.1, -28.7] | 0/20 | 0.0 | 0.0001 | 0.0012 |
| SuccessWeighted | KnnFeel | 20 | -33.5 | [-38.4, -28.2] | 0/20 | 0.0 | 0.0001 | 0.0012 |
| SuccessWeighted | EpisodicControl | 20 | -12.3 | [-18.5, -5.9] | 4/20 | 25.0 | 0.0030 | 0.0120 |
| SuccessWeighted | TabularQ | 20 | 29.9 | [23.7, 36.9] | 20/20 | 210.0 | 0.0001 | 0.0012 |
| SuccessWeighted | TabularNgramBC_Tuned | 20 | -44.5 | [-51.3, -37.4] | 0/20 | 0.0 | 0.0001 | 0.0012 |
| SuccessWeighted | KnnFeel_Tuned | 20 | -45.7 | [-52.2, -38.5] | 0/20 | 0.0 | 0.0001 | 0.0012 |
| SuccessWeighted | EpisodicControl_Tuned | 20 | -16.1 | [-24.0, -7.8] | 4/20 | 23.0 | 0.0023 | 0.0117 |
| SuccessWeighted | TabularQ_Tuned | 20 | 27.1 | [21.5, 33.6] | 20/20 | 210.0 | 0.0001 | 0.0012 |
| SuccessWeighted | SW_NoSuccessWeight | 20 | 0.8 | [-1.6, 3.0] | 12/20 | 128.0 | 0.4009 | 0.4500 |
| TabularNgramBC_Tuned | TabularNgramBC | 20 | 10.6 | [8.0, 13.2] | 20/20 | 210.0 | 0.0001 | 0.0012 |
| KnnFeel_Tuned | KnnFeel | 20 | 12.2 | [10.0, 14.3] | 20/20 | 210.0 | 0.0001 | 0.0012 |
| EpisodicControl_Tuned | EpisodicControl | 20 | 3.8 | [1.2, 6.5] | 15/20 | 173.0 | 0.0117 | 0.0352 |
| TabularQ_Tuned | TabularQ | 20 | 2.8 | [-0.9, 6.7] | 11/20 | 138.0 | 0.2250 | 0.4500 |

### Tuning picks (count of folds choosing each config; chosen on inner validation worlds only)

- gk, TabularNgramBC: boost=8;K=5;Wg=0.5 x113; boost=4;K=5;Wg=0.5 x58; boost=2;K=5;Wg=0.5 x29
- gk, KnnFeel: boost=8;K=3;Wg=0.5 x129; boost=4;K=3;Wg=0.5 x50; boost=2;K=3;Wg=0.5 x19
- gk, EpisodicControl: boost=2;K=5;Wg=0.5 x33; boost=2;K=9;Wg=0.5 x30; boost=2;K=3;Wg=0.5 x29
- gk, TabularQ: boost=2;K=5;Wg=0.5 x137; boost=4;K=5;Wg=0.5 x55; boost=8;K=5;Wg=0.5 x8
- gk, TabularNgramBC_GC: boost=8;K=5;Wg=0.5 x194; boost=4;K=5;Wg=0.5 x6
- gk, KnnFeel_GC: boost=8;K=3;Wg=1 x166; boost=8;K=5;Wg=1 x23; boost=8;K=9;Wg=1 x11
- gk, EpisodicControl_GC: boost=8;K=9;Wg=1 x75; boost=8;K=5;Wg=1 x55; boost=8;K=3;Wg=1 x40
- gk, TabularQ_GC: boost=8;K=5;Wg=0.5 x191; boost=4;K=5;Wg=0.5 x9
- dh, TabularNgramBC: boost=8;K=5;Wg=0.5 x178; boost=4;K=5;Wg=0.5 x20; boost=2;K=5;Wg=0.5 x2
- dh, KnnFeel: boost=8;K=3;Wg=0.5 x174; boost=4;K=3;Wg=0.5 x15; boost=8;K=9;Wg=0.5 x8
- dh, EpisodicControl: boost=8;K=3;Wg=0.5 x40; boost=8;K=5;Wg=0.5 x40; boost=8;K=9;Wg=0.5 x35
- dh, TabularQ: boost=2;K=5;Wg=0.5 x112; boost=4;K=5;Wg=0.5 x61; boost=8;K=5;Wg=0.5 x27


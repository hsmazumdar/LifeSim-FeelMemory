# Pos-gain stats (pilot): 924 raw rows, 38 pre-specified comparisons (Holm family)

## Mean gain %% over worlds (gain vs EmptyLTM of the same condition)

| exp | cond | SuccessWeighted | SW_NoSuccessWeight | TabularNgramBC_GC | KnnFeel_GC | Hyb_BC_GC_tau | Hyb_kNN_GC_tau | Hyb_BC_GC_miss | Hyb_kNN_GC_miss | TabularNgramBC | KnnFeel | HybDH_BC_miss | HybDH_kNN_miss |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| E1 | base | 72.4 | 70.2 | 87.5 | 88.2 | 88.5 | 88.1 | 88.6 | 88.2 |  |  |  |  |
| E1 | dh | 5.9 |  |  |  |  |  |  |  | 62.2 | 62.1 | 62.3 | 63.8 |
| E2 | k10 | 71.6 | 71.2 | 86.9 | 88.1 |  |  | 88.4 | 88.0 |  |  |  |  |
| E2 | k2 | 72.0 | 72.8 | 81.0 | 87.6 |  |  | 86.3 | 87.5 |  |  |  |  |
| E2 | k5 | 69.4 | 69.0 | 85.9 | 87.8 |  |  | 87.6 | 87.9 |  |  |  |  |
| E3 | drop25 | 65.8 |  | 86.7 | 87.5 | 86.2 | 85.8 | 86.3 | 86.2 |  |  |  |  |
| E3 | drop50 | 54.2 |  | 85.2 | 85.1 | 81.3 | 81.0 | 81.4 | 81.7 |  |  |  |  |
| E3 | noise25 | 64.0 |  | 84.1 | 84.7 | 85.2 | 84.4 | 85.2 | 84.6 |  |  |  |  |
| E3 | noise50 | 55.1 |  | 78.2 | 79.0 | 78.9 | 79.2 | 78.7 | 78.9 |  |  |  |  |
| E3 | shift | 75.6 | 75.4 | 89.1 | 89.9 | 90.2 | 90.0 | 90.2 | 90.0 |  |  |  |  |

## Hybrid: fraction of decision steps handed to SW

- base Hyb_BC_GC_miss: 0.127
- base Hyb_BC_GC_tau: 0.141
- base Hyb_kNN_GC_miss: 0.159
- base Hyb_kNN_GC_tau: 0.171
- dh HybDH_BC_miss: 0.009
- dh HybDH_kNN_miss: 0.018
- drop25 Hyb_BC_GC_miss: 0.345
- drop25 Hyb_BC_GC_tau: 0.358
- drop25 Hyb_kNN_GC_miss: 0.339
- drop25 Hyb_kNN_GC_tau: 0.354
- drop50 Hyb_BC_GC_miss: 0.569
- drop50 Hyb_BC_GC_tau: 0.579
- drop50 Hyb_kNN_GC_miss: 0.538
- drop50 Hyb_kNN_GC_tau: 0.557
- k10 Hyb_BC_GC_miss: 0.148
- k10 Hyb_kNN_GC_miss: 0.156
- k2 Hyb_BC_GC_miss: 0.323
- k2 Hyb_kNN_GC_miss: 0.148
- k5 Hyb_BC_GC_miss: 0.189
- k5 Hyb_kNN_GC_miss: 0.154
- noise25 Hyb_BC_GC_miss: 0.173
- noise25 Hyb_BC_GC_tau: 0.191
- noise25 Hyb_kNN_GC_miss: 0.121
- noise25 Hyb_kNN_GC_tau: 0.157
- noise50 Hyb_BC_GC_miss: 0.184
- noise50 Hyb_BC_GC_tau: 0.209
- noise50 Hyb_kNN_GC_miss: 0.086
- noise50 Hyb_kNN_GC_tau: 0.139
- shift Hyb_BC_GC_miss: 0.131
- shift Hyb_BC_GC_tau: 0.144
- shift Hyb_kNN_GC_miss: 0.158
- shift Hyb_kNN_GC_tau: 0.169

## Paired comparisons (A - B, pp of gain; + means A better)

| exp | cond | A | B | n worlds | mean diff | 95% CI | A wins | p | Holm p |
|---|---|---|---|---:|---:|---|---:|---:|---:|
| E1 | base | Hyb_kNN_GC_tau | KnnFeel_GC | 4 | -0.07 | [-0.18, 0.04] | 1/4 | 0.3750 | 1.0000 |
| E1 | base | Hyb_BC_GC_tau | TabularNgramBC_GC | 4 | +1.01 | [0.57, 1.44] | 4/4 | 0.1250 | 1.0000 |
| E1 | base | Hyb_kNN_GC_miss | KnnFeel_GC | 4 | -0.03 | [-0.09, 0.04] | 1/4 | 0.6250 | 1.0000 |
| E1 | base | Hyb_BC_GC_miss | TabularNgramBC_GC | 4 | +1.12 | [0.65, 1.59] | 4/4 | 0.1250 | 1.0000 |
| E1 | dh | HybDH_kNN_miss | KnnFeel | 4 | +1.71 | [-0.11, 4.11] | 3/4 | 0.2500 | 1.0000 |
| E1 | dh | HybDH_BC_miss | TabularNgramBC | 4 | +0.09 | [-0.54, 0.80] | 2/4 | 0.7500 | 1.0000 |
| E2 | k2 | SuccessWeighted | KnnFeel_GC | 4 | -15.57 | [-16.93, -14.14] | 0/4 | 0.1250 | 1.0000 |
| E2 | k2 | SuccessWeighted | TabularNgramBC_GC | 4 | -8.97 | [-11.57, -6.36] | 0/4 | 0.1250 | 1.0000 |
| E2 | k2 | Hyb_kNN_GC_miss | KnnFeel_GC | 4 | -0.07 | [-0.29, 0.16] | 2/4 | 0.8750 | 1.0000 |
| E2 | k2 | Hyb_BC_GC_miss | TabularNgramBC_GC | 4 | +5.26 | [3.50, 7.18] | 4/4 | 0.1250 | 1.0000 |
| E2 | k5 | SuccessWeighted | KnnFeel_GC | 4 | -18.47 | [-22.81, -14.12] | 0/4 | 0.1250 | 1.0000 |
| E2 | k5 | SuccessWeighted | TabularNgramBC_GC | 4 | -16.52 | [-20.95, -12.09] | 0/4 | 0.1250 | 1.0000 |
| E2 | k5 | Hyb_kNN_GC_miss | KnnFeel_GC | 4 | +0.07 | [-0.06, 0.19] | 2/4 | 0.6250 | 1.0000 |
| E2 | k5 | Hyb_BC_GC_miss | TabularNgramBC_GC | 4 | +1.68 | [1.08, 2.35] | 4/4 | 0.1250 | 1.0000 |
| E2 | k10 | SuccessWeighted | KnnFeel_GC | 4 | -16.46 | [-17.77, -14.84] | 0/4 | 0.1250 | 1.0000 |
| E2 | k10 | SuccessWeighted | TabularNgramBC_GC | 4 | -15.26 | [-16.14, -14.51] | 0/4 | 0.1250 | 1.0000 |
| E2 | k10 | Hyb_kNN_GC_miss | KnnFeel_GC | 4 | -0.11 | [-0.18, -0.01] | 1/4 | 0.2500 | 1.0000 |
| E2 | k10 | Hyb_BC_GC_miss | TabularNgramBC_GC | 4 | +1.47 | [0.80, 1.95] | 4/4 | 0.1250 | 1.0000 |
| E3 | noise25 | SuccessWeighted | KnnFeel_GC | 4 | -20.64 | [-23.19, -18.09] | 0/4 | 0.1250 | 1.0000 |
| E3 | noise25 | SuccessWeighted | TabularNgramBC_GC | 4 | -20.12 | [-23.17, -17.27] | 0/4 | 0.1250 | 1.0000 |
| E3 | noise25 | Hyb_kNN_GC_tau | KnnFeel_GC | 4 | -0.28 | [-0.39, -0.17] | 0/4 | 0.1250 | 1.0000 |
| E3 | noise25 | Hyb_BC_GC_tau | TabularNgramBC_GC | 4 | +1.04 | [0.52, 1.51] | 4/4 | 0.1250 | 1.0000 |
| E3 | noise50 | SuccessWeighted | KnnFeel_GC | 4 | -23.91 | [-27.19, -20.62] | 0/4 | 0.1250 | 1.0000 |
| E3 | noise50 | SuccessWeighted | TabularNgramBC_GC | 4 | -23.16 | [-24.83, -21.21] | 0/4 | 0.1250 | 1.0000 |
| E3 | noise50 | Hyb_kNN_GC_tau | KnnFeel_GC | 4 | +0.20 | [-0.75, 1.11] | 2/4 | 0.8750 | 1.0000 |
| E3 | noise50 | Hyb_BC_GC_tau | TabularNgramBC_GC | 4 | +0.64 | [-0.03, 1.32] | 3/4 | 0.3750 | 1.0000 |
| E3 | drop25 | SuccessWeighted | KnnFeel_GC | 4 | -21.69 | [-24.63, -19.33] | 0/4 | 0.1250 | 1.0000 |
| E3 | drop25 | SuccessWeighted | TabularNgramBC_GC | 4 | -20.92 | [-24.25, -18.44] | 0/4 | 0.1250 | 1.0000 |
| E3 | drop25 | Hyb_kNN_GC_tau | KnnFeel_GC | 4 | -1.71 | [-2.42, -0.99] | 0/4 | 0.1250 | 1.0000 |
| E3 | drop25 | Hyb_BC_GC_tau | TabularNgramBC_GC | 4 | -0.55 | [-1.15, 0.21] | 1/4 | 0.3750 | 1.0000 |
| E3 | drop50 | SuccessWeighted | KnnFeel_GC | 4 | -30.84 | [-34.50, -26.83] | 0/4 | 0.1250 | 1.0000 |
| E3 | drop50 | SuccessWeighted | TabularNgramBC_GC | 4 | -30.99 | [-34.93, -27.05] | 0/4 | 0.1250 | 1.0000 |
| E3 | drop50 | Hyb_kNN_GC_tau | KnnFeel_GC | 4 | -4.05 | [-5.32, -2.78] | 0/4 | 0.1250 | 1.0000 |
| E3 | drop50 | Hyb_BC_GC_tau | TabularNgramBC_GC | 4 | -3.91 | [-4.99, -2.94] | 0/4 | 0.1250 | 1.0000 |
| E3 | shift | SuccessWeighted | KnnFeel_GC | 4 | -14.31 | [-18.85, -9.78] | 0/4 | 0.1250 | 1.0000 |
| E3 | shift | SuccessWeighted | TabularNgramBC_GC | 4 | -13.42 | [-17.47, -9.38] | 0/4 | 0.1250 | 1.0000 |
| E3 | shift | Hyb_kNN_GC_tau | KnnFeel_GC | 4 | +0.00 | [-0.24, 0.16] | 3/4 | 0.8750 | 1.0000 |
| E3 | shift | Hyb_BC_GC_tau | TabularNgramBC_GC | 4 | +1.11 | [0.52, 1.85] | 4/4 | 0.1250 | 1.0000 |

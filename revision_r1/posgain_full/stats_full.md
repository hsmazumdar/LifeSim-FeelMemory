# Pos-gain stats (full): 15400 raw rows, 38 pre-specified comparisons (Holm family)

## Mean gain %% over worlds (gain vs EmptyLTM of the same condition)

| exp | cond | SuccessWeighted | SW_NoSuccessWeight | TabularNgramBC_GC | KnnFeel_GC | Hyb_BC_GC_tau | Hyb_kNN_GC_tau | Hyb_BC_GC_miss | Hyb_kNN_GC_miss | TabularNgramBC | KnnFeel | HybDH_BC_miss | HybDH_kNN_miss |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| E1 | base | 75.1 | 74.2 | 89.2 | 89.7 | 90.1 | 89.6 | 90.1 | 89.7 |  |  |  |  |
| E1 | dh | 16.6 |  |  |  |  |  |  |  | 63.2 | 63.3 | 63.2 | 63.6 |
| E2 | k10 | 71.9 | 71.2 | 88.7 | 89.7 |  |  | 89.8 | 89.7 |  |  |  |  |
| E2 | k2 | 73.5 | 73.0 | 84.5 | 89.2 |  |  | 88.1 | 89.3 |  |  |  |  |
| E2 | k5 | 72.5 | 71.4 | 87.5 | 89.6 |  |  | 89.3 | 89.6 |  |  |  |  |
| E3 | drop25 | 68.4 |  | 88.3 | 88.8 | 87.8 | 87.3 | 87.8 | 87.5 |  |  |  |  |
| E3 | drop50 | 58.1 |  | 86.2 | 86.6 | 83.3 | 82.7 | 83.4 | 83.0 |  |  |  |  |
| E3 | noise25 | 68.9 |  | 85.7 | 86.6 | 86.9 | 86.5 | 87.0 | 86.6 |  |  |  |  |
| E3 | noise50 | 58.1 |  | 79.5 | 80.7 | 80.8 | 80.4 | 80.9 | 80.8 |  |  |  |  |
| E3 | shift | 74.6 | 74.1 | 88.9 | 89.5 | 89.7 | 89.4 | 89.7 | 89.5 |  |  |  |  |

## Hybrid: fraction of decision steps handed to SW

- base Hyb_BC_GC_miss: 0.127
- base Hyb_BC_GC_tau: 0.139
- base Hyb_kNN_GC_miss: 0.159
- base Hyb_kNN_GC_tau: 0.172
- dh HybDH_BC_miss: 0.008
- dh HybDH_kNN_miss: 0.017
- drop25 Hyb_BC_GC_miss: 0.354
- drop25 Hyb_BC_GC_tau: 0.364
- drop25 Hyb_kNN_GC_miss: 0.351
- drop25 Hyb_kNN_GC_tau: 0.367
- drop50 Hyb_BC_GC_miss: 0.577
- drop50 Hyb_BC_GC_tau: 0.585
- drop50 Hyb_kNN_GC_miss: 0.551
- drop50 Hyb_kNN_GC_tau: 0.568
- k10 Hyb_BC_GC_miss: 0.148
- k10 Hyb_kNN_GC_miss: 0.158
- k2 Hyb_BC_GC_miss: 0.303
- k2 Hyb_kNN_GC_miss: 0.153
- k5 Hyb_BC_GC_miss: 0.190
- k5 Hyb_kNN_GC_miss: 0.157
- noise25 Hyb_BC_GC_miss: 0.166
- noise25 Hyb_BC_GC_tau: 0.181
- noise25 Hyb_kNN_GC_miss: 0.122
- noise25 Hyb_kNN_GC_tau: 0.156
- noise50 Hyb_BC_GC_miss: 0.184
- noise50 Hyb_BC_GC_tau: 0.201
- noise50 Hyb_kNN_GC_miss: 0.085
- noise50 Hyb_kNN_GC_tau: 0.133
- shift Hyb_BC_GC_miss: 0.134
- shift Hyb_BC_GC_tau: 0.144
- shift Hyb_kNN_GC_miss: 0.159
- shift Hyb_kNN_GC_tau: 0.171

## Paired comparisons (A - B, pp of gain; + means A better)

| exp | cond | A | B | n worlds | mean diff | 95% CI | A wins | p | Holm p |
|---|---|---|---|---:|---:|---|---:|---:|---:|
| E1 | base | Hyb_kNN_GC_tau | KnnFeel_GC | 20 | -0.07 | [-0.12, -0.02] | 6/20 | 0.0192 | 0.1539 |
| E1 | base | Hyb_BC_GC_tau | TabularNgramBC_GC | 20 | +0.85 | [0.61, 1.11] | 20/20 | 0.0000 | 0.0001 |
| E1 | base | Hyb_kNN_GC_miss | KnnFeel_GC | 20 | +0.04 | [-0.00, 0.08] | 14/20 | 0.0897 | 0.5382 |
| E1 | base | Hyb_BC_GC_miss | TabularNgramBC_GC | 20 | +0.90 | [0.65, 1.16] | 20/20 | 0.0000 | 0.0001 |
| E1 | dh | HybDH_kNN_miss | KnnFeel | 20 | +0.31 | [-0.41, 0.99] | 13/20 | 0.3300 | 0.9899 |
| E1 | dh | HybDH_BC_miss | TabularNgramBC | 20 | +0.01 | [-0.13, 0.16] | 12/20 | 0.4307 | 0.9899 |
| E2 | k2 | SuccessWeighted | KnnFeel_GC | 20 | -15.67 | [-17.36, -13.87] | 0/20 | 0.0000 | 0.0001 |
| E2 | k2 | SuccessWeighted | TabularNgramBC_GC | 20 | -10.96 | [-12.59, -9.35] | 0/20 | 0.0000 | 0.0001 |
| E2 | k2 | Hyb_kNN_GC_miss | KnnFeel_GC | 20 | +0.11 | [0.04, 0.18] | 15/20 | 0.0107 | 0.0962 |
| E2 | k2 | Hyb_BC_GC_miss | TabularNgramBC_GC | 20 | +3.64 | [2.97, 4.30] | 20/20 | 0.0000 | 0.0001 |
| E2 | k5 | SuccessWeighted | KnnFeel_GC | 20 | -17.08 | [-19.28, -14.85] | 0/20 | 0.0000 | 0.0001 |
| E2 | k5 | SuccessWeighted | TabularNgramBC_GC | 20 | -15.04 | [-17.09, -13.06] | 0/20 | 0.0000 | 0.0001 |
| E2 | k5 | Hyb_kNN_GC_miss | KnnFeel_GC | 20 | +0.03 | [-0.01, 0.06] | 14/20 | 0.1429 | 0.7145 |
| E2 | k5 | Hyb_BC_GC_miss | TabularNgramBC_GC | 20 | +1.74 | [1.39, 2.10] | 20/20 | 0.0000 | 0.0001 |
| E2 | k10 | SuccessWeighted | KnnFeel_GC | 20 | -17.72 | [-20.03, -15.38] | 0/20 | 0.0000 | 0.0001 |
| E2 | k10 | SuccessWeighted | TabularNgramBC_GC | 20 | -16.74 | [-18.96, -14.52] | 0/20 | 0.0000 | 0.0001 |
| E2 | k10 | Hyb_kNN_GC_miss | KnnFeel_GC | 20 | +0.01 | [-0.04, 0.06] | 12/20 | 0.3488 | 0.9899 |
| E2 | k10 | Hyb_BC_GC_miss | TabularNgramBC_GC | 20 | +1.15 | [0.87, 1.44] | 20/20 | 0.0000 | 0.0001 |
| E3 | noise25 | SuccessWeighted | KnnFeel_GC | 20 | -17.66 | [-19.73, -15.46] | 0/20 | 0.0000 | 0.0001 |
| E3 | noise25 | SuccessWeighted | TabularNgramBC_GC | 20 | -16.78 | [-18.68, -14.79] | 0/20 | 0.0000 | 0.0001 |
| E3 | noise25 | Hyb_kNN_GC_tau | KnnFeel_GC | 20 | -0.07 | [-0.17, 0.03] | 7/20 | 0.2024 | 0.8098 |
| E3 | noise25 | Hyb_BC_GC_tau | TabularNgramBC_GC | 20 | +1.20 | [0.85, 1.56] | 20/20 | 0.0000 | 0.0001 |
| E3 | noise50 | SuccessWeighted | KnnFeel_GC | 20 | -22.63 | [-25.37, -19.75] | 0/20 | 0.0000 | 0.0001 |
| E3 | noise50 | SuccessWeighted | TabularNgramBC_GC | 20 | -21.43 | [-23.84, -18.92] | 0/20 | 0.0000 | 0.0001 |
| E3 | noise50 | Hyb_kNN_GC_tau | KnnFeel_GC | 20 | -0.30 | [-0.46, -0.13] | 6/20 | 0.0032 | 0.0315 |
| E3 | noise50 | Hyb_BC_GC_tau | TabularNgramBC_GC | 20 | +1.25 | [0.86, 1.63] | 19/20 | 0.0000 | 0.0001 |
| E3 | drop25 | SuccessWeighted | KnnFeel_GC | 20 | -20.33 | [-22.64, -17.82] | 0/20 | 0.0000 | 0.0001 |
| E3 | drop25 | SuccessWeighted | TabularNgramBC_GC | 20 | -19.88 | [-22.10, -17.49] | 0/20 | 0.0000 | 0.0001 |
| E3 | drop25 | Hyb_kNN_GC_tau | KnnFeel_GC | 20 | -1.46 | [-1.66, -1.27] | 0/20 | 0.0000 | 0.0001 |
| E3 | drop25 | Hyb_BC_GC_tau | TabularNgramBC_GC | 20 | -0.58 | [-0.75, -0.42] | 1/20 | 0.0000 | 0.0001 |
| E3 | drop50 | SuccessWeighted | KnnFeel_GC | 20 | -28.49 | [-31.74, -24.96] | 0/20 | 0.0000 | 0.0001 |
| E3 | drop50 | SuccessWeighted | TabularNgramBC_GC | 20 | -28.13 | [-31.35, -24.72] | 0/20 | 0.0000 | 0.0001 |
| E3 | drop50 | Hyb_kNN_GC_tau | KnnFeel_GC | 20 | -3.91 | [-4.50, -3.32] | 0/20 | 0.0000 | 0.0001 |
| E3 | drop50 | Hyb_BC_GC_tau | TabularNgramBC_GC | 20 | -2.99 | [-3.46, -2.53] | 0/20 | 0.0000 | 0.0001 |
| E3 | shift | SuccessWeighted | KnnFeel_GC | 20 | -14.89 | [-16.68, -13.07] | 0/20 | 0.0000 | 0.0001 |
| E3 | shift | SuccessWeighted | TabularNgramBC_GC | 20 | -14.27 | [-16.01, -12.49] | 0/20 | 0.0000 | 0.0001 |
| E3 | shift | Hyb_kNN_GC_tau | KnnFeel_GC | 20 | -0.06 | [-0.11, -0.02] | 5/20 | 0.0240 | 0.1677 |
| E3 | shift | Hyb_BC_GC_tau | TabularNgramBC_GC | 20 | +0.80 | [0.63, 0.99] | 20/20 | 0.0000 | 0.0001 |

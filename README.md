# CleanImp Benchmark

## 1. Introduction

**CleanImp** is an end-to-end benchmark for evaluating the impact of time series imputation on downstream tasks. The technical details are
described in the paper **CleanImp: Benchmarking the Impact of Time Series Imputation on Downstream Quality [Experiment, Analysis & Benchmark]** (under review for PVLDB 27).

The benchmark follows the complete experimental pipeline introduced in the CleanImp paper:

``` text
Time Series → Contamination → Imputation → Downstream Model → Evaluation
```


All experimental results are available through this interactive benchmark explorer: https://exascaleinfolab.github.io/CleanImp/

<br>

------------------------------------------------------------------------

[Introduction](#1-introduction) · [Prerequisites](#2-prerequisites) · [Configurations](#3-configurations) · [Parameters and Options](#4-parameters-and-options) · [Imputation Experiments](#5-imputation-experiments) · [Forecasting Experiments](#6-forecasting-experiments) · [Classification Experiments](#7-classification-experiments)

------------------------------------------------------------------------


## 2. Prerequisites

CleanImp is implemented in **Python** and relies on **ImputeGAP** for time series contamination and imputation. Please start by cloning the GitHub repository:

``` bash
git clone https://github.com/eXascaleInfolab/CleanImp
cd CleanImp/
```

To set up your environment and prepare the development installation for C++, please run this script of installation:

``` bash
source cleanimp_install.sh
cd framework/
```

------------------------------------------------------------------------

## 3. Configurations

This file provides a compact overview of the options available when configuring a CleanImp benchmark.

## Imputation Options

| Imputation Algorithms |               |            |            |                |
|-----------------------|---------------|------------|------------|----------------|
| `MOMENT`              | `NuwaTS`      | `GPT4TS`   | `MissNet`  | `MPIN`         |
| `BayOTIDE`            | `BitGraph`    | `TimesNet` | `SAITS`    | `PriSTI`       |
| `GRIN`                | `CSDI`        | `HKMFT`    | `DeepMVI`  | `MRNN`         |
| `BRITS`               | `GAIN`        | `CDRec`    | `TRMF`     | `GROUSE`       |
| `ROSL`                | `SoftImpute`  | `SVT`      | `SPIRIT`   | `IterativeSVD` |
| `TKCM`                | `STMVL`       | `DynaMMo`  | `IIM`      | `XGBoost`      |
| `MICE`                | `MissForest`  |            |            |                |

## Forecasting Options

| Forecasting Models |                  |              |                |           |
|--------------------|------------------|--------------|----------------|-----------|
| `Chronos`          | `MOMENT`         | `PatchTST`   | `Transformer`  | `LTSF`    |
| `LSTM`             | `DeepAR`         | `DLinear`    | `N-BEATS`      | `NLinear` |
| `Holt-Winters`     | `Exp. Smoothing` | `AutoARIMA`  | `Croston`      | `XGBoost` |
| `LightGBM`         | `Prophet`        |              |                |           |


| Forecasting Datasets |                   |            |              |                |
|----------------------|-------------------|------------|--------------|----------------|
| `czelan`             | `electricity`     | `etth1`    |  `etth2`     | `ili`          |
| `nn5`                | `nyse`            | `wike2000` | `wind_speed` | `airq`         |
| `atm`                | `beijing_traffic` | `climate`  | `economics`  | `human_access` |
| `paris`              |                   |            |              |                |
The complete list of available forecasting datasets can be found in the following directory: https://github.com/eXascaleInfolab/CleanImp/tree/main/framework/datasets/forecast

## Classification Options

| Classification Models |             |          |            |             |
|-----------------------|-------------|----------|------------|-------------|
| `KNN`                 | `Catch22`   | `CNN`    | `LSTM`     | `TSF`       |
| `CBOSS`               | `STC`       | `WEASEL` | `ShapeDTW` | `TSFresh`   |
| `ProxStump`           | `CIF`       | `ITDE`   | `Arsenal`  | `Signature` |
| `SVC`                 |             |          |            |             |

| Classification Datasets    |                                  |                                 |                                |                               |
|----------------------------|----------------------------------|---------------------------------|--------------------------------|-------------------------------|
| `Adiac`                    | `BirdChicken`                    | `Fish`                          | `InsectEPGRegularTrain`        | `Rock`                        |
| `OSULeaf`                  | `SwedishLeaf`                    | `Worms`                         | `WormsTwoClass`                | `Herring`                     |
| `Ham`                      | `EthanolLevel`                   | `Beef`                          | `Meat`                         | `OliveOil`                    |
| `Strawberry`               | `Wine`                           | `Car`                           | `FaceFour`                     | `FacesUCR`                    |
| `ArrowHead`                | `CinCECGTorso`                   | `Colposcopy`                    | `DistalPhalanxOutlineAgeGroup` | `DistalPhalanxOutlineCorrect` |
| `DistalPhalanxTW`          | `ECGFiveDays`                    | `EOGHorizontalSignal`           | `EOGVerticalSignal`            | `MedicalImages`               |
| `PhalangesOutlinesCorrect` | `ProximalPhalanxOutlineAgeGroup` | `ProximalPhalanxOutlineCorrect` | `SemgHandGenderCh2`            | `SemgHandMovementCh2`         |
| `SemgHandSubjectCh2`       | `TwoLeadECG`                     | `MoteStrain`                    | `Lightning2`                   | `Lightning7`                  |
| `Earthquakes`              | `Computers`                      | `LargeKitchenAppliances`        | `FreezerRegularTrain`          | `FreezerSmallTrain`           |
| `PowerCons`                | `RefrigerationDevices`           | `ScreenType`                    | `SmallKitchenAppliances`       | `SonyAIBORobotSurface1`       |
| `GunPoint`                 | `GunPointAgeSpan`                | `GunPointMaleVersusFemale`      | `GunPointOldVersusYoung`       | `CricketX`                    |
| `Haptics`                  | `InlineSkate`                    | `ToeSegmentation1`              | `ToeSegmentation2`             | `Yoga`                        |
| `SharePriceIncrease`       | `CBF`                            | `SyntheticControl`              | `Trace`                        | `ShapeletSim`                 |
| `TwoPatterns`              | `UMD`                            | `Wafer`                         |                                |                               |
The complete list of available classification datasets can be found in the following directory: https://github.com/eXascaleInfolab/CleanImp/tree/main/framework/datasets/classify


## Output directory

Each benchmark execution creates a **unique experiment directory**. The directory tree below directly describes the purpose of each generated  folder and file:

``` text
cleanimp/
│
├── _caching/                                  # Cache used to avoid recomputing expensive pipelines
│   └── ...                                    # Imputed matrices and downstream predictions
│
├── imputegap_assets/
│   └── benchmark/
│       └── <experiment>/                      # Unique directory created for the benchmark run
│           │
│           ├── <dataset>/                     # Results grouped by evaluated dataset
│           │   └── <pattern>/                 # Results grouped by missingness pattern
│           │       └── error/                 # Upstream/downstream metric results
│           │       │   ├── _metrics_subplot.jpg
│           │       │   │                      # Metric evolution across missingness rates
│           │       │   └── report_<pattern>_<dataset>.txt
│           │       │                           # Detailed results for this dataset/
│           │       └── recovery/               # Individal imputation plot for each rate
│           │
│           ├── _heatmaps/                     # Aggregated results used for heatmap analyses
│           │   └── _benchmarking_*.txt        # Benchmark results represented as heatmaps
│           │
│           ├── _summary_cleanimp_*.xlsx       # Complete summary of metrics and evaluated pipelines
│           ├── experimentation_setup.txt      # Exact configuration of the benchmark run
│           ├── report_cleanimp_benchmark_*.log# Complete benchmark results in log format
│           └── runtime.log                    # Runtime information for the experiment
│
└── *_log.txt                                  # General execution logs
```

------------------------------------------------------------------------






------------------------------------------------------------------------

## 4. Parameters and Options


### Parameters
The list of the runner’s parameters is the following:

| Parameters/Arguments | Imputation                                                                                      | Forecasting                                  | Classification                                     | Description                                                                                       |
|:---------------------|:------------------------------------------------------------------------------------------------|:---------------------------------------------|:---------------------------------------------------|:--------------------------------------------------------------------------------------------------|
| `--task`             | `imputation`                                                                                    | `forecasting`                                | `classification`                                   | Selects the benchmark task to execute.                                                            |
| `--imp_algs`         | [Imputation Algorithms](#imputation-options)                                                    | [Imputation Algorithms](#imputation-options) | [Imputation Algorithms](#imputation-options)       | Selects one or more imputation algorithms. Use `all` to include every available algorithm.        |
| `--datasets_type`    | `forecasting`, `classification`                                                                 | `forecasting`                                | `classification`                                   | Selects the dataset category used for the benchmark.                                              |
| `--datasets_list`    | [Forecasting Datasets](#forecasting-options) [Classification Datasets](#classification-options) | [Forecasting Datasets](#forecasting-options) | [Classification Datasets](#classification-options) | Selects one or more datasets for the selected task. Use `all` to include every available dataset. |
| `--patterns`         | `mcar`, `seqn`, `blks`                                                                          | `mcar`, `seqn`, `blks`                       | `mcar`, `seqn`, `blks`                             | Selects one or more missingness patterns. Use `all` to include every available pattern.           |
| `--miss_rate`        | `0.1` -> `0.8`                                                                                  | `0.1` -> `0.8`                               | `0.1` -> `0.8`                                     | Sets one or more missing-value rates. Use `all` to evaluate the predefined rates.                 |
| `--metrics`          | `RMSE`, `MAE`, `MI`, `CORRELATION`                                                              | `SMAPE`, `MSE`, `MAE`                        | `F1`, `ACCURACY`, `RECALL`                         | Selects the evaluation metrics for the chosen task. Use `all` to include every available metric.  |
| `--downstream_mod`   | —                                                                                               | [Forecasting Models](#forecasting-options)   | [Classification Models](#classification-options)   | Selects the downstream model used to measure the impact of imputation.                            |
| `--horizon`          | `12`                                                                                            | `12`                                         | —                                                  | Sets the forecasting horizon as the number of future timestamps.                                  |
| `--caching`          | `True`, `False`                                                                                 | `True`, `False`                              | `True`, `False`                                    | Enables or disables caching of intermediate results to avoid repeated computations.               |
| `--plots`            | `True`, `False`                                                                                 | `True`, `False`                              | `True`, `False`                                    | Enables or disables benchmark plot generation.                                                    |
| `--verbose`          | `True`, `False`                                                                                 | `True`, `False`                              | `True`, `False`                                    | Enables or disables detailed execution output.                                                    |

<i>With the **“caching”** tag, you can store the imputed matrix and, for downstream tasks, the classification or prediction results. This allows you to rerun the benchmark without having to recompute pipelines that have already been executed.</i>

## 5. Imputation Experiments

CleanImp supports **32 imputation models** spanning Matrix Completion, Pattern Search, Machine Learning, Deep Learning, and LLM-based approaches.

### Experiments Examples

- To produce the imputation results with one forecasting dataset (Paris), one imputation algorithm (SAITS), one missingness pattern (MCAR), a missing rate (20%), one metric (RMSE), and a forecasting horizon (12 timestamps), run the following command:

``` bash
python cleanimp_bench.py \
    --task imputation \
    --imp_algs SAITS \
    --datasets_type forecasting \
    --datasets_list paris \
    --patterns mcar \
    --miss_rate 0.2 \
    --metrics RMSE \
    --horizon 12
```
After running the benchmark, the generated results can be found in: `./imputegap_assets/benchmark/[unique_bench_name]/`

<br />

-To produce the imputation results with two imputation algorithms (MICE and MeanImpute), two datasets (Paris and ILI), two missingness patterns (MCAR and SeqN), two missingness rates (0.1 and 0.8), and a forecasting horizon (24 timestamps), run the following command:

``` bash
python cleanimp_bench.py \
    --task imputation \
    --imp_algs MICE MeanImpute \
    --datasets_type forecasting \
    --datasets_list paris ili \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8 \
    --metrics all \
    --horizon 24
```
<br />

- To produce the imputation results with all possible configurations, replace the parameter values with `all`:
<br /><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>


``` bash
python cleanimp_bench.py \
    --task imputation \
    --imp_algs all \
    --datasets_type forecasting \
    --datasets_list all \
    --patterns all \
    --miss_rate all \
    --metrics all \
    --horizon 12
```
<br />

- To adapt the experiment for classification, change the task tag and replace the dataset list with classification datasets:

``` bash
python cleanimp_bench.py \
    --task imputation \
    --imp_algs GRIN \
    --datasets_type classification \
    --datasets_list Computers \
    --patterns mcar \
    --miss_rate 0.2 \
    --metrics all
```
<br />



## 6. Forecasting Experiments

CleanImp supports **17 forecasting models** spanning Statistical, Machine Learning, Deep Learning, and LLM-based approaches.

### Experiments Examples

- To produce the impact of imputation on a forecasting model (chronos) with
one forecasting dataset (Paris),
one imputation algorithm (SAITS),
one missingness pattern (MCAR),
one missing rate (20%),
all downstream metrics,
and a future prediction horizon (12 timestamps),
use the downstream script and run the following command:

``` bash
python cleanimp_bench.py \
    --task forecasting \
    --downstream_mod chronos \
    --imp_algs SAITS \
    --datasets_list paris \
    --patterns mcar \
    --miss_rate 0.2 \
    --metrics all \
    --horizon 12
```
<br />

- To produce the impact of imputation on a forecasting model (chronos) with
two imputation algorithms (MICE and MeanImpute),
two datasets (Paris and ILI),
two missingness patterns (MCAR and SeqN),
two missingness rates (0.1 and 0.8),
all downstream metrics,
and a future prediction horizon (24 timestamps),
run the following command:

``` bash
python cleanimp_bench.py \
    --task forecasting \
    --downstream_mod chronos \
    --imp_algs MICE MeanImpute \
    --datasets_list paris ili \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8 \
    --metrics all \
    --horizon 24
```
<br />

- To produce the impact of imputation on a forecasting model with all possible configurations,
replace the parameter values with `all`:
<br /><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>

``` bash
python cleanimp_bench.py \
    --task forecasting \
    --downstream_mod chronos \
    --imp_algs all \
    --datasets_list all \
    --patterns all \
    --miss_rate all \
    --metrics all \
    --horizon 12
```
<br />


------------------------------------------------------------------------

## 7. Classification Experiments

CleanImp supports **16 classification models** spanning Statistical, Machine Learning, and Deep Learning-based approaches.


### Experiments Examples

- To produce the impact of imputation on a classification model (arsenal) with
one classification dataset (Computers),
one imputation algorithm (GRIN),
one missingness pattern (MCAR),
one missing rate (20%),
all downstream metrics,
use the downstream script and run the following command:

``` bash
python cleanimp_bench.py \
    --task classification \
    --downstream_mod arsenal \
    --datasets_list Computers \
    --imp_algs GRIN \
    --patterns mcar \
    --miss_rate 0.2 \
    --metrics all
```
<br />

- To produce the impact of imputation on a classification model (arsenal) with
two classification dataset (Computers and Car),
two imputation algorithm (MeanImpute and MICE),
two missingness pattern (MCAR and SeqN),
two missing rate (20% and 80%),
all downstream metrics,
use the downstream script and run the following command:

``` bash
python cleanimp_bench.py \
    --task classification \
    --downstream_mod arsenal \
    --datasets_list Computers Car \
    --imp_algs MeanImpute MICE \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8 \
    --metrics all
```
<br />

- To produce the impact of imputation on a classification model with all possible configurations,
replace the parameter values with `all`:
<br /><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>

``` bash
python cleanimp_bench.py \
    --task classification \
    --downstream_mod arsenal \
    --datasets_list all  \
    --imp_algs all \
    --patterns all \
    --miss_rate all \
    --metrics all
```
<br />

------------------------------------------------------------------------



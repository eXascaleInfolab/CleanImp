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

[Configurations](#3-list-of-techniques-and-datasets) | [Parameters](#4-parameters-and-options) | [Imputation Experiments](#5-imputation-experiments) | [Forecasting Experiments](#6-forecasting-experiments) | [Classification Experiments](#7-classification-experiments)

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

## 3. List of Techniques and Datasets

The tables below provide a compact overview of the options available when configuring the benchmark.

## List of Techniques
| Imputation Algorithms |             |              |                |           |
|-----------------------|-------------|--------------|----------------|-----------|
| `BRITS`               | `BayOTIDE`  | `BitGraph`   | `CDRec`        | `CSDI`    |
| `DeepMVI`             | `DynaMMo`   | `GAIN`       | `GPT4TS`       | `GRIN`    |
| `GROUSE`              | `HKMFT`     | `IIM`        | `IterativeSVD` | `MICE`    |
| `MPIN`                | `MRNN`      | `MeanImpute` | `MissForest`   | `MissNet` |
| `Moment`              | `NuwaTS`    | `PRISTI`     | `ROSL`         | `SAITS`   |
| `SPIRIT`              | `STMVL`     | `SVT`        | `SoftImpute`   | `TKCM`    |
| `TRMF`                | `TimesNet`  | `XGBOOST`    |                |           |



| Forecasting Models |            |            |            |           |
|--------------------|------------|------------|------------|-----------|
| `arima`            | `chronos`  | `croston`  | `deepar`   | `dlinear` |
| `exp-smoothing`    | `hw-add`   | `lightgbm` | `lstm`     | `ltsf`    |
| `moment`           | `nbeats`   | `nlinear`  | `patchtst` | `prophet` |
| `transformer`      | `xgboost`  |            |            |           |



| Classification Models |            |           |             |            |
|-----------------------|------------|-----------|-------------|------------|
| `arsenal`             | `catch22`  | `cboss`   | `cif`       | `cnn`      |
| `itde`                | `knn`      | `lstm`    | `proxstump` | `shapedtw` |
| `signature`           | `stc`      | `svc`     | `tsf`       | `tsfresh`  |
| `weasel`              |            |           |             |            |


## List of Datasets


| Forecasting Datasets |               |                     |            |                |
|----------------------|---------------|---------------------|------------|----------------|
| `airq`               | `atm`         | `beijing_traffic`   | `climate`  | `czelan`       |
| `economics`          | `electricity` | `etth1`             | `etth2`    | `human_access` |
| `ili`                | `nn5`         | `nyse`              | `paris`    | `wike2000`     |
| `wind_speed`         |               |                     |            |                |


| Classification Datasets        |                               |                                  |                                 |                        |
|--------------------------------|-------------------------------|----------------------------------|---------------------------------|------------------------|
| `Adiac`                        | `ArrowHead`                   | `Beef`                           | `BirdChicken`                   | `CBF`                  |
| `Car`                          | `CinCECGTorso`                | `Colposcopy`                     | `Computers`                     | `CricketX`             |
| `DistalPhalanxOutlineAgeGroup` | `DistalPhalanxOutlineCorrect` | `DistalPhalanxTW`                | `ECGFiveDays`                   | `EOGHorizontalSignal`  |
| `EOGVerticalSignal`            | `Earthquakes`                 | `EthanolLevel`                   | `FaceFour`                      | `FacesUCR`             |
| `Fish`                         | `FreezerRegularTrain`         | `FreezerSmallTrain`              | `GunPoint`                      | `GunPointAgeSpan`      |
| `GunPointMaleVersusFemale`     | `GunPointOldVersusYoung`      | `Ham`                            | `Haptics`                       | `Herring`              |
| `InlineSkate`                  | `InsectEPGRegularTrain`       | `LargeKitchenAppliances`         | `Lightning2`                    | `Lightning7`           |
| `Meat`                         | `MedicalImages`               | `MoteStrain`                     | `OSULeaf`                       | `OliveOil`             |
| `PhalangesOutlinesCorrect`     | `PowerCons`                   | `ProximalPhalanxOutlineAgeGroup` | `ProximalPhalanxOutlineCorrect` | `RefrigerationDevices` |
| `Rock`                         | `ScreenType`                  | `SemgHandGenderCh2`              | `SemgHandMovementCh2`           | `SemgHandSubjectCh2`   |
| `ShapeletSim`                  | `SharePriceIncrease`          | `SmallKitchenAppliances`         | `SonyAIBORobotSurface1`         | `Strawberry`           |
| `SwedishLeaf`                  | `SyntheticControl`            | `ToeSegmentation1`               | `ToeSegmentation2`              | `Trace`                |
| `TwoLeadECG`                   | `TwoPatterns`                 | `UMD`                            | `Wafer`                         | `Wine`                 |
| `Worms`                        | `WormsTwoClass`               | `Yoga`                           |                                 |                        |


The datasets can be found in the following directory: https://github.com/eXascaleInfolab/CleanImp/tree/main/framework/datasets/


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


## 4. Parameters and Options


### Parameters
The list of the runner’s parameters is the following:

| Possible Arguments | Values                                                                          | Description                                                                                      |
|:-------------------|:--------------------------------------------------------------------------------|:-------------------------------------------------------------------------------------------------|
| `--task`           | `{imputation, forecasting, classification}`                                     | Selects the benchmark task to execute.                                                           |
| `--imp_algs`       | [List of Algorithms](#list-of-techniques)                                       | Selects one or more imputation algorithms. Use `all` to include every available algorithm.       |
| `--datasets_type`  | `{forecasting, classification}`                                                 | Selects the dataset category used for the benchmark.                                             |
| `--datasets_list`  | [List of Datasets](#list-of-datasets)                                           | Selects one or more datasets. Use `all` to include every available dataset.                      |
| `--patterns`       | `{mcar, seqn, blks}`                                                            | Selects one or more missingness patterns. Use `all` to include every available pattern.          |
| `--miss_rate`      | `{0.1, 0.2, 0.4, 0.6, 0.8}`                                                     | Sets one or more missing-value rates. Use `all` to evaluate the predefined rates.                |
| `--metrics`        | `{RMSE, MAE, MI, CORRELATION}` / `{SMAPE, MSE, MAE}` / `{F1, ACCURACY, RECALL}` | Selects the evaluation metrics for the chosen task. Use `all` to include every available metric. |
| `--downstream_mod` | [List of Models](#list-of-techniques)                                           | Selects the downstream model used to measure the impact of imputation.                           |
| `--horizon`        | `{12, 24, 48}`                                                                  | Sets the forecasting horizon as the number of future timestamps.                                 |
| `--caching`        | `{True, False}`                                                                 | Enables or disables caching of intermediate results to avoid repeated computations.              |
| `--plots`          | `{True, False}`                                                                 | Enables or disables benchmark plot generation.                                                   |
| `--verbose`        | `{True, False}`                                                                 | Enables or disables detailed execution output.                                                   |


<i>With the **“caching”** tag, you can store the imputed matrix and, for downstream tasks, the classification or prediction results. This allows you to rerun the benchmark without having to recompute pipelines that have already been executed.</i>

- **NOTE**: The computed results and the plots of the benchmark will be saved in: `./imputegap_assets/benchmark/*`


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

------------------------------------------------------------------------------------------------------------------------------------------------


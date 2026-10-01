# CleanImp Benchmark

## 1. Introduction

**CleanImp** is an end-to-end benchmark for evaluating the impact of time series imputation on downstream tasks. The technical details are
described in the paper **CleanImp: Benchmarking the Impact of Time Series Imputation on Downstream Quality [Experiment, Analysis & Benchmark]** (under review for PVLDB 27).

The benchmark follows the complete experimental pipeline introduced in the CleanImp paper:

``` text
Time Series → Contamination → Imputation → Downstream Model → Evaluation
```


All experimental results are available through this interactive benchmark explorer: https://exascaleinfolab.github.io/CleanImp/


------------------------------------------------------------------------

## 2. Datasets
The complete list of available forecasting and classification datasets can be found in the following directory: https://github.com/eXascaleInfolab/CleanImp/tree/main/framework/datasets


## 3. Prerequisites

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

## 4. Configurations

Here is the list of configurations for the benchmarks: you can find the list of all models and configurations on this page: https://github.com/eXascaleInfolab/CleanImp/tree/main/configurations

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

## 5. Imputation Experiments

CleanImp supports **32 imputation models** spanning Matrix Completion, Pattern Search, Machine Learning, Deep Learning, and LLM-based approaches.

### Parameters
The list of the runner’s parameters is the following:

### 🩹 Imputation

| Parameter         | Task      | Default       | Description                                   |
|:------------------|:----------|:--------------|:----------------------------------------------|
| `--task`          | Upstream  | `imputation`  | load the correct setup for the task           |
| `--imp_algs`      | Upstream  | `SAITS`       | list of imputation algorithms - `all` for all |
| `--datasets_type` | Upstream  | `forecasting` | load the speciality of the task datasets      |
| `--datasets_list` | Upstream  | `paris`       | list of datasets - `all` for all              |
| `--patterns`      | Upstream  | `mcar`        | list of patterns - `all` for all              |
| `--miss_rate`     | Upstream  | `20%`         | contamination rate - `all` for all            |


### ⚙️ Optional

| Parameter     | Task     | Default | Description                                       |
|:--------------|:---------| :--- |:--------------------------------------------------|
| `--caching`   | Optional | `False` | caching the imputed matrix and downstream results |
| `--metrics`   | Optional | `RMSE` | list of metrics - `all` for all                   |
| `--plots`     | Optional | `True` | generate plots                                    |
| `--verbose`   | Optional | `False` | display the detail of execution                   |

<i>With the **“caching”** tag, you can store the imputed matrix and, for downstream tasks, the classification or prediction results. This allows you to rerun the benchmark without having to recompute pipelines that have already been executed.</i>

### Experiments Examples

- To produce the imputation results with 
one forecasting dataset (Paris),
one imputation algorithm (SAITS),
one missingness pattern (MCAR),
a missing rate (20%),
all upstream metrics,
and a future prediction horizon (12 timestamps),
run the following command:

``` bash
python cleanimp_bench.py \
    --task imputation \
    --imp_algs SAITS \
    --datasets_type forecasting \
    --datasets_list paris \
    --patterns mcar \
    --miss_rate 0.2 \
    --metrics all \
    --horizon 12
```
After running the benchmark, the generated results can be found in: `./imputegap_assets/benchmark/[unique_bench_name]/`

<br />

- To produce the imputation results with
two imputation algorithms (MICE and MeanImpute),
two datasets (Paris and ILI),
two missingness patterns (MCAR and SeqN),
two missingness rates (0.1 and 0.8),
and a future prediction horizon (24 timestamps),
run the following command:

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

- To produce the imputation results with all possible configurations,
replace the parameter values with `all`:
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

### Parameters
The list of the runner’s parameters is the following:

| Parameter          | Task          | Default        | Description                   |
|:-------------------|:--------------|:---------------|:------------------------------|
| `--task`           | Forecasting   | `forecasting`  | load the correct setup for the task  |
| `--downstream_mod` | Forecasting   | `chronos`      | forecasting model             |
| `--horizon`        | Forecasting   | `12`           | horizon value for forecasting |


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

### Parameters
The list of the runner’s parameters is the following:

| Parameter          | Task             | Default            | Description          |
|:-------------------|:-----------------|:-------------------|:---------------------|
| `--task`           | Classification   | `classification`   | load the correct setup for the task  |
| `--downstream_mod` | Classification   | `arsenal`          | classification model |


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



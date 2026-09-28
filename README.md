# CleanImp Benchmark

## 1. Introduction

**CleanImp** is an end-to-end benchmark for evaluating the impact of time series imputation on downstream tasks. This GitHub is related to the paper:
**CleanImp: Benchmarking the Impact of Time Series Imputation on Downstream Quality [Experiment, Analysis & Benchmark]**

The benchmark follows the complete experimental pipeline introduced in
the CleanImp paper:

``` text
Time Series → Contamination → Imputation → Downstream Model → Evaluation
```

------------------------------------------------------------------------

## 2. Prerequisites

CleanImp is implemented in **Python** and relies on **ImputeGAP** for time series contamination and imputation. Please start by downloading the GitHub repository:

``` bash
git clone https://github.com/eXascaleInfolab/CleanImp
cd ./CleanImp/
```

To set up your environment and prepare the development installation for C++, please run this script of installation:

``` bash
source cleanimp_install.sh
```


------------------------------------------------------------------------

## 3. Configurations

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
│           │           ├── _metrics_subplot.jpg
│           │           │                      # Metric evolution across missingness rates
│           │           └── report_<pattern>_<dataset>.txt
│           │                                  # Detailed results for this dataset/pattern
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


## 4. Parameters

We will review the different demos together, but here are the runner’s parameters:

### 🩹 Upstream

| Parameter      | Task      | Default | Description                                   |
|:---------------|:----------|:--------|:----------------------------------------------|
| `--datasets`   | Upstream  | `paris` | list of datasets - `all` for all              |
| `--imp_algs`   | Upstream  | `SAITS` | list of imputation algorithms - `all` for all |
| `--patterns`   | Upstream  | `mcar`  | list of patterns - `all` for all              |
| `--miss_rate`  | Upstream  | `20%`   | contamination rate - `all` for all            |

### 🎯 Downstream

| Parameter          | Task | Default | Description |
|:-------------------| :--- | :--- | :--- |
| `--downstream_mod` | Forecasting | `chronos` | forecasting model |
| `--downstream_mod` | Classification | `arsenal` | classification model |
| `--horizon`        | Forecasting | `12` | horizon value for forecasting |

### ⚙️ Optional

| Parameter     | Task     | Default | Description                                       |
|:--------------|:---------| :--- |:--------------------------------------------------|
| `--upstream`  | Optional | `True` | upstream experiment                               |
| `--caching`   | Optional | `False` | caching the imputed matrix and downstream results |
| `--metrics`   | Optional | `RMSE` | list of metrics - `all` for all                   |
| `--plots`     | Optional | `True` | generate plots                                    |
| `--verbose`   | Optional | `False` | display the detail of execution                   |

<i>With the **“caching”** tag, you can store the imputed matrix and, for downstream tasks, the classification or prediction results. This allows you to rerun the benchmark without having to recompute pipelines that have already been executed.</i>


------------------------------------------------------------------------

## 5. Forecasting Demo

CleanImp supports **17 forecasting models** spanning Statistical, Machine Learning, Deep Learning, and LLM-based approaches.


### Upstream evaluation (f)

A simple upstream experiment can use the following configuration: one dataset (Paris), one imputation algorithm (SAITS), one missingness pattern (MCAR), a missing rate (20%), and a future prediction horizon (12 timestamps).

``` bash
python cleanimp_f_benchmark.py \
    --upstream \
    --datasets paris \
    --imp_algs SAITS \
    --patterns mcar \
    --miss_rate 0.2 \
    --horizon 12
```

Our benchmark arguments accept multiple values, allowing you to evaluate several datasets, missingness patterns, and imputation algorithms within a single run. You can customize the command to suit your needs by adding, removing, or modifying the different parameters.

``` bash
python cleanimp_f_benchmark.py \
    --upstream \
    --imp_algs MICE MeanImpute \
    --datasets paris ili \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8 \
    --horizon 24
```

For the full forecasting benchmark (upstream), to run all possible benchmark configurations, replace the parameter values with `all`:<br /><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>


``` bash
python cleanimp_f_benchmark.py \
    --upstream \
    --datasets all \
    --patterns all \
    --imp_algs all \
    --miss_rate all \
    --horizon 12
```


### Downstream evaluation (f)

To evaluate the impact of imputation on a forecasting model, disable upstream-only evaluation and specify a downstream model:

``` bash
python cleanimp_f_benchmark.py \
    --no-upstream \
    --downstream_mod chronos \
    --imp_algs SAITS \
    --datasets paris \
    --patterns mcar \
    --miss_rate 0.2 \
    --horizon 12
```

You can evaluate several datasets, missingness patterns, and imputation algorithms within a single run. You can customize the command to suit your needs by adding, removing, or modifying the different parameters.

``` bash
python cleanimp_f_benchmark.py \
    --no-upstream \
    --downstream_mod chronos \
    --imp_algs MICE MeanImpute \
    --datasets paris ili \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8 \
    --horizon 24
```

For the full forecasting benchmark (downstream), to run all possible benchmark configurations, replace the parameter values with `all`: <br /><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>

``` bash
python cleanimp_f_benchmark.py \
    --no-upstream \
    --downstream_mod chronos \
    --imp_algs all \
    --datasets all \
    --patterns all \
    --miss_rate all \
    --horizon 12
```


------------------------------------------------------------------------

## 6. Classification Demo

CleanImp supports **16 classification models** spanning Statistical, Machine Learning, and Deep Learning-based approaches.


### Upstream evaluation (c)

A simple upstream experiment can use the following configuration: one dataset (Computers), one imputation algorithm (GRIN), one missingness pattern (MCAR), and a missing rate (20%).

``` bash
python cleanimp_c_benchmark.py \
    --upstream \
    --datasets Computers \
    --imp_algs GRIN \
    --patterns mcar \
    --miss_rate 0.2
```

Our benchmark arguments accept multiple values, allowing you to evaluate several datasets, missingness patterns, and imputation algorithms within a single run. You can customize the command to suit your needs by adding, removing, or modifying the different parameters.

``` bash
python cleanimp_c_benchmark.py \
    --upstream \
    --imp_algs MeanImpute MICE \
    --datasets Computers Car \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8
```


For the full classification benchmark (upstream), to run all possible benchmark configurations, replace the parameter values with `all`: <br /><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>

``` bash
python cleanimp_c_benchmark.py \
    --upstream \
    --datasets all \
    --imp_algs all \
    --patterns all \
    --miss_rate all
```


### Downstream evaluation (c)

To evaluate the impact of imputation on a classification model, disable upstream-only evaluation and specify a downstream model:

``` bash
python cleanimp_c_benchmark.py \
    --no-upstream \
    --downstream_mod arsenal \
    --datasets Computers \
    --imp_algs GRIN \
    --patterns mcar \
    --miss_rate 0.2
```

Our benchmark arguments accept multiple values. For example, several datasets, missingness patterns, and imputation algorithms can be evaluated in a single run:

``` bash
python cleanimp_c_benchmark.py \
    --no-upstream \
    --downstream_mod arsenal \
    --datasets Computers Car \
    --imp_algs MeanImpute MICE \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8
```

For the full classification benchmark (downstream), to run all possible benchmark configurations, replace the parameter values with `all`: <br /><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>

``` bash
python cleanimp_c_benchmark.py \
    --no-upstream \
    --downstream_mod arsenal \
    --datasets all  \
    --imp_algs all \
    --patterns all \
    --miss_rate all
```


------------------------------------------------------------------------
# CleanIMP: A comprehensive Framework to Evaluate the Impact of Imputation on Downstream Tasks

CleanIMP is a unified framework designed to extensively evaluate the downstream effects of 10 advanced and five
basic imputation algorithms for time series data. It evaluates two downstream tasks: classification and forecasting
using 89 datasets, 27 downstream techniques, and various contamination scenarios. Technical details can be found in our
paper: Does Cleaning Time Series Really Matter? An Evaluation of the Impact of Imputation on Downstream Tasks (under review at PVLDB'25) </a>. 


 [**Prerequisites**](#prerequisites) | [**Build**](#build) | [**Configuration**](#benchmark-configuration) | [**Execution**](#execution) | [**Extension**](#extension) | [**Contributors**](#contributors) |


---

## Prerequisites

- Ubuntu 22 or Ubuntu 24 (including Ubuntu derivatives, e.g., Xubuntu) or the same distribution under WSL.
- Clone this repository
 

---

## Build
- Build the Testing Framework using the installation script located in the root folder 

```bash
    $ sh setup.sh
```
  

## Benchmark Configuration 

- **Datasets**: The datasets are downloaded by running the setup script. They are task-dependent and can be found in `WorkDir/_RawDataStorage` in resp. folders `UniClass` or `Forecasting`. 

- **Missing Patterns**: `Scenarios` control the patterns of contamination performed by the benchmark. The available options are listed in the table below.

| Scenario      | Task           | Description  |
| --------      | --------       | --------     |
| miss_percNN   | Classification | contaminate NN% of all time series and vary the size of the missing block from 10% to 80% of the length of the series; NN can be [10, 20 ... 50] |
| mc_NN         | Classification | vary the number of contaminated series from 10% to 100%, each affected time series has a missing block of NN% of the length of the series; NN can be [10, 20 ... 50] |
| miss_perc_rev | Forecasting    | contaminate a single time series and vary the size of the missing block from 10% to 80% of the length of the series |
| mc_rev        | Forecasting    | vary the number of contaminated series from 10% to 100%, each affected time series has a missing block of 10% of the length of the series |



- **Algorithms**: The list of Algorithms and their parameters is provided below. The parameters can be overritten from their defaults by specifying the algorithm in the config file as `algorithm:p00` where `p` is the name of the parameter and `00` is the value. For example IMM with the neighborhood size 5 is `IIM:n5`.

| Algorithms | param      | default  | param. descr. | range    |
| --------   | --------   | -------- | --------      | -------- |
| CDRec      | k          | 3        | truncation    | [1, 10]  |
| SVDImp     | k          | 3        | truncation    | [1, 10]  |
| SoftImp    | k          | 3        | truncation    | [1, 10]  |
| STMVL      | n/a        |          |               |          |
| DynaMMo    | k          | 3        | hidden var.   | [1, 10]  |
| IIM        | n          | 3        | neighbors     | [1, 100] |
| GROUSE     | k          | 3        | truncation    | [1, 10]  |
| SVT        | n/a        |          |               |          |
| --------   | --------   | -------- | --------      | -------- |
| MeanImp    | n/a        |          |               |          |
| ZeroImp    | n/a        |          |               |          |
| 1NNImp     | n/a        |          |               |          |
| LinearImp  | n/a        |          |               |          |
| knnimp     | n          | 3        | neighbors     | [1, 100] |

- **Config files**: The existing config files for classification `config_uniclass_custom.cfg` and `config_forecast_custom.cfg` create new customized experiment runs. Those files already contain lists of available options, but this section provides descriptions of the most important parameters available in the benchmark.



## Execution

- To produce a curated set of results, run the following command (takes ~ 5 days on a server-grade CPU):
  
```bash
    $ sh experiments.sh
```

The output will be stored in the `Results/` folder, which will be created in the root folder.


## Execution (fine-grained)

- This section gives some examples on how to produce different analysis results on a customized config file. Their current configuration is geared towards a smaller experiment that can be completed in a reasonable timespan.

- To produce the classification and resp. forecasting experiment runs, run the following commands:

```bash
    $ cd TestFramework/
    $ dotnet run ../configs/config_uniclass_custom.cfg
    $ dotnet run ../configs/config_forecast_custom.cfg
```

- Running the configuration file will execute the experiment specified there and cache the upstream/downstream result data.
- To produce the analysis of the runs, a parametrized `analysis` argument needs to be specified after the name of the config file.
  For example, the following command produces the analysis for the classification run using rmse as an upstream metric:

```bash
    $ dotnet run ../configs/config_uniclass_custom.cfg analysis upstream:rmse
```

- TBC

---

## Extension

TBA

---

## Contributors

- Mourad Khayati (mourad.khayati@unifr.ch)
- Zakhar Tymchenko (zakhar@exascale.info)

---

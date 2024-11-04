# CleanIMP: A comprehensive Framework to Evaluate the Impact of Imputation on Downstream Tasks

CleanIMP is a unified framework designed to extensively evaluate the downstream effects of 10 advanced and five
basic imputation algorithms for time series data. It evaluates two downstream tasks: classification and forecasting
using 89 datasets, 27 downstream techniques, and various contamination scenarios. Technical details can be found in our
paper: Does Cleaning Time Series Really Matter? An Evaluation of the Impact of Imputation on Downstream Tasks (under review at PVLDB'25) </a>. 


 [**Prerequisites**](#prerequisites) | [**Build**](#build) | [**Execution**](#execution) | [**Extension**](#extension) | [**Contributors**](#contributors) |


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
  

## Execution (Full)

- To produce a curated set of results, run the following command (takes ~ 5 days on a server-grade CPU):
  
```bash
    $ sh experiments.sh
```

The output will be stored in the `Results/` folder, which will be created in the root folder.

## Benchmark configuration

- Any existing config file for classification or forecasting can be used as a basis to make new experiment runs. This section provides the list of parameters which are available in the benchmark.

- Imputation algorithms are given in the table below. Configuration file field name is `Algorithms =`. Parameters can be overriden from their defaults by specifying the algorithm in the config file as `algorithm:p00` where `p` is the name of the parameter and `00` is the value. For example IMM with the neighborhood size 5 is `IIM:n5`.

| Algorithms | param      | default  | description |
| --------   | --------   | -------- | --------    |
| CDRec      | k          | 3        | truncation  |
| SVDImp     | k          | 3        | truncation  |
| SoftImp    | k          | 3        | truncation  |
| STMVL      | n/a        |          |             |
| DynaMMo    | k          | 3        | dimension   |
| IIM        | n          | 3        | neighbors   |
| GROUSE     | k          | 3        | truncation  |
| SVT        | n/a        |          |             |
| --------   | --------   | -------- | --------    |
| MeanImp    | n/a        |          |             |
| ZeroImp    | n/a        |          |             |
| 1NNImp     | n/a        |          |             |
| LinearImp  | n/a        |          |             |
| knnimp     | n          | 3        | neighbors   |

- TBC

## Execution (fine-grained)

- To produce the classification and resp. forecasting experiment runs, run the following commands:
  
```bash
    $ cd TestFramework/
    $ dotnet run ../configs/config_uniclass_main.cfg
    $ dotnet run ../configs/config_forecast_main.cfg
```

- Running the configuration file will execute the experiment specified there and cache the upstream/downstream result data.
- To produce the analysis of the runs, a parametrized `analysis` argument needs to be specified after the name of the config file.
  For example, the following command produces the analysis for the classification run using rmse as an upstream metric:

```bash
    $ dotnet run ../configs/config_uniclass_main.cfg analysis upstream:rmse
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

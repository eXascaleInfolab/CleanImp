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
  

## Execution (everything)

- To produce a curated set of results, run the following command:
  
```bash
    $ sh experiments.sh
```

Estimated running time for a full set of results is around 5 days on a server-grade CPU. The output will be stored to `Results/` folder that will be created in the root folder.

## Execution (fine-grained)

- To produce the classification and resp. forecasting experiment runs, run the following commands:
  
```bash
    $ cd TestFramework/
    $ dotnet run ../configs/config_uniclass_main.cfg
    $ dotnet run ../configs/config_forecast_main.cfg
```

- Running the configuration file is going to execute the experiment specified there and cache the upstream/downstream result data.
- To produce the analysis of the runs, a parametrized `analysis` argument can be specified after the name of the config file. For example the following command produces the analysis for the classification run using rmse as an upstream metric:

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

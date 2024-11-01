# CleanIMP:  An Evaluation of the Impact of Imputation on Downstream Tasks

CleanIMP is a unified framework designed to extensively evaluate the downstream effects of 15 advanced and five
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
  

## Execution

- To produce the classification results, run the following commands
  
```bash
    $ cd TestFramework/
    $ dotnet run ../configs/config_uniclass_test.cfg
    $ dotnet run ../configs/config_uniclass_test.cfg analysis reference:f1
```

- To produce the forecasting results, run the following commands
  
```bash
    $ dotnet run ../configs/config_forecast_test.cfg
    $ dotnet run ../configs/config_forecast_test.cfg analysis reference:smape12
```


---

## Extension

TBA

---

## Contributors

- Mourad Khayati (mourad.khayati@unifr.ch)
- Zakhar Tymchenko (zakhar@exascale.info)

---

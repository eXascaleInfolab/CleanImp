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
 
```bash
    $ git clone https://github.com/eXascaleInfolab/CleanImp CleanImp
```

---

## Build
- Download and unzip the classification datasets

```bash
    $ zenodo_get https://doi.org/10.5281/zenodo.14022916
    $ unzip UniClass.zip -d WorkDir/_RawDataStorage/UniClass/
```

- Download and unzip the forecasting datasets

```bash
    $ zenodo_get https://doi.org/10.5281/zenodo.14023107
    $ unzip Forecast.zip -d WorkDir/_RawDataStorage/Forecast/
```
  
- Build the Testing Framework using the installation script located in the root folder 

```bash
    $ sh setup_guide.sh
```

## Execution

- To execute all the experiments, run the following commands
  
```bash
    $ cd CleanImp
    $ dotnet run ../configs/config_uniclass_test.cfg
    $ dotnet run ../configs/config_forecast_test.cfg
```

- To produce the analysis of the experiments, run the following commands
  
```bash
    $ dotnet run ../configs/config_uniclass_test.cfg analysis reference:f1
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

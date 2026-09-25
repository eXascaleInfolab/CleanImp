# CleanImp Benchmark

## 1. Introduction

**CleanImp** is an end-to-end benchmark for evaluating the impact of
**time series imputation on downstream tasks**. This GitHub is related to the paper:
**CleanImp: Benchmarking the Impact of Time Series Imputation on Downstream Quality [Experiment, Analysis & Benchmark]**

Rather than evaluating imputation only through reconstruction accuracy,
CleanImp measures how different imputation strategies affect the final
performance of **forecasting** and **classification** models.

The benchmark follows the complete experimental pipeline introduced in
the CleanImp paper:

``` text
Time Series → Contamination → Imputation → Downstream Model → Evaluation
```

CleanImp first introduces controlled missing values into complete time
series using different missingness patterns and rates. The contaminated
data is then reconstructed using time series imputation algorithms and
evaluated at two levels:

-   **Upstream evaluation:** measures the quality of the imputation
    itself.
-   **Downstream evaluation:** measures the impact of the imputed data
    on forecasting or classification.

The benchmark covers **32 advanced imputation algorithms**, **33
downstream models**, and **84 real-world datasets**, resulting in
approximately **238K end-to-end cleaning pipelines**.

The imputation algorithms span five families:

-   **Pattern Search (PS)**
-   **Machine Learning (ML)**
-   **Matrix Completion (MC)**
-   **Deep Learning (DL)**
-   **Large Language Models (LLMs)**

The benchmark evaluates their impact on two downstream tasks:
**forecasting** and **classification**.

The objective of CleanImp is to determine **when imputation improves
downstream performance, by how much, and under which dataset,
missingness, and task configurations**.

------------------------------------------------------------------------

## 2. Prerequisites

CleanImp is implemented in **Python** and relies on **ImputeGAP** for
time series contamination and imputation.

Install the required dependencies with:

``` bash
git init
git clone https://github.com/eXascaleInfolab/CleanImp
cd ./CleanImp/cleanimp/
```

``` bash
pip install -e .
```

------------------------------------------------------------------------

------------------------------------------------------------------------

## 3. Configurations

Here is the list of configurations for the benchmarks:

## Upstream

### Imputation algorithms

| **Algorithm** | **Family** | **Venue -- Year** |
|---|---|---|
| NuwaTS [[13]](#ref13) | LLMs | Arxiv -- 2024 |
| GPT4TS [[99]](#ref99) | LLMs | NeurIPS -- 2023 |
| MOMENT [[26]](#ref26) | LLMs | ICML -- 2024 |
| MissNet [[63]](#ref63) | Deep Learning | KDD -- 2024 |
| MPIN [[40]](#ref40) | Deep Learning | PVLDB -- 2024 |
| BayOTIDE [[20]](#ref20) | Deep Learning | ICML -- 2024 |
| BitGraph [[12]](#ref12) | Deep Learning | ICLR -- 2024 |
| TimesNet [[86]](#ref86) | Deep Learning | ICLR -- 2023 |
| SAITS [[18]](#ref18) | Deep Learning | ESWA -- 2023 |
| PriSTI [[44]](#ref44) | Deep Learning | ICDE -- 2023 |
| GRIN [[15]](#ref15) | Deep Learning | ICLR -- 2022 |
| CSDI [[80]](#ref80) | Deep Learning | NeurIPS -- 2021 |
| HKMFT [[84]](#ref84) | Deep Learning | TKDE -- 2021 |
| DeepMVI [[5]](#ref5) | Deep Learning | PVLDB -- 2021 |
| MRNN [[93]](#ref93) | Deep Learning | IEEE TBME -- 2019 |
| BRITS [[10]](#ref10) | Deep Learning | NeurIPS -- 2018 |
| GAIN [[92]](#ref92) | Deep Learning | ICML -- 2018 |
| CDRec [[35]](#ref35) | Matrix Completion | KAIS -- 2020 |
| TRMF [[94]](#ref94) | Matrix Completion | NeurIPS -- 2016 |
| GROUSE [[4]](#ref4) | Matrix Completion | Arxiv -- 2010 |
| ROSL [[76]](#ref76) | Matrix Completion | CVPR -- 2014 |
| SoftImpute [[50]](#ref50) | Matrix Completion | JMLR -- 2010 |
| SVT [[8]](#ref8) | Matrix Completion | SIAM J. Optim. -- 2010 |
| SPIRIT [[65]](#ref65) | Matrix Completion | VLDB -- 2005 |
| IterativeSVD [[82]](#ref82) | Matrix Completion | Bioinformatics -- 2001 |
| TKCM [[85]](#ref85) | Pattern Search | EDBT -- 2017 |
| STMVL [[91]](#ref91) | Pattern Search | IJCAI -- 2016 |
| DynaMMo [[38]](#ref38) | Pattern Search | KDD -- 2009 |
| IIM [[96]](#ref96) | Machine Learning | ICDE -- 2019 |
| XGBoost [[11]](#ref11) | Machine Learning | KDD -- 2016 |
| MICE [[69]](#ref69) | Machine Learning | JSS -- 2011 |
| MissForest [[77]](#ref77) | Machine Learning | Bioinformatics -- 2012 |


### Missingness patterns

| `blks` | `seqn`           | `mcar`                       |
|---|------------------|------------------------------|
| Block size | Sequence Number  | Missing Completely at Random |


## Forecasting
### Forecasting Models

| **Algorithm** | **Family** | **Venue -- Year** |
|---|---|---|
| Chronos [[3]](#ref3) | LLMs | TMLR -- 2024 |
| MOMENT [[26]](#ref26) | LLMs | ICML -- 2024 |
| PatchTST [[62]](#ref62) | Deep Learning | ICLR -- 2023 |
| Transformer [[83]](#ref83) | Deep Learning | NeurIPS -- 2017 |
| LTSF [[95]](#ref95) | Deep Learning | AAAI -- 2023 |
| LSTM [[9]](#ref9) | Deep Learning | Physica A -- 2019 |
| DeepAR [[72]](#ref72) | Deep Learning | IJF -- 2020 |
| DLinear [[95]](#ref95) | Deep Learning | AAAI -- 2023 |
| N-BEATS [[64]](#ref64) | Deep Learning | ICLR -- 2020 |
| NLinear [[95]](#ref95) | Deep Learning | AAAI -- 2023 |
| Holt-Winters [[24]](#ref24) | Statistics | Journal of Forecasting -- 2010 |
| Exp. Smoothing [[24]](#ref24) | Statistics | Journal of Forecasting -- 2010 |
| AutoARIMA [[33]](#ref33) | Statistics | IJACSA -- 2020 |
| Croston [[66]](#ref66) | Statistics | IJF -- 2014 |
| XGBoost [[11]](#ref11) | Machine Learning | KDD -- 2016 |
| LightGBM [[31]](#ref31) | Machine Learning | NeurIPS -- 2017 |
| Prophet [[81]](#ref81) | Machine Learning | PeerJ Preprints -- 2017 |


### Forecasting datasets

| | | | |
|---|---|---|---|
| `czelan` | `electricity` | `etth1` | `etth2` |
| `ili` | `nn5` | `nyse` | `wike2000` |
| `wind_speed` | `airq` | `atm` | `beijing_traffic` |
| `climate` | `economics` | `human_access` | `paris` |


## Classification

### Classification Models

| **Algorithm** | **Family** | **Venue -- Year** |
|---|---|---|
| KNN [[23]](#ref23) | Statistics | KAIS -- 2016 |
| Catch22 [[47]](#ref47) | Statistics | DMKD -- 2019 |
| CNN [[97]](#ref97) | Deep Learning | JSEE -- 2017 |
| LSTM [[30]](#ref30) | Deep Learning | Neural Networks -- 2019 |
| TSF [[16]](#ref16) | Machine Learning | Information Sciences -- 2013 |
| CBOSS [[73]](#ref73) | Machine Learning | DMKD -- 2015 |
| STC [[7]](#ref7) | Machine Learning | TLDKS -- 2017 |
| WEASEL [[74]](#ref74) | Machine Learning | CIKM -- 2017 |
| ShapeDTW [[98]](#ref98) | Machine Learning | Pattern Recognition -- 2018 |
| TSFresh [[14]](#ref14) | Machine Learning | Neurocomputing -- 2018 |
| ProxStump [[48]](#ref48) | Machine Learning | DMKD -- 2019 |
| CIF [[54]](#ref54) | Machine Learning | IEEE BigData -- 2020 |
| ITDE [[55]](#ref55) | Machine Learning | ECML PKDD -- 2020 |
| Arsenal [[56]](#ref56) | Machine Learning | Machine Learning -- 2021 |
| Signature [[58]](#ref58) | Machine Learning | -- -- 2021 |
| SVC [[71]](#ref71) | Machine Learning | DMKD -- 2021 |

### Classification Datasets

| | | | |
|---|---|---|---|
| `Adiac` | `BirdChicken` | `Fish` | `InsectEPGRegularTrain` |
| `Rock` | `OSULeaf` | `SwedishLeaf` | `Worms` |
| `WormsTwoClass` | `Herring` | `Ham` | `EthanolLevel` |
| `Beef` | `Meat` | `OliveOil` | `Strawberry` |
| `Wine` | `Car` | `FaceFour` | `FacesUCR` |
| `ArrowHead` | `CinCECGTorso` | `Colposcopy` | `DistalPhalanxOutlineAgeGroup` |
| `DistalPhalanxOutlineCorrect` | `DistalPhalanxTW` | `ECGFiveDays` | `EOGHorizontalSignal` |
| `EOGVerticalSignal` | `MedicalImages` | `PhalangesOutlinesCorrect` | `ProximalPhalanxOutlineAgeGroup` |
| `ProximalPhalanxOutlineCorrect` | `SemgHandGenderCh2` | `SemgHandMovementCh2` | `SemgHandSubjectCh2` |
| `TwoLeadECG` | `MoteStrain` | `Lightning2` | `Lightning7` |
| `Earthquakes` | `Computers` | `LargeKitchenAppliances` | `FreezerRegularTrain` |
| `FreezerSmallTrain` | `PowerCons` | `RefrigerationDevices` | `ScreenType` |
| `SmallKitchenAppliances` | `SonyAIBORobotSurface1` | `GunPoint` | `GunPointAgeSpan` |
| `GunPointMaleVersusFemale` | `GunPointOldVersusYoung` | `CricketX` | `Haptics` |
| `InlineSkate` | `ToeSegmentation1` | `ToeSegmentation2` | `Yoga` |
| `SharePriceIncrease` | `CBF` | `SyntheticControl` | `Trace` |
| `ShapeletSim` | `TwoPatterns` | `UMD` | `Wafer` |


## Output directory

Each benchmark execution creates a **unique experiment directory**. The
directory tree below directly describes the purpose of each generated
folder and file:

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

In short, the outputs are organized at three levels:

``` text
<experiment>
    ├── <dataset>/<pattern>/   → Detailed results
    ├── _heatmaps/             → Aggregated visual analysis
    └── summary/log files      → Global experiment results and configuration
```

This structure keeps every benchmark run self-contained while allowing
detailed results to be traced back to the corresponding **dataset**,
**missingness pattern**, and **experiment configuration**.



------------------------------------------------------------------------


## 4. Demo - Forecasting

The forecasting benchmark evaluates how different imputation strategies
affect time series forecasting performance.

CleanImp supports **17 forecasting models** spanning Statistical,
Machine Learning, Deep Learning, and LLM-based approaches.

We recommend starting with a small experiment to verify the installation
and become familiar with the benchmark workflow.

We will look together the different scenarios, but here's the parameters of the runner:
``` text
=upstream==============================================================================
datasets                    paris   # list of datasets
imp_algs:                   SAITS   # list of imputation algorithms
patterns:                   mcar    # list of patterns
miss_rate:                  20%     # contamination rate

=downstream=============================================================================
downstream_mod: (f)         chronos # forecasting model
downstream_mod: (c)         arsenal # classifiction model
horizon: (f)                12      # horizon value for forecasting

=optional================================================================================
upstream:                   True    # upstream experiment
caching:                    False   # caching the imputed matrix and downstream results
metrics:                    RMSE    # list of metrics - * for all
plots:                      True    # generate plots
verbose                     False   # display the detail of execution
```


## Upstream evaluation (f)

A simple upstream experiment can use the following configuration:


Run it with:

``` bash
python cleanimp_f_benchmark.py \
    --upstream \
    --datasets paris \
    --imp_algs SAITS \
    --patterns mcar \
    --miss_rate 0.2 \
    --horizon 12
```

In upstream mode, CleanImp contaminates the input time series, applies
the selected imputation algorithm, and evaluates the **imputation
quality**.

### Running multiple configurations (f)

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

CleanImp automatically evaluates the requested combinations and stores
their results in the experiment directory.

### Running the full configurations (f)

To run all possible benchmark configurations, replace the parameter values with `all`: <br></br><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>


``` bash
python cleanimp_f_benchmark.py \
    --upstream \
    --datasets all \
    --patterns all \
    --imp_algs all \
    --miss_rate all \
    --horizon 12
```


## Downstream evaluation (f)

To evaluate the impact of imputation on a forecasting model, disable
upstream-only evaluation and specify a downstream model:

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

This makes it possible to compare the reconstruction quality of an
imputation algorithm with its actual impact on forecasting performance.

### Running multiple configurations (f)

Our benchmark arguments accept multiple values, allowing you to evaluate several datasets, missingness patterns, and imputation algorithms within a single run. You can customize the command to suit your needs by adding, removing, or modifying the different parameters.

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

### Running the full configurations (f)

To run all possible benchmark configurations, replace the parameter values with `all`: <br></br><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>

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

---

## 5. Demo - Classification

The classification benchmark evaluates how different imputation strategies
affect time series classification performance.

CleanImp supports **16 classification models** spanning Statistical, Machine Learning, and Deep Learning-based approaches.

We recommend starting with a small experiment to verify the installation
and become familiar with the benchmark workflow.


## Upstream evaluation (c)

A simple upstream experiment can use the following configuration:


Run it with:

``` bash
python cleanimp_c_benchmark.py \
    --upstream \
    --datasets Computers \
    --imp_algs GRIN \
    --patterns mcar \
    --miss_rate 0.2
```

In upstream mode, CleanImp contaminates the input time series, applies
the selected imputation algorithm, and evaluates the **imputation
quality**.

### Running multiple configurations (c)

Our benchmark arguments accept multiple values, allowing you to evaluate several datasets, missingness patterns, and imputation algorithms within a single run. You can customize the command to suit your needs by adding, removing, or modifying the different parameters.


``` bash
python cleanimp_c_benchmark.py \
    --upstream \
    --imp_algs MeanImpute MICE \
    --datasets Computers Car \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8
```

CleanImp automatically evaluates the requested combinations and stores
their results in the experiment directory.

### Running the full configurations (c)

To run all possible benchmark configurations, replace the parameter values with `all`: <br></br><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>

``` bash
python cleanimp_c_benchmark.py \
    --upstream \
    --datasets all \
    --imp_algs all \
    --patterns all \
    --miss_rate all
```


## Downstream evaluation (c)

To evaluate the impact of imputation on a classification model, disable
upstream-only evaluation and specify a downstream model:

``` bash
python cleanimp_c_benchmark.py \
    --no-upstream \
    --downstream_mod arsenal \
    --datasets Computers \
    --imp_algs GRIN \
    --patterns mcar \
    --miss_rate 0.2
```

This makes it possible to compare the reconstruction quality of an
imputation algorithm with its actual impact on forecasting performance.

### Running multiple configurations (c)

Most benchmark arguments accept multiple values. For example, several
datasets, missingness patterns, and imputation algorithms can be
evaluated in a single run:

``` bash
python cleanimp_c_benchmark.py \
    --no-upstream \
    --downstream_mod arsenal \
    --datasets Computers Car \
    --imp_algs MeanImpute MICE \
    --patterns mcar seqn \
    --miss_rate 0.1 0.8
```

### Running the full configurations (c)

To run all possible benchmark configurations, replace the parameter values with `all`: <br></br><i>⚠️ Be aware that running the full benchmark may take several weeks to complete.</i>

``` bash
python cleanimp_c_benchmark.py \
    --no-upstream \
    --downstream_mod arsenal \
    --datasets all  \
    --imp_algs all \
    --patterns all \
    --miss_rate all
```

---

## References

<a id="ref3"></a>**[3]** Abdul Fatir Ansari et al. 2024. Chronos: Learning the Language of Time Series. Transactions on Machine Learning Research 2024. https://openreview.net/forum?id=gerNCVqqtR

<a id="ref4"></a>**[4]** Laura Balzano, Robert D. Nowak, and Benjamin Recht. 2010. Online Identification and Tracking of Subspaces from Highly Incomplete Information. CoRR abs/1006.4046. http://arxiv.org/abs/1006.4046

<a id="ref5"></a>**[5]** Parikshit Bansal, Prathamesh Deshpande, and Sunita Sarawagi. 2021. Missing Value Imputation on Multidimensional Time Series. Proc. VLDB Endow. 14(11), 2533–2545. http://www.vldb.org/pvldb/vol14/p2533-bansal.pdf

<a id="ref7"></a>**[7]** Aaron Bostrom and Anthony J. Bagnall. 2017. Binary Shapelet Transform for Multiclass Time Series Classification. Trans. Large Scale Data Knowl. Centered Syst. 32, 24–46. https://doi.org/10.1007/978-3-662-55608-5_2

<a id="ref8"></a>**[8]** Jian-Feng Cai, Emmanuel J. Candès, and Zuowei Shen. 2010. A Singular Value Thresholding Algorithm for Matrix Completion. SIAM Journal on Optimization 20(4), 1956–1982. https://doi.org/10.1137/080738970

<a id="ref9"></a>**[9]** Jian Cao, Zhi Li, and Jian Li. 2019. Financial time series forecasting model based on CEEMDAN and LSTM. Physica A 519, 127–139.

<a id="ref10"></a>**[10]** Wei Cao et al. 2018. BRITS: Bidirectional Recurrent Imputation for Time Series. NeurIPS 2018, 6776–6786.

<a id="ref11"></a>**[11]** Tianqi Chen and Carlos Guestrin. 2016. XGBoost: A Scalable Tree Boosting System. KDD 2016, 785–794. https://doi.org/10.1145/2939672.2939785

<a id="ref12"></a>**[12]** Xiaodan Chen, Xiucheng Li, Bo Liu, and Zhijun Li. 2024. Biased Temporal Convolution Graph Network for Time Series Forecasting with Missing Values. ICLR 2024. https://openreview.net/forum?id=O9nZCwdGcG

<a id="ref13"></a>**[13]** Jinguo Cheng et al. 2024. NuwaTS: a Foundation Model Mending Every Incomplete Time Series. CoRR abs/2405.15317. https://doi.org/10.48550/ARXIV.2405.15317

<a id="ref14"></a>**[14]** Maximilian Christ et al. 2018. Time Series FeatuRe Extraction on basis of Scalable Hypothesis tests (tsfresh - A Python package). Neurocomputing 307, 72–77. https://doi.org/10.1016/J.NEUCOM.2018.03.067

<a id="ref15"></a>**[15]** Andrea Cini, Ivan Marisca, and Cesare Alippi. 2022. Filling the G_ap_s: Multivariate Time Series Imputation by Graph Neural Networks. ICLR 2022. https://openreview.net/forum?id=kOu3-S3wJ7

<a id="ref16"></a>**[16]** Houtao Deng et al. 2013. A time series forest for classification and feature extraction. Information Sciences 239, 142–153. https://doi.org/10.1016/J.INS.2013.02.030

<a id="ref18"></a>**[18]** Wenjie Du, David Côté, and Yan Liu. 2023. SAITS: Self-attention-based imputation for time series. Expert Systems with Applications 219, 119619. https://doi.org/10.1016/J.ESWA.2023.119619

<a id="ref20"></a>**[20]** Shikai Fang et al. 2024. BayOTIDE: Bayesian Online Multivariate Time Series Imputation with Functional Decomposition. ICML 2024, 12993–13009. https://proceedings.mlr.press/v235/fang24d.html

<a id="ref23"></a>**[23]** Zoltan Geler et al. 2016. Comparison of different weighting schemes for the kNN classifier on time-series data. Knowledge and Information Systems 48(2), 331–378. https://doi.org/10.1007/S10115-015-0881-0

<a id="ref24"></a>**[24]** Sarah Gelper, Roland Fried, and Christophe Croux. 2010. Robust forecasting with exponential and Holt–Winters smoothing. Journal of Forecasting 29(3), 285–300.

<a id="ref26"></a>**[26]** Mononito Goswami et al. 2024. MOMENT: A Family of Open Time-series Foundation Models. ICML 2024, 16115–16152. https://proceedings.mlr.press/v235/goswami24a.html

<a id="ref30"></a>**[30]** Fazle Karim et al. 2019. Multivariate LSTM-FCNs for time series classification. Neural Networks 116, 237–245. https://doi.org/10.1016/j.neunet.2019.04.014

<a id="ref31"></a>**[31]** Guolin Ke et al. 2017. LightGBM: A Highly Efficient Gradient Boosting Decision Tree. NeurIPS 2017, 3146–3154.

<a id="ref33"></a>**[33]** Shakir Khan and Hela Alghulaiakh. 2020. ARIMA model for accurate time series stocks forecasting. International Journal of Advanced Computer Science and Applications 11(7).

<a id="ref35"></a>**[35]** Mourad Khayati, Philippe Cudré-Mauroux, and Michael H. Böhlen. 2020. Scalable recovery of missing blocks in time series with high and low cross-correlations. Knowledge and Information Systems 62(6), 2257–2280. https://doi.org/10.1007/s10115-019-01421-7

<a id="ref38"></a>**[38]** Lei Li et al. 2009. DynaMMo: mining and summarization of coevolving sequences with missing values. KDD 2009, 507–516. https://doi.org/10.1145/1557019.1557078

<a id="ref40"></a>**[40]** Xiao Li et al. 2023. Missing Value Imputation for Multi-attribute Sensor Data Streams via Message Propagation. Proc. VLDB Endow. 17(3), 345–358. https://doi.org/10.14778/3632093.3632100

<a id="ref44"></a>**[44]** Mingzhe Liu et al. 2023. PriSTI: A Conditional Diffusion Framework for Spatiotemporal Imputation. ICDE 2023, 1927–1939. https://doi.org/10.1109/ICDE55515.2023.00150

<a id="ref47"></a>**[47]** Carl Henning Lubba et al. 2019. catch22: CAnonical Time-series CHaracteristics - Selected through highly comparative time-series analysis. Data Mining and Knowledge Discovery 33(6), 1821–1852. https://doi.org/10.1007/S10618-019-00647-X

<a id="ref48"></a>**[48]** Benjamin Lucas et al. 2019. Proximity Forest: an effective and scalable distance-based classifier for time series. Data Mining and Knowledge Discovery 33(3), 607–635. https://doi.org/10.1007/S10618-019-00617-3

<a id="ref50"></a>**[50]** Rahul Mazumder, Trevor Hastie, and Robert Tibshirani. 2010. Spectral Regularization Algorithms for Learning Large Incomplete Matrices. JMLR 11, 2287–2322.

<a id="ref54"></a>**[54]** Matthew Middlehurst, James Large, and Anthony J. Bagnall. 2020. The Canonical Interval Forest (CIF) Classifier for Time Series Classification. IEEE BigData 2020, 188–195. https://doi.org/10.1109/BIGDATA50022.2020.9378424

<a id="ref55"></a>**[55]** Matthew Middlehurst et al. 2020. The Temporal Dictionary Ensemble (TDE) Classifier for Time Series Classification. ECML PKDD 2020, 660–676.

<a id="ref56"></a>**[56]** Matthew Middlehurst et al. 2021. HIVE-COTE 2.0: a new meta ensemble for time series classification. Machine Learning 110(11), 3211–3243. https://doi.org/10.1007/S10994-021-06057-9

<a id="ref58"></a>**[58]** James Morrill, Adeline Fermanian, Patrick Kidger, and Terry Lyons. 2021. A Generalised Signature Method for Multivariate Time Series Feature Extraction. https://api.semanticscholar.org/CorpusID:243830489

<a id="ref62"></a>**[62]** Yuqi Nie et al. 2023. A Time Series is Worth 64 Words: Long-term Forecasting with Transformers. ICLR 2023. https://openreview.net/forum?id=Jbdc0vTOcol

<a id="ref63"></a>**[63]** Kohei Obata et al. 2024. Mining of Switching Sparse Networks for Missing Value Imputation in Multivariate Time Series. KDD 2024, 2296–2306. https://doi.org/10.1145/3637528.3671760

<a id="ref64"></a>**[64]** Boris N. Oreshkin et al. 2020. N-BEATS: Neural basis expansion analysis for interpretable time series forecasting. ICLR 2020.

<a id="ref65"></a>**[65]** Spiros Papadimitriou, Jimeng Sun, and Christos Faloutsos. 2005. Streaming Pattern Discovery in Multiple Time-Series. VLDB 2005, 697–708.

<a id="ref66"></a>**[66]** S.D. Prestwich et al. 2014. Forecasting intermittent demand by hyperbolic-exponential smoothing. International Journal of Forecasting 30(4), 928–933. https://doi.org/10.1016/j.ijforecast.2014.01.006

<a id="ref69"></a>**[69]** Patrick Royston and Ian R. White. 2011. Multiple Imputation by Chained Equations (MICE): Implementation in Stata. Journal of Statistical Software 45(4), 1–20. https://doi.org/10.18637/jss.v045.i04

<a id="ref71"></a>**[71]** Alejandro Pasos Ruiz et al. 2021. The great multivariate time series classification bake off: a review and experimental evaluation of recent algorithmic advances. Data Mining and Knowledge Discovery 35(2), 401–449. https://doi.org/10.1007/S10618-020-00727-3

<a id="ref72"></a>**[72]** David Salinas et al. 2020. DeepAR: Probabilistic forecasting with autoregressive recurrent networks. International Journal of Forecasting 36(3), 1181–1191. https://doi.org/10.1016/j.ijforecast.2019.07.001

<a id="ref73"></a>**[73]** Patrick Schäfer. 2015. The BOSS is concerned with time series classification in the presence of noise. Data Mining and Knowledge Discovery 29(6), 1505–1530. https://doi.org/10.1007/S10618-014-0377-7

<a id="ref74"></a>**[74]** Patrick Schäfer and Ulf Leser. 2017. Fast and Accurate Time Series Classification with WEASEL. CIKM 2017, 637–646. https://doi.org/10.1145/3132847.3132980

<a id="ref76"></a>**[76]** Xianbiao Shu, Fatih Porikli, and Narendra Ahuja. 2014. Robust Orthonormal Subspace Learning: Efficient Recovery of Corrupted Low-Rank Matrices. CVPR 2014, 3874–3881.

<a id="ref77"></a>**[77]** Daniel J. Stekhoven and Peter Bühlmann. 2012. MissForest - non-parametric missing value imputation for mixed-type data. Bioinformatics 28(1), 112–118. https://doi.org/10.1093/BIOINFORMATICS/BTR597

<a id="ref80"></a>**[80]** Yusuke Tashiro et al. 2021. CSDI: Conditional Score-based Diffusion Models for Probabilistic Time Series Imputation. NeurIPS 2021, 24804–24816.

<a id="ref81"></a>**[81]** Sean J. Taylor and Benjamin Letham. 2017. Forecasting at Scale. PeerJ Preprints 5, e3190. https://doi.org/10.7287/PEERJ.PREPRINTS.3190V1

<a id="ref82"></a>**[82]** Olga G. Troyanskaya et al. 2001. Missing value estimation methods for DNA microarrays. Bioinformatics 17(6), 520–525. https://doi.org/10.1093/bioinformatics/17.6.520

<a id="ref83"></a>**[83]** Ashish Vaswani et al. 2017. Attention Is All You Need. CoRR abs/1706.03762. http://arxiv.org/abs/1706.03762

<a id="ref84"></a>**[84]** Liang Wang et al. 2021. HKMF-T: Recover From Blackouts in Tagged Time Series With Hankel Matrix Factorization. IEEE TKDE 33(11), 3582–3593. https://doi.org/10.1109/TKDE.2020.2971190

<a id="ref85"></a>**[85]** Kevin Wellenzohn et al. 2017. Continuous Imputation of Missing Values in Streams of Pattern-Determining Time Series. EDBT 2017, 330–341.

<a id="ref86"></a>**[86]** Haixu Wu et al. 2023. TimesNet: Temporal 2D-Variation Modeling for General Time Series Analysis. ICLR 2023. https://openreview.net/forum?id=ju_Uqw384Oq

<a id="ref91"></a>**[91]** Xiuwen Yi et al. 2016. ST-MVL: Filling Missing Values in Geo-Sensory Time Series Data. IJCAI 2016, 2704–2710.

<a id="ref92"></a>**[92]** Jinsung Yoon, James Jordon, and Mihaela van der Schaar. 2018. GAIN: Missing Data Imputation using Generative Adversarial Nets. ICML 2018, 5675–5684.

<a id="ref93"></a>**[93]** Jinsung Yoon, William R. Zame, and Mihaela van der Schaar. 2019. Estimating Missing Data in Temporal Data Streams Using Multi-Directional Recurrent Neural Networks. IEEE TBME 66(5), 1477–1490. https://doi.org/10.1109/TBME.2018.2874712

<a id="ref94"></a>**[94]** Hsiang-Fu Yu, Nikhil Rao, and Inderjit S. Dhillon. 2016. Temporal Regularized Matrix Factorization for High-dimensional Time Series Prediction. NeurIPS 2016, 847–855.

<a id="ref95"></a>**[95]** Ailing Zeng et al. 2023. Are Transformers Effective for Time Series Forecasting? AAAI 2023, 11121–11128. https://doi.org/10.1609/AAAI.V37I9.26317

<a id="ref96"></a>**[96]** Aoqian Zhang et al. 2019. Learning Individual Models for Imputation. ICDE 2019, 160–171. https://doi.org/10.1109/ICDE.2019.00023

<a id="ref97"></a>**[97]** Bendong Zhao et al. 2017. Convolutional neural networks for time series classification. Journal of Systems Engineering and Electronics 28(1), 162–169.

<a id="ref98"></a>**[98]** Jiaping Zhao and Laurent Itti. 2018. shapeDTW: Shape Dynamic Time Warping. Pattern Recognition 74, 171–184. https://doi.org/10.1016/J.PATCOG.2017.09.020

<a id="ref99"></a>**[99]** Tian Zhou et al. 2023. One Fits All: Power General Time Series Analysis by Pretrained LM. NeurIPS 2023.


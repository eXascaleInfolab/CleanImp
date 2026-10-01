# CleanImp interactive benchmark

Static Plotly.js dashboard for GitHub Pages.

Currently included:
- Upstream / Classification: RMSE
- Downstream / Classification: F1
- Downstream classifiers: 16
- Downstream imputers: Dynammo, GRIN, MICE, MeanImpute, Moment, TRMF
- Downstream datasets: 68
- Patterns: aligned_series, aligned_timestamps, mcar
- Rates: 0.1, 0.2, 0.4, 0.6, 0.8

The **Classifier** dropdown appears only for `Downstream + Classification`.

## Filter behavior

- `Upstream / Classification`: no classifier/model selector; all available imputation algorithms are shown.
- `Downstream / Classification`: the `Classifier` selector is shown and all available imputation algorithms are displayed directly as checkboxes.
- The algorithm checkboxes always show all imputers available for the selected dataset/pattern/classifier configuration.


## Upstream / Forecasting

Imported from `for_up.xlsx`.

- datasets: 16 (airq, atm, beijing_traffic, climate, czelan, economics, electricity, etth1, etth2, human_access, ili, nn5, nyse, paris, wike2000, wind_speed)
- patterns: aligned_series, aligned_timestamps, mcar
- algorithms: 33, including MeanImpute
- entries verified: 7920
- verification errors: 0

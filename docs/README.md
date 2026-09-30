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

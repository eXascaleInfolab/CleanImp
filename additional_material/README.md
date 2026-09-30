# CleanImp interactive benchmark

Static Plotly.js dashboard for GitHub Pages.

Current dataset: **Upstream / Classification** from `imputers3(2).xlsx`.

- 68 datasets
- 3 missingness patterns
- 31 algorithms (SPIRIT excluded)
- 6324 dataset/pattern/algorithm series
- rates: [0.1, 0.2, 0.4, 0.6, 0.8]

Filters: Experiment, Task, Pattern, Dataset, Family, Algorithms.

Preview locally:

```bash
python -m http.server 8000
```

Then open `/additional_material/benchmark_dashboard/`.

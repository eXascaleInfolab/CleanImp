import argparse
from recovery.benchmark import Benchmark
from tools import utils


def parse_args():
    parser = argparse.ArgumentParser(description="Run downstream forecasting benchmark")

    parser.add_argument(
        "--upstream",
        action=argparse.BooleanOptionalAction,
        default=True,
        help="Call upstream or downstream"
    )

    parser.add_argument(
        "--caching",
        action=argparse.BooleanOptionalAction,
        default=False,
        help="Enable or disable cache"
    )

    parser.add_argument(
        "--downstream_mod",
        nargs="+",
        default=["chronos"],
        help="Forecasters to evaluate"
    )

    parser.add_argument(
        "--horizon",
        type=int,
        default=12,
        help="Forecasting horizon"
    )

    parser.add_argument(
        "--imp_algs",
        nargs="+",
        default=["SAITS"],
        help="Imputation algorithms to evaluate"
    )

    parser.add_argument(
        "--datasets",
        nargs="+",
        default=["paris"],
        help="Datasets to evaluate"
    )

    parser.add_argument(
        "--patterns",
        nargs="+",
        default=["mcar"],
        help="Patterns to evaluate"
    )

    parser.add_argument(
        "--miss_rate",
        nargs="+",
        type=float,
        default=[0.2],
        help="Contamination rates"
    )

    parser.add_argument(
        "--metrics",
        nargs="+",
        type=str,
        default=["RMSE", "DOWNSTREAM_SMAPE", "MI", "CORRELATION", "RUNTIME"],
        help="Enable or disable metrics"
    )

    parser.add_argument(
        "--plots",
        action=argparse.BooleanOptionalAction,
        default=True,
        help="Enable or disable plots"
    )

    parser.add_argument(
        "--verbose",
        action=argparse.BooleanOptionalAction,
        default=False,
        help="Enable or disable verbose"
    )


    return parser.parse_args()


if __name__ == "__main__":
    args = parse_args()

    if args.upstream:
        # launch the evaluation
        bench = Benchmark()
        bench.eval_downstream_forecasting(forecasters=args.downstream_mod,
                                          horizon=args.horizon,
                                          algorithms=args.imp_algs,  # utils.list_of_top_cleanimp_for_algorithms(),
                                          datasets=args.datasets, # "utils.get_dataset_forecasters(directory='datasets/forecast/'),
                                          patterns=args.patterns,
                                          x_axis=args.miss_rate,
                                          metrics=args.metrics,
                                          normalizer="z-score",
                                          report_title="cleanimp_benchmark_for_up",
                                          bypass_error=False,
                                          to_cache=args.caching,
                                          use_cache=args.caching,
                                          run_downstream=False,
                                          evaluate_upstream=args.upstream,
                                          fixed_rate=0.2,
                                          verbose=args.verbose,
                                          inner_plots=False,
                                          plots=False,
                                          generate_plot=args.plots,
                                          referential=False,
                                          nbr_series=10000,
                                          nbr_vals=10000)

    else:
        bench = Benchmark()
        bench.eval_downstream_forecasting(forecasters=args.downstream_mod,
                                          horizon=args.horizon,
                                          algorithms=args.imp_algs,  # utils.list_of_top_cleanimp_for_algorithms(),
                                          datasets=args.datasets, # "utils.get_dataset_forecasters(directory='datasets/forecast/'),
                                          patterns=args.patterns,
                                          x_axis=args.miss_rate,
                                          metrics=args.metrics,
                                          normalizer="z-score",
                                          report_title="cleanimp_benchmark_for_down",
                                          bypass_error=False,
                                          to_cache=args.caching,
                                          use_cache=args.caching,
                                          run_downstream=True,
                                          evaluate_upstream=args.upstream,
                                          fixed_rate=0.2,
                                          verbose=args.verbose,
                                          inner_plots=False,
                                          plots=False,
                                          generate_plot=args.plots,
                                          referential=False,
                                          nbr_series=10000,
                                          nbr_vals=10000)
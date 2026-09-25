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
        default=["arsenal"],
        help="Forecasters to evaluate"
    )


    parser.add_argument(
        "--imp_algs",
        nargs="+",
        default=["GRIN"],
        help="Imputation algorithms to evaluate"
    )

    parser.add_argument(
        "--datasets",
        nargs="+",
        default=["Computers"],
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
        default=["RMSE", "DOWNSTREAM_ACC", "MI", "CORRELATION", "RUNTIME"],
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
        # launch the evaluation
        # launch the evaluation
        bench = Benchmark()
        bench.eval_downstream_classification(classifiers=args.downstream_mod,  # for all: utils.list_of_classifiers(),
                                             algorithms=args.imp_algs,  # for all: utils.list_of_algorithms()
                                             datasets=args.datasets, # for all: utils.get_datasets_classifiers(),
                                             patterns=args.patterns,
                                             x_axis=args.miss_rate,
                                             metrics=args.metrics,
                                             normalizer="z-score",
                                             report_title="cleanimp_benchmark_cl_up",
                                             contamination_by_class=True,
                                             imputation_by_class=True,
                                             bypass_error=False,
                                             evaluate_upstream=args.upstream,
                                             to_cache=args.caching,
                                             use_cache=args.caching,
                                             run_downstream=False,
                                             fixed_rate=0.2,
                                             inner_plots=False,
                                             referential=False,
                                             plots=False,
                                             generate_plot=args.plots,
                                             verbose=args.verbose)

    else:
        bench = Benchmark()
        bench.eval_downstream_classification(classifiers=args.downstream_mod,  # for all: utils.list_of_classifiers(),
                                             algorithms=args.imp_algs,  # for all: utils.list_of_algorithms()
                                             datasets=args.datasets, # for all: utils.get_datasets_classifiers(),
                                             patterns=args.patterns,
                                             x_axis=args.miss_rate,
                                             metrics=args.metrics,
                                             normalizer="z-score",
                                             report_title="cleanimp_benchmark_cl_down",
                                             contamination_by_class=True,
                                             imputation_by_class=True,
                                             bypass_error=False,
                                             evaluate_upstream=args.upstream,
                                             to_cache=args.caching,
                                             use_cache=args.caching,
                                             run_downstream=True,
                                             referential=False,
                                             fixed_rate=0.2,
                                             plots=False,
                                             generate_plot=args.plots,
                                             verbose=args.verbose)
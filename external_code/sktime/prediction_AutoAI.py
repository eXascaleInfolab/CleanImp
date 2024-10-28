#!/usr/bin/python3

# basic
import sys;

import warnings;
warnings.simplefilter(action='ignore', category=FutureWarning);
warnings.simplefilter(action='ignore', category=UserWarning);

import numpy as np;
import sktime as skt;
import pandas as pd;
from sktime.forecasting.base import ForecastingHorizon
from windowlen.window_length_selector import get_window;

# Forecasters
from sktime.forecasting.exp_smoothing import ExponentialSmoothing;
from sktime.forecasting.arima import AutoARIMA;
from sktime.forecasting.statsforecast import StatsForecastAutoARIMA;
from sktime.forecasting.bats import BATS;
from sktime.forecasting.tbats import TBATS;
from sktime.forecasting.ets import AutoETS;
from sktime.forecasting.fbprophet import Prophet;
from sktime.forecasting.ltsf import LTSFLinearForecaster;

# DARTS
from src.estimater.forecasting.n_beats_darts import NBeatsDarts

# Compose
from sktime.forecasting.compose import make_reduction
from src.estimater.forecasting.daub_forecaster import TDaub

#
# input
#
if len(sys.argv) < 3:
    print("Insufficient number of CLI arguments. Usage: `python3 forecast.py pred_algo rows_to_predict season slot`");
    exit(-1);
#endif

if len(sys.argv) >= 3:
    season = int(sys.argv[3]);
else:
    season = 0;

if len(sys.argv) >= 4:
    slot = int(sys.argv[4]);
else:
    slot = 0;

matrix = np.loadtxt("data/dataset_" + str(slot) + ".txt");
n = len(matrix);
m = len(matrix[0]);

algo = sys.argv[1];

idx_train = range(0, n);
y_train = pd.Series(index = idx_train, data = matrix[:, 0]);
to_pred = int(sys.argv[2]);

shiftval = 0.0

AUTOAI_TS_RANDOM_STATE = 42

#
# prepare predictions
#

prediction = np.zeros((to_pred, m)); #container for the result

AUTOAI_TS_PIPELINES = (
    (
        "Arima",
        StatsForecastAutoARIMA,
        {"sp": season}
    ),
    (
        "HW Multiplicative",
        ExponentialSmoothing,
        {"sp": season, "trend": "add", "seasonal": "multiplicative"},
    ),
    (
        "HW Additive",
        ExponentialSmoothing,
        {"sp": season, "trend": "add", "seasonal": "add"},
    ),
    (
        "BATS",
        BATS,
        {"sp": season}
    ),
    (
        "Prophet",
        Prophet,
        {}
    ),
    #(
    #    # some external docs
    #    # https://github.com/cure-lab/LTSF-Linear
    #    "LTSF",
    #    LTSFLinearForecaster,
    #    {"seq_len" : 10, "pred_len" : to_pred}
    #),
    (
        "NBeats Darts",
        NBeatsDarts,
        {"input_chunk_length" : 168, "fh" : to_pred}
    )
    #(
    #    "Window SVR",
    #    make_reduction,
    #    {"estimator": SVR(), "window_length": None},
    #),
    #(
    #    "Window RandomForest",
    #    make_reduction,
    #    {"estimator": RandomForestRegressor(), "window_length": None},
    #),
)

AUTOAI_TS_PIPELINES = (
    (
        "HW Additive",
        ExponentialSmoothing,
        {"sp": season, "trend": "add", "seasonal": "add"},
    ),
    (
        "NBeats Darts",
        NBeatsDarts,
        {"input_chunk_length" : 84, "fh" : to_pred, "layer_widths" : 64}
    )
)

if algo == "croston":
    from sktime.forecasting.croston import Croston;
    forecaster = Croston(); #no sp

elif algo == "theta":
    from sktime.forecasting.theta import ThetaForecaster;
    forecaster = ThetaForecaster(sp=season, deseasonalize=False);

elif algo == "autoai-ts":
    print("good to go");
else:
    print("Unrecognized forecaster specified: " + algo);
    exit(-1);
#endif

# AutoAI

forecaster = TDaub(
    learners=AUTOAI_TS_PIPELINES,
    min_allocation_size=110, # instead of 100 to accomodate LTSF
    allocation_size=20,
    fixed_allocation_cutoff=550, # ditto
    geo_increment_size=1.5,
    run_to_completion=1,
    validation_ratio=0.2,
    random_state=AUTOAI_TS_RANDOM_STATE,
)

#
# predict
#

# redo in case idx_train has changed (prophet)
y_train = pd.Series(index = idx_train, data = matrix[:, 0]);
y_train = y_train.add(shiftval) # will be 0.0 unless HW-Multiplicative

forecaster.fit(y_train, fh = ForecastingHorizon(np.array(range(0, to_pred), dtype=int)));
y_pred = forecaster.predict();

prediction = (y_pred.to_numpy() - shiftval).reshape(to_pred); #-shift because the value is non-negative

np.savetxt("data/output_" + str(slot) + ".txt", prediction, fmt='%.18f');

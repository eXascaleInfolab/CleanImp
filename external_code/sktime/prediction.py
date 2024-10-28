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

idx_train = range(0, n);
y_train = pd.Series(index = idx_train, data = matrix[:, 0]);

algo = sys.argv[1];
to_pred = int(sys.argv[2]);

is_special = False
shiftval = 0.0

AUTOAI_TS_RANDOM_STATE = 42

#
# prepare predictions
#

prediction = np.zeros((to_pred, m)); #container for the result

if algo == "hw-mul":
    shiftval = y_train.min()
    if shiftval < 0:
        shiftval = (shiftval * -1.1 + 0.3) # this will become strictly positive
    else:
        shiftval = 0.0
    from sktime.forecasting.exp_smoothing import ExponentialSmoothing;
    forecaster = ExponentialSmoothing(sp=season, trend="add", seasonal="multiplicative");

elif algo == "hw-add":
    from sktime.forecasting.exp_smoothing import ExponentialSmoothing;
    forecaster = ExponentialSmoothing(sp=season, trend="add", seasonal="additive");

elif algo == "arima":
    from sktime.forecasting.arima import AutoARIMA;
    forecaster = AutoARIMA(
        sp=season,
        suppress_warnings = True,
        start_p=1,
        start_q=1,
        max_p=3,
        max_q=3,
        start_P=0,
        seasonal=True,
        d=1,
        D=1,
    );

elif algo == "sf-arima":
    from sktime.forecasting.statsforecast import StatsForecastAutoARIMA;
    forecaster = StatsForecastAutoARIMA(
        sp=season,
        start_p=1,
        start_q=1,
        max_p=3,
        max_q=3,
        start_P=0,
        seasonal=True,
        d=1,
        D=1
    );
    forecaster.set_config(warnings='off');

elif algo == "arima3":
    from sktime.forecasting.arima import ARIMA;
    forecaster = ARIMA(sp=season, order = (3,0,0), suppress_warnings = True);

elif algo == "bats":
    from sktime.forecasting.bats import BATS;
    forecaster = BATS(sp=season, use_trend=True, use_box_cox=False);

elif algo == "tbats":
    from sktime.forecasting.tbats import TBATS;
    forecaster = TBATS(sp=season, use_trend=True, use_box_cox=False);

elif algo == "ets":
    from sktime.forecasting.ets import AutoETS;
    forecaster = AutoETS(sp=season, auto=True);

elif algo == "sf-ets":
    from sktime.forecasting.statsforecast import StatsForecastAutoETS;
    forecaster = StatsForecastAutoETS(season_length=season);

elif algo == "croston":
    from sktime.forecasting.croston import Croston;
    forecaster = Croston(); #no sp

elif algo == "theta":
    from sktime.forecasting.theta import ThetaForecaster;
    forecaster = ThetaForecaster(sp=season, deseasonalize=False);

elif algo == "unobs":
    from sktime.forecasting.structural import UnobservedComponents;
    forecaster = UnobservedComponents(); #no sp

elif algo == "ltsf":
    # some external docs
    # https://github.com/cure-lab/LTSF-Linear
    from sktime.forecasting.ltsf import LTSFLinearForecaster;
    forecaster = LTSFLinearForecaster(seq_len=168, pred_len=to_pred);
    is_special = True

elif algo == "rnn":
    from sktime.forecasting.neuralforecast import NeuralForecastRNN;
    forecaster = NeuralForecastRNN(input_size=168, inference_input_size=12);

elif algo == "fbprophet":
    from sktime.forecasting.fbprophet import Prophet;
    forecaster = Prophet(
        n_changepoints=25,
        changepoint_range=0.8,
        yearly_seasonality="auto",
        weekly_seasonality="auto",
        daily_seasonality="auto",
        holidays=None,
        seasonality_mode="additive",
        mcmc_samples=0,
        seasonality_prior_scale=10,
        changepoint_prior_scale=0.05,
        alpha=0.8,
        uncertainty_samples=1000,
    );
    idx_train = pd.date_range(start='01/01/2021', periods = n, freq='D'); #prophet requires DatetimeIndex, range won't work

else:
    print("Unrecognized forecaster specified: " + algo);
    exit(-1);
#endif

#
# predict
#

# redo in case idx_train has changed (prophet)
y_train = pd.Series(index = idx_train, data = matrix[:, 0]);
y_train = y_train.add(shiftval) # will be 0.0 unless HW-Multiplicative

if is_special:
    forecaster.fit(y_train, fh = ForecastingHorizon(np.array(range(0, to_pred), dtype=int)));
    y_pred = forecaster.predict();
else:
    forecaster.fit(y_train);
    y_pred = forecaster.predict(fh = ForecastingHorizon(np.array(range(0, to_pred), dtype=int), is_relative=True));

prediction = (y_pred.to_numpy() - shiftval).reshape(to_pred); #-shift because the value is non-negative

np.savetxt("data/output_" + str(slot) + ".txt", prediction, fmt='%.18f');

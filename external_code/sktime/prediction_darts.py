#!/usr/bin/python3

# basic
import sys;

import warnings;
warnings.simplefilter(action='ignore', category=FutureWarning);

import numpy as np;
import sktime as skt;
import pandas as pd;
from darts import TimeSeries;

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
to_pred = int(sys.argv[2]);

AUTOAI_TS_RANDOM_STATE = 42

#
# prepare predictions
#
idx_train = range(0, n);
prediction = np.zeros((to_pred, m)); #container for the result

if algo == "expsmooth":
    from darts.models import ExponentialSmoothing;
    forecaster = ExponentialSmoothing();

elif algo == "nbeats":
    from darts.models import NBEATSModel;
    forecaster = NBEATSModel(
        input_chunk_length=168,
        output_chunk_length=12,
        num_blocks=3,
        layer_widths=128,
        random_state=AUTOAI_TS_RANDOM_STATE,
        n_epochs=50,
        pl_trainer_kwargs={"accelerator": "cpu"});

elif algo == "nbeats-gpu":#do not use
    from darts.models import NBEATSModel;
    import torch
    torch.set_float32_matmul_precision('medium')
    #forecaster = NBEATSModel(input_chunk_length=24, output_chunk_length=12, pl_trainer_kwargs={"accelerator": "gpu", "devices": -1, "auto_select_gpus": True});
    forecaster = NBEATSModel(input_chunk_length=24, output_chunk_length=12, pl_trainer_kwargs={"accelerator": "gpu", "devices": [0]});

elif algo == "xgboost":
    from darts.models.forecasting.xgboost import XGBModel;
    forecaster = XGBModel(lags=season);

elif algo == "lightgbm":
    from darts.models.forecasting.lgbm import LightGBMModel;
    forecaster = LightGBMModel(lags=season, verbose=-1);

elif algo == "lstm":
    from darts.models.forecasting.rnn_model import RNNModel;
    forecaster = RNNModel(
        input_chunk_length=168,
        model = 'LSTM',
        random_state=AUTOAI_TS_RANDOM_STATE,
        n_epochs=50,
        pl_trainer_kwargs={"accelerator": "cpu"});

elif algo == "deepar":
    from darts.models.forecasting.rnn_model import RNNModel;
    forecaster = RNNModel(
        input_chunk_length=168,
        model = 'RNN',
        random_state=AUTOAI_TS_RANDOM_STATE,
        n_epochs=50,
        pl_trainer_kwargs={"accelerator": "cpu"});

elif algo == "transformer":
    from darts.models.forecasting.transformer_model import TransformerModel;
    forecaster = TransformerModel(
        input_chunk_length=168,
        output_chunk_length=12,
        random_state=AUTOAI_TS_RANDOM_STATE,
        n_epochs=50,
        pl_trainer_kwargs={"accelerator": "cpu"});

elif algo == "transformer-gpu":
    from darts.models.forecasting.transformer_model import TransformerModel;
    forecaster = TransformerModel(
        input_chunk_length=168,
        output_chunk_length=12,
        random_state=AUTOAI_TS_RANDOM_STATE,
        n_epochs=50,
        pl_trainer_kwargs={"accelerator": "gpu", "devices": [0]});

else:
    print("Unrecognized forecaster specified: " + algo);
    exit(-1);
#endif

#
# predict
#

y_train = TimeSeries.from_values(matrix[:, 0]);
forecaster.fit(y_train);

y_pred = forecaster.predict(n = to_pred);
prediction = y_pred.pd_dataframe().to_numpy().reshape(to_pred);

np.savetxt("data/output_" + str(slot) + ".txt", prediction, fmt='%.18f');

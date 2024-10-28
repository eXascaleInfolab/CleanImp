#!/usr/bin/python3

import sys;
import warnings;
import numpy as np;
import sktime as skt;
import pandas as pd;
warnings.simplefilter(action='ignore', category=FutureWarning);
warnings.simplefilter(action='ignore', category=pd.errors.PerformanceWarning);

from sktime.datasets import load_from_tsfile_to_dataframe;

#
# cli input
#
if len(sys.argv) < 3:
    print("Insufficient number of CLI arguments. Usage: `python3 classify.py classif_algo slot`");
    exit(-1);
#endif

classifier_string = sys.argv[1];

#
# prepare classification
#

X_train, y_train = load_from_tsfile_to_dataframe('data/dataset_TRAIN_' + sys.argv[2] + '.ts');
X_test,  y_test  = load_from_tsfile_to_dataframe('data/dataset_TEST_'  + sys.argv[2] + '.ts');

parallel_threads = 1; # todo: replace with sys.argv[2] and set one above to static

def make_boring(X_train, X_test, y_train):
    # Step 1: transform test set from string/object to int and store a dictionary
    myDict = {}
    newTrain = [];
    for i in range(0, len(y_train)):
        classVal = y_train[i];
        keys = [k for k, v in myDict.items() if v == classVal];
        if len(keys) == 0:
            keyVal = len(myDict);
            myDict[keyVal] = y_train[i];
        else:
            keyVal = keys[0];
        #endif
        newTrain.append(keyVal);
    #end for
    y_train = np.array(newTrain);
    
    # Step 2: transform train & test sets from fancy pandas indexed table to boring list of lists
    X_train = X_train.to_numpy();
    X_test = X_test.to_numpy();
    
    series = len(X_train);
    seriesTest = len(X_test);
    seriesLen = len(X_train[0][0]);
    
    newTrain = np.array([X_train[j][0][i] for j in range(0, series) for i in range(0, seriesLen)]);
    newTest = np.array([X_test[j][0][i] for j in range(0, seriesTest) for i in range(0, seriesLen)]);
    
    X_train = newTrain.reshape(series, seriesLen);
    X_test = newTest.reshape(seriesTest, seriesLen);
    
    return [X_train, X_test, y_train, myDict];
#end function

    #
    # Dictionary based
    #
if classifier_string == "muse":
    from sktime.classification.dictionary_based import MUSE;
    classifier = MUSE(random_state=182322303);
    
elif classifier_string == "weasel": #UNIVAR
    from sktime.classification.dictionary_based import WEASEL;
    classifier = WEASEL(n_jobs=parallel_threads, random_state=182322303);
    
elif classifier_string == "itde":
    from sktime.classification.dictionary_based import IndividualTDE;
    classifier = IndividualTDE(random_state=182322303);
    
elif classifier_string == "tde":
    from sktime.classification.dictionary_based import TemporalDictionaryEnsemble;
    classifier = TemporalDictionaryEnsemble(random_state=182322303);
    
elif classifier_string == "cboss":
    from sktime.classification.dictionary_based import ContractableBOSS;
    classifier = ContractableBOSS(n_jobs=parallel_threads, random_state=182322303);
    
    #
    # Distance based
    #
elif classifier_string == "knn":
    from sktime.classification.distance_based import KNeighborsTimeSeriesClassifier;
    classifier = KNeighborsTimeSeriesClassifier(); #no random_state
    
elif classifier_string == "proxforest":
    from sktime.classification.distance_based import ProximityForest;
    classifier = ProximityForest(random_state=182322303);
    
elif classifier_string == "proxtree":
    from sktime.classification.distance_based import ProximityTree;
    classifier = ProximityTree(random_state=182322303);
    
elif classifier_string == "proxstump":
    from sktime.classification.distance_based import ProximityStump;
    classifier = ProximityStump(random_state=182322303);
    
elif classifier_string == "shapedtw":
    from sktime.classification.distance_based import ShapeDTW;
    classifier = ShapeDTW();#no random_state
    # something goes wrong with the original structure
    [X_train, X_test, y_train, myDict] = make_boring(X_train, X_test, y_train);
    
    #
    # Hybrid
    #
elif classifier_string == "hivecote":
    from sktime.classification.hybrid import HIVECOTEV1;
    classifier = HIVECOTEV1();
    
elif classifier_string == "hivecote2":
    from sktime.classification.hybrid import HIVECOTEV2;
    classifier = HIVECOTEV2();
    
    #
    # Interval based
    #
elif classifier_string == "tsf":
    from sktime.classification.interval_based import TimeSeriesForestClassifier;
    classifier = TimeSeriesForestClassifier(n_jobs=parallel_threads, random_state=182322303);
    
elif classifier_string == "cif":
    from sktime.classification.interval_based import CanonicalIntervalForest;
    classifier = CanonicalIntervalForest(n_jobs=parallel_threads, random_state=182322303);
    
    #
    # Shapelet based
    #
elif classifier_string == "stc":
    from sktime.classification.shapelet_based import ShapeletTransformClassifier;
    classifier = ShapeletTransformClassifier(n_jobs=parallel_threads, random_state=182322303);
    
    #
    # NN based
    #
elif classifier_string == "lstm-fcn":
    from sktime.classification.deep_learning import LSTMFCNClassifier;
    classifier = LSTMFCNClassifier(n_epochs=1000, random_state=182322303, verbose=0);
    
elif classifier_string == "cnn":
    from sktime.classification.deep_learning.cnn import CNNClassifier;
    classifier = CNNClassifier(n_epochs=1000, random_state=182322303, verbose=False);
    
    #
    # Kernel based
    #
elif classifier_string == "svc":
    from sktime.classification.kernel_based import TimeSeriesSVC;
    classifier = TimeSeriesSVC(random_state=182322303);

elif classifier_string == "arsenal":
    from sktime.classification.kernel_based import Arsenal;
    classifier = Arsenal(random_state=182322303);

elif classifier_string == "rocket":
    from sktime.classification.kernel_based import RocketClassifier;
    classifier = RocketClassifier(random_state=182322303);
    
    #
    # Feature based
    #
elif classifier_string == "catch22":
    from sktime.classification.feature_based import Catch22Classifier;
    from sklearn.ensemble import RandomForestClassifier;
    classifier = Catch22Classifier(estimator=RandomForestClassifier(n_estimators=200), n_jobs=parallel_threads, random_state=182322303);
    
elif classifier_string == "mpc":
    from sktime.classification.feature_based import MatrixProfileClassifier;
    classifier = MatrixProfileClassifier(random_state=182322303);
    
elif classifier_string == "signature":
    from sktime.classification.feature_based import SignatureClassifier;
    classifier = SignatureClassifier(random_state=182322303);
    
elif classifier_string == "tsfresh":
    from sktime.classification.feature_based import TSFreshClassifier;
    classifier = TSFreshClassifier(random_state=182322303);
    
elif classifier_string == "tsfresh-all":
    from sktime.classification.feature_based import TSFreshClassifier;
    classifier = TSFreshClassifier(relevant_feature_extractor=False, random_state=182322303);
    
    #
    # External (non-sktime)
    #
elif classifier_string == "xgboost":
    import xgboost as xgb;
    classifier = xgb.XGBClassifier(n_jobs=parallel_threads, random_state=182322303);
    # unlike sktime, the structure expectation is very different
    [X_train, X_test, y_train, myDict] = make_boring(X_train, X_test, y_train);
#endif

#
# classify
#

classifier.fit(X_train, y_train)
y_pred = classifier.predict(X_test)

# revert modifications done on classlist
if classifier_string == "xgboost":
    # the only revert needed is to substitute numerical indices of class identifiers with their original forms
    y_pred = [myDict[y] for y in y_pred];

for i in range(0, len(y_pred)):
    print(y_pred[i])

#!/bin/sh
export DEBIAN_FRONTEND=noninteractive
export SETUPTOOLS_USE_DISTUTILS=stdlib

sudo apt-get update
sudo apt-get install -y build-essential cmake libopenmpi-dev libopenblas-dev liblapack-dev libarmadillo-dev
sudo apt-get install -y dotnet-sdk-8.0

# install : py+libraries
sudo apt install -y software-properties-common
sudo add-apt-repository -y ppa:deadsnakes/ppa
sudo apt-get update
sudo apt-get install -y python3.9
sudo apt-get install -y python3.9-distutils

wget https://bootstrap.pypa.io/get-pip.py
python3.9 get-pip.py

# core
python3.9 -m pip install numpy==1.23.5;
python3.9 -m pip install sktime==0.24.1;
python3.9 -m pip install darts==0.27.1;

# targeted
python3.9 -m pip install statsmodels==0.14.1;
python3.9 -m pip install statsforecast==1.5.0;
python3.9 -m pip install protobuf==3.20.0;
python3.9 -m pip install tensorflow==2.11.0;
python3.9 -m pip install pmdarima==2.0.2;
python3.9 -m pip install tbats==1.1.2;
python3.9 -m pip install holidays==0.24;
python3.9 -m pip install prophet==1.1.2;
python3.9 -m pip install lightgbm==4.3.0;
python3.9 -m pip install esig==0.9.7;
python3.9 -m pip install tsfresh==0.20.0;

# arma wrap
cd external_code/ArmaWrap/
make all
sudo cp libArmaWrap.so /usr/lib
cd ../..

#!/bin/sh
cd TestFramework/
dotnet run ../configs/config_uniclass_main.cfg
dotnet run ../configs/config_forecast_main.cfg

mkdir ../Results/
dotnet run ../configs/config_uniclass_main.cfg analysis upstream:rmse:true > ../Results/classification_upstream.txt
dotnet run ../configs/config_uniclass_main.cfg analysis downstream:f1:true > ../Results/classification_downstream_aggregated.txt
dotnet run ../configs/config_uniclass_main.cfg analysis downstream:f1:false > ../Results/classification_downstream.txt

dotnet run ../configs/config_forecast_main.cfg analysis upstream:rmse:true > ../Results/forecasting_upstream.txt
dotnet run ../configs/config_forecast_main.cfg analysis bydata:f1:true > ../Results/forecasting_downstream_aggregated.txt
dotnet run ../configs/config_forecast_main.cfg analysis downstream:f1:false > ../Results/forecasting_downstream.txt

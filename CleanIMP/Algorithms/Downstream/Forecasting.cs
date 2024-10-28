using System.Diagnostics;
using CleanIMP.Utilities;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Downstream;

public static class Forecasting
{
    //
    // Constants/Variables
    //

    private const string SkTimeLocation = "../external_code/sktime/";
    private const string DataFolder = "data/";

    //
    // Main API
    //

    public static (long, Vector<double>) RunForecast(Matrix<double> dataset, int season, int rowsToForecast, string forecastAlgorithm, int slot = 0)
    {
        // step 1 - store data
        string inputFile = SkTimeLocation + DataFolder + $"dataset_{slot}.txt";
        string resultFile = SkTimeLocation + DataFolder + $"output_{slot}.txt";

        dataset.ExportMx().FileWriteAllLines(inputFile);
        
        // step 2 - run
        long runtime;
        
        if (forecastAlgorithm.StartsWith("darts-"))
        {
            forecastAlgorithm = forecastAlgorithm.Substring(forecastAlgorithm.IndexOf('-') + 1);//strip the prefix
            runtime = LaunchDarts(forecastAlgorithm, season, rowsToForecast, slot);
        }
        else
        {
            runtime = LaunchSktime(forecastAlgorithm, season, rowsToForecast, slot);
        }

        Vector<double> output = MathX.LoadVectorFile(resultFile, rowsToForecast);
        return (runtime, output);
    }

    private static long LaunchSktime(string forecastAlgorithm, int season, int rowsToForecast, int slot)
    {
        Stopwatch sw = new();
        sw.Start();
        Utils.RunVoidProcess(Utils.PythonExec, $"prediction.py {forecastAlgorithm} {rowsToForecast} {season} {slot}", SkTimeLocation);
        sw.Stop();
        
        return (long)(sw.Elapsed.TotalMilliseconds * 1000);
    }

    private static long LaunchDarts(string forecastAlgorithm, int season, int rowsToForecast, int slot)
    {
        Stopwatch sw = new();
        sw.Start();
        Utils.RunVoidProcess(Utils.PythonExec, $"prediction_darts.py {forecastAlgorithm} {rowsToForecast} {season} {slot}", SkTimeLocation);
        sw.Stop();
        
        return (long)(sw.Elapsed.TotalMilliseconds * 1000);
    }
}
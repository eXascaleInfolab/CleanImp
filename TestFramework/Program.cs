using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using MathNet.Numerics.LinearAlgebra;

using CleanIMP.Algorithms.Analysis;
using CleanIMP.Utilities;
using CleanIMP.Testing;
using CleanIMP.Config;

namespace CleanIMP;

public static class Program
{
    public static void Main(string[] args)
    {
        /* Testing on a synth example
        Random r = new Random(0x18af1);

        Matrix<double> mat = Utilities.Mathematical.MathX.Zeros(12, 5);
        foreach ((int i, int j, _) in mat.EnumerateIndexed())
        {
            mat[i, j] = r.NextDouble();//.NextGaussian();
        }

        Console.WriteLine(mat.ToMatrixString());

        mat[0, 0] = mat[1, 0] = mat[2, 0] = Double.NaN;
        mat[9, 1] = mat[10, 1] = mat[11, 1] = Double.NaN;
        //mat[2, 2] = mat[3, 2] = mat[4, 2] = mat[5, 2] = Double.NaN;
        mat[2, 2] = mat[3, 2] = mat[4, 2] = Double.NaN;
        
        Console.WriteLine(mat.ToMatrixString());
        
        AlgorithmFactory.ConstructAlgorithm("knnimp:n3").RecoverMatrix(ref mat);
        //new HorizontalMeanImputeAlgorithm().RecoverMatrix(ref mat);
        
        Console.WriteLine(mat.ToMatrixString());
        
        return;
        //*/
        
        if (args.Length == 0)
        {
            Console.WriteLine("Config file name must be supplied as a CLI parameter. Exiting the program.");
            return;
        }
        string configFile = args[0];

        if (!File.Exists(configFile))
        {
            Console.WriteLine($"No config file found at a given location {configFile}. Exiting.");
            return;
        }

        Console.WriteLine($"Using {configFile} as a config, loading...");
        
        Dictionary<string, string> configuration = ReadConfigFile(configFile);

        if (!configuration.ContainsKey("task"))
        {
            Console.WriteLine("Downstream task is not specified in the config file. Exiting the program.");
            return;
        }
        
        // Determine if the job is to run the experiments, or to produce analysis of cached experiments
        bool analysis = false;
        string parametersArg = "downstream:default:true"; // format "job:metric1,metric2:aggregate_type" of types "str:str[]:enum"
        
        if (args.Length > 1) //analysis
        {
            if (args[1].ToLower() != "analysis")
            {
                Console.WriteLine($"Unknown secondary task {args[1]}");
                return;
            }

            analysis = true;
            if (args.Length > 2) parametersArg = args[2];
        }
        
        string[] parameters = parametersArg.Split(':');

        Task task = TaskHelpers.GetTask(configuration.Consume("task"));
        
        switch (task)
        {
            case Task.UnivariateTsClassification:
                UniClassConfig uniClassConfig = new(configuration);
                TaskHelpers.ValidateConfig(uniClassConfig, analysis);
                if (analysis)
                    TestRoutines<TaskUniClassification, UniClassConfig, ScenarioUnivariate, UnivarDataset, string[], ClassificationMetric>.RunAnalysis(uniClassConfig, parameters);
                else
                    TestRoutines<TaskUniClassification, UniClassConfig, ScenarioUnivariate, UnivarDataset, string[], ClassificationMetric>.ExecuteExperiment(uniClassConfig);
                break;
            
            case Task.Forecasting:
                ForecastConfig forecastConfig = new(configuration);
                TaskHelpers.ValidateConfig(forecastConfig, analysis);
                if (analysis)
                    TestRoutines<TaskForecasting, ForecastConfig, ScenarioMultivariate, ForecastDataset, Vector<double>, ForecastMetric>.RunAnalysis(forecastConfig, parameters);
                else
                    TestRoutines<TaskForecasting, ForecastConfig, ScenarioMultivariate, ForecastDataset, Vector<double>, ForecastMetric>.ExecuteExperiment(forecastConfig);
                break;
                
            case Task.TimeSeriesClustering:
                UniClusterConfig uniClusterConfig = new(configuration);
                TaskHelpers.ValidateConfig(uniClusterConfig, analysis);
                if (analysis)
                    TestRoutines<TaskUniClustering, UniClusterConfig, ScenarioUnivariate, UniClusterDataset, int[][], ClusteringMetric>.RunAnalysis(uniClusterConfig, parameters);
                else
                    TestRoutines<TaskUniClustering, UniClusterConfig, ScenarioUnivariate, UniClusterDataset, int[][], ClusteringMetric>.ExecuteExperiment(uniClusterConfig);
                break;
                
            case Task.MultivariateTsClassification:
                MvClassConfig mvClassConfig = new(configuration);
                TaskHelpers.ValidateConfig(mvClassConfig, analysis);
                if (analysis)
                    TestRoutines<TaskMvClassification, MvClassConfig, ScenarioMultivariate, MultivarDataset, string[], ClassificationMetric>.RunAnalysis(mvClassConfig, parameters);
                else
                    TestRoutines<TaskMvClassification, MvClassConfig, ScenarioMultivariate, MultivarDataset, string[], ClassificationMetric>.ExecuteExperiment(mvClassConfig);
                break;
            
            default:
                throw new InvalidOperationException();
        }

        Console.WriteLine("Completion.");

        Console.WriteLine();
        Console.WriteLine("-=-=-=-=-=-=-=-=-=-");
        Console.WriteLine();
    }
    
    private static Dictionary<string, string> ReadConfigFile(string configFile)
    {
        Dictionary<string, string> dict = new();

        IEnumerable<string> lines = File.ReadAllLines(configFile)
            .Select(s => s.Trim())
            .WhereNOT(String.IsNullOrWhiteSpace)
            .Where(s => !s.StartsWith("#"));

        foreach (string line in lines)
        {
            string[] keyValue = line.Split('=');

            if (keyValue.Length < 2)
            {
                Console.WriteLine("Malformed config file: an entry isn't of the format Key=Value.");
                Environment.Exit(-1);
            }

            string key = keyValue[0].Trim().ToLower();
            string val = keyValue.Skip(1).StringJoin("=").Trim();
            
            if (dict.ContainsKey(key))
            {
                Console.WriteLine("Malformed config file: one of the fields is present twice.");
                Environment.Exit(-1);
            }
            dict.Add(key, val);
        }

        return dict;
    }
}

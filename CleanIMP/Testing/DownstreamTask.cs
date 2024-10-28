using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

using CleanIMP.Algorithms.Imputation;
using CleanIMP.Config;
using CleanIMP.Utilities;
using CleanIMP.Utilities.Mathematical;

namespace CleanIMP.Testing;

//
// This is the individual portion of IO, where task-specific IO is used
// Base class and all subclasses (below)
//

/// <summary>
/// Base interface for downstream task.
/// It's the main description of task-specific behaviors and constraints related to working with data and handling the specific components of experiments.
/// </summary>
/// <remarks>
/// Functionally it should be an abstract class, however implementation constraints (related to 'static abstract' keyword) force it to be an interface.
/// </remarks>
/// <typeparam name="TTask">Self-referential type implementing this interface</typeparam>
/// <typeparam name="TConfig">Type for experiment configuration for a given task.</typeparam>
/// <typeparam name="TScenario">Scenario types used by the task for the experiments.</typeparam>
/// <typeparam name="TData">Type for the dataset container.</typeparam>
/// <typeparam name="TDown">Type for the container of downstream results.</typeparam>
public interface IDownstreamTask<TTask, in TConfig, in TScenario, TData, TDown>
    where TTask : IDownstreamTask<TTask, TConfig, TScenario, TData, TDown>
    where TConfig : TaskConfig<TScenario>
    where TScenario : IScenario<TScenario>
    where TData : IDataset<TData, TConfig, TScenario, TDown>
{
    // Abstract
    static abstract TData LoadData(string data, TConfig config);
    
    static abstract Dictionary<string, Dictionary<int, TData>> LoadDecontaminatedData(TData data, string location, int[] ticks, ImmutableList<Algorithm> algorithms);
    
    static abstract Dictionary<string, Dictionary<int, TDown?>> LoadDownstreamResults(string dataPath, TScenario scen, int[] ticks, ImmutableList<Algorithm> algorithms, string downAlgo);

    static abstract TDown? LoadReference(string dataPath, string downAlgo);

    static abstract void WriteContamination(string location, TData data);
    
    static abstract void WriteDownstream(string path, TDown result);
    
    // Implementation
    public static long? LoadReferenceRt(string dataPath, string downAlgo)
    {
        string referenceRuntimeLocation = TestIOHelpers.ReferenceResultLocation(dataPath, downAlgo) + ".runtime";
        
        if (!File.Exists(referenceRuntimeLocation)) return null;
        
        return TestIOHelpers.LoadRuntimeFile(referenceRuntimeLocation);
    }
}

public sealed class TaskUniClassification : IDownstreamTask<TaskUniClassification, UniClassConfig, ScenarioUnivariate, UnivarDataset, string[]>
{
    public static UnivarDataset LoadData(string data, UniClassConfig config)
    {
        string trainFile = $"{config.DataSourcePath(data)}{data}_TRAIN.ts";
        string testFile = $"{config.DataSourcePath(data)}{data}_TEST.ts";

        if (!File.Exists(trainFile) || !File.Exists(testFile))
        {
            Console.WriteLine($"File {trainFile} or {testFile} doesn't exist");
            Console.WriteLine($"Data = {data}");
            Console.WriteLine("Operation aborted since one or more pieces of data for the test isn't present");
            Console.WriteLine("Make sure tha data is suitable for the task (classification)");
            throw new InvalidOperationException("No data file found in the expect location.");
        }

        (List<string> headers, List<UnivarSeries> trainSeries) = TestIOHelpers.LoadUniClassDataFileExtended(trainFile);
        List<UnivarSeries> testSeries = TestIOHelpers.LoadUniClassDataFile(testFile);

        // different lengths
        int lengths = trainSeries.Concat(testSeries).Select(x => x.Vector.Count).Distinct().Count();
        if (lengths > 1)
        {
            Console.WriteLine("Dataset error - data contains time series of different lengths. This type of data is not supported by the benchmark.");
            throw new InvalidOperationException("Datasets with different time series lengths are not supported");
        }
        
        // ts count
        if (trainSeries.Count < 20 || testSeries.Count < 20)
        {
            Console.WriteLine("Dataset error - can't have less than 20 time series.");
            throw new InvalidOperationException("Datasets with less than 20 time series are not supported");
        }

        int minClass = trainSeries.GroupBy(x => x.TrueClass).Select(x => x.Count()).Min();
        int minClassTest = testSeries.GroupBy(x => x.TrueClass).Select(x => x.Count()).Min();

        if (minClass < 3 || minClassTest < 3)
        {
            Console.WriteLine("Can't impute dataset by class - at least one class contains less than 3 time series.");
            throw new InvalidOperationException("Not enough time series in a class for imputation.");
        }
        
        UnivarDataset dataset = new(data, headers, trainSeries, testSeries);
        
        if (config.PerformNormalization)
        {
            foreach (UnivarSeries series in dataset.Train)
            {
                (double mean, double stddev) = MathX.BatchMeanStddev(series.Vector);
                MathX.NormalizeVector(ref series.Vector, mean, stddev);
            }
            
            foreach (UnivarSeries series in dataset.Test)
            {
                (double mean, double stddev) = MathX.BatchMeanStddev(series.Vector);
                MathX.NormalizeVector(ref series.Vector, mean, stddev);
            }
        }

        if (config.EnableTestSubSample) // injection of subsampling process
        {
            Random rndSubSample = new(config.SeedTestSubSample);
            int subSampleLength = Math.Min(Math.Max(dataset.Test.Count / 2, 50), dataset.Test.Count);
            dataset = new UnivarDataset(data, dataset.Headers, dataset.Train, rndSubSample.RandomSample(dataset.Test, subSampleLength).ToList());
        }

        return dataset;
    }

    public static Dictionary<string, Dictionary<int, UnivarDataset>> LoadDecontaminatedData(UnivarDataset data, string location, int[] ticks, ImmutableList<Algorithm> algorithms)
    {
        Dictionary<string, Dictionary<int, UnivarDataset>> algorithmRecoveries = new();

        foreach (Algorithm alg in algorithms)
        {
            algorithmRecoveries.Add(alg.AlgCode, new Dictionary<int, UnivarDataset>());

            foreach (int tick in ticks)
            {
                string trainFile = $"{TestIOHelpers.ContaminatedLocationInstance(location, tick, alg)}{data.Data}_TRAIN.ts";
                string testFile = $"{TestIOHelpers.ContaminatedLocationInstance(location, tick, alg)}{data.Data}_TEST.ts";

                if (!File.Exists(trainFile))
                {
                    Console.WriteLine($"File {trainFile} doesn't exist");
                    Console.WriteLine($"Data = {data.Data}; Scenario Tick = {tick}; Algorithm = {alg.AlgCode}");
                    Console.WriteLine("Operation aborted since one or more pieces of contaminated data requested for the test isn't cached");
                    throw new InvalidOperationException("Contaminated data isn't cached.");
                }

                UnivarDataset dataset = new(data.Data, data.Headers, TestIOHelpers.LoadUniClassDataFile(trainFile), TestIOHelpers.LoadUniClassDataFile(testFile));

                algorithmRecoveries[alg.AlgCode].Add(tick, dataset);
            }
        }

        return algorithmRecoveries;
    }

    public static Dictionary<string, Dictionary<int, string[]?>> LoadDownstreamResults(string dataPath, ScenarioUnivariate scen, int[] ticks, ImmutableList<Algorithm> algorithms, string downAlgo)
    {
        Dictionary<string, Dictionary<int, string[]?>> algorithmClassifications = new();

        foreach (Algorithm alg in algorithms)
        {
            algorithmClassifications.Add(alg.AlgCode, new Dictionary<int, string[]?>());

            foreach (int tick in ticks)
            {
                string file = $"{TestIOHelpers.ResultLocation(dataPath, scen.ToString(), tick, alg)}{downAlgo}.txt";

                if (!File.Exists(file))
                {
                    algorithmClassifications[alg.AlgCode].Add(tick, null);
                    continue;
                }
                
                algorithmClassifications[alg.AlgCode].Add(tick, TestIOHelpers.LoadClasses(file));
            }
        }

        return algorithmClassifications;
    }

    public static string[]? LoadReference(string dataPath, string downAlgo)
    {
        string referenceResultLocation = TestIOHelpers.ReferenceResultLocation(dataPath, downAlgo) + ".txt";
        
        if (!File.Exists(referenceResultLocation)) return null;
        
        return TestIOHelpers.LoadClasses(referenceResultLocation);
    }

    public static void WriteContamination(string location, UnivarDataset data)
    {
        string trainFile = $"{location}{data.Data}_TRAIN.ts";
        string testFile = $"{location}{data.Data}_TEST.ts";

        data.Headers.Concat(data.Train.Select(x => x.ToSkTimeLine())).FileWriteAllLines(trainFile);
        data.Headers.Concat(data.Test.Select(x => x.ToSkTimeLine())).FileWriteAllLines(testFile);
    }
    
    public static void WriteDownstream(string path, string[] result)
    {
        TestIOHelpers.DumpClasses(result, path);
    }
}

public sealed class TaskForecasting : IDownstreamTask<TaskForecasting, ForecastConfig, ScenarioMultivariate, ForecastDataset, Vector<double>>
{
    public static ForecastDataset LoadData(string data, ForecastConfig config)
    {
        string matrixFile = $"{config.DataSourcePath(data)}{data}_matrix.txt";
        string seasonFile = $"{config.DataSourcePath(data)}season.txt";

        Matrix<double> mat = MathX.LoadMatrixFile(matrixFile);

        if (!File.Exists(seasonFile))
        {
            Console.WriteLine("Dataset error - no seasonality file is found in the dataset folder.");
            throw new InvalidOperationException("Seasonality file is absent from the dataset folder");
        }

        int season = Int32.Parse(File.ReadAllText(seasonFile).Trim());

        if (mat.RowCount < 200)
        {
            Console.WriteLine("Dataset error - time series are shorter than 200 time points. This type of data is not supported by the benchmark.");
            throw new InvalidOperationException("Datasets with less than 100 time points are not supported");
        }

        int forecastRows = (mat.RowCount * config.ForecastWindow) / 100; // 10%
        forecastRows = Math.Max(forecastRows, 100); // FHs up to 96 need to be supported
        int baseRows = mat.RowCount - forecastRows;

        if (config.PerformNormalization)
        {
            (double[] mean, double[] stddev) = MathX.BatchMeanStddev(mat);
            MathX.NormalizeMatrix(ref mat, mean, stddev);
        }

        return new ForecastDataset(
            data,
            mat.SubMatrix(0, baseRows /*[0, br[*/, 0, mat.ColumnCount),
            mat.Column(0).SubVector(baseRows, forecastRows /*[br, br+pr[*/),
            season
        );
    }

    public static Dictionary<string, Dictionary<int, ForecastDataset>> LoadDecontaminatedData(ForecastDataset data, string location, int[] ticks, ImmutableList<Algorithm> algorithms)
    {
        Dictionary<string, Dictionary<int, ForecastDataset>> algorithmRecoveries = new();

        foreach (Algorithm alg in algorithms)
        {
            algorithmRecoveries.Add(alg.AlgCode, new Dictionary<int, ForecastDataset>());

            foreach (int tick in ticks)
            {
                string decFile = $"{TestIOHelpers.ContaminatedLocationInstance(location, tick, alg)}{data.Data}_DEC.txt";

                if (!File.Exists(decFile))
                {
                    Console.WriteLine($"File {decFile} doesn't exist");
                    Console.WriteLine($"Data = {data.Data}; Scenario Tick = {tick}; Algorithm = {alg.AlgCode}");
                    Console.WriteLine("Operated aborted since one or more pieces of contaminated data requested for the test isn't cached");
                    throw new InvalidOperationException("Contaminated data isn't cached.");
                }

                Matrix<double> dataset = MathX.LoadMatrixFile(decFile);

                ForecastDataset decontaminated = new(data.Data, dataset, data.Forecast, data.Season);

                algorithmRecoveries[alg.AlgCode].Add(tick, decontaminated);
            }
        }

        return algorithmRecoveries;
    }

    public static Dictionary<string, Dictionary<int, Vector<double>?>> LoadDownstreamResults(string dataPath,
        ScenarioMultivariate scen, int[] ticks, ImmutableList<Algorithm> algorithms, string downAlgo)
    {
        Dictionary<string, Dictionary<int, Vector<double>?>> algorithmForecasts = new();

        foreach (Algorithm alg in algorithms)
        {
            algorithmForecasts.Add(alg.AlgCode, new Dictionary<int, Vector<double>?>());

            foreach (int tick in ticks)
            {
                string file = $"{TestIOHelpers.ResultLocation(dataPath, scen.ToString(), tick, alg)}{downAlgo}.txt";
                
                if (!File.Exists(file))
                {
                    algorithmForecasts[alg.AlgCode].Add(tick, null);
                    continue;
                }
                
                algorithmForecasts[alg.AlgCode].Add(tick, MathX.LoadVectorFile(file));
            }
        }

        return algorithmForecasts;
    }

    public static Vector<double>? LoadReference(string dataPath, string downAlgo)
    {
        string referenceResultLocation = TestIOHelpers.ReferenceResultLocation(dataPath, downAlgo) + ".txt";
        
        if (!File.Exists(referenceResultLocation)) return null;
        
        return MathX.LoadVectorFile(TestIOHelpers.ReferenceResultLocation(dataPath, downAlgo) + ".txt");
    }

    public static void WriteContamination(string location, ForecastDataset data)
    {
        data.Train.ExportMx().FileWriteAllLines($"{location}{data.Data}_DEC.txt");
    }

    public static void WriteDownstream(string path, Vector<double> result)
    {
        result.ExportVec().FileWriteAllLines(path);
    }
}

public sealed class TaskUniClustering : IDownstreamTask<TaskUniClustering, UniClusterConfig, ScenarioUnivariate, UniClusterDataset, int[][]>
{
    public static UniClusterDataset LoadData(string data, UniClusterConfig config)
    {
        string trainFile = $"{config.DataSourcePath(data)}{data}_TRAIN.ts";
        string testFile = $"{config.DataSourcePath(data)}{data}_TEST.ts";

        if (!File.Exists(trainFile) || !File.Exists(testFile))
        {
            Console.WriteLine($"File {trainFile} or {testFile} doesn't exist");
            Console.WriteLine($"Data = {data}");
            Console.WriteLine("Operation aborted since one or more pieces of data for the test isn't present");
            Console.WriteLine("Make sure tha data is suitable for the task (clustering)");
            throw new InvalidOperationException("No data file found in the expect location.");
        }

        (List<string> headers, List<UnivarSeries> trainSeries) = TestIOHelpers.LoadUniClassDataFileExtended(trainFile);
        List<UnivarSeries> testSeries = TestIOHelpers.LoadUniClassDataFile(testFile);

        UnivarDataset ds = new(data, headers, trainSeries, testSeries);
        UniClusterDataset dataset = new(ds);

        // different lengths
        int lengths = dataset.Series.Select(x => x.Count).Distinct().Count();
        if (lengths > 1)
        {
            Console.WriteLine("Dataset error - data contains time series of different lengths. This type of data is not supported by the benchmark.");
            throw new InvalidOperationException("Datasets with different time series lengths are not supported");
        }
        
        // ts count
        if (dataset.Series.Length < 20)
        {
            Console.WriteLine("Dataset error - can't have less than 20 time series.");
            throw new InvalidOperationException("Datasets with less than 20 time series are not supported");
        }
        
        if (config.PerformNormalization)
        {
            for (int i = 0; i < dataset.Series.Length; i++)
            {
                (double mean, double stddev) = MathX.BatchMeanStddev(dataset.Series[i]);
                MathX.NormalizeVector(ref dataset.Series[i], mean, stddev);
            }
        }

        return dataset;
    }

    public static Dictionary<string, Dictionary<int, UniClusterDataset>> LoadDecontaminatedData(UniClusterDataset data, string location, int[] ticks, ImmutableList<Algorithm> algorithms)
    {
        Dictionary<string, Dictionary<int, UniClusterDataset>> algorithmRecoveries = new();

        foreach (Algorithm alg in algorithms)
        {
            algorithmRecoveries.Add(alg.AlgCode, new Dictionary<int, UniClusterDataset>());

            foreach (int tick in ticks)
            {
                string decFile = $"{TestIOHelpers.ContaminatedLocationInstance(location, tick, alg)}{data.Data}_DEC.ts";

                if (!File.Exists(decFile))
                {
                    Console.WriteLine($"File {decFile} doesn't exist");
                    Console.WriteLine($"Data = {data.Data}; Tick = {tick}; Algorithm = {alg.AlgCode}");
                    Console.WriteLine("Operated aborted since one or more pieces of contaminated data requested for the test isn't cached");
                    throw new InvalidOperationException("Contaminated data isn't cached.");
                }

                Vector<double>[] dataset = LoadUniClusterDataFile(decFile).ToArray();

                algorithmRecoveries[alg.AlgCode].Add(tick, new UniClusterDataset(data.Data, data.Headers, dataset.ToArray(), data.ClassCount));
            }
        }

        return algorithmRecoveries;
    }

    private static List<Vector<double>> LoadUniClusterDataFile(string file)
    {
        List<Vector<double>> series = new();

        foreach (var line in IOTools.EnumerateAllLines(file))
        {
            if (String.IsNullOrEmpty(line))
                continue;

            if (!line.StartsWith("%") && !line.StartsWith("#") && !line.StartsWith("@"))
            {
                series.Add(UniClusterDataset.FromSktimeNoClass(line));
            }
        }

        return series;
    }

    public static Dictionary<string, Dictionary<int, int[][]?>> LoadDownstreamResults(string dataPath,
        ScenarioUnivariate scen, int[] ticks, ImmutableList<Algorithm> algorithms, string downAlgo)
    {
        Dictionary<string, Dictionary<int, int[][]?>> clusteringResults = new();

        foreach (Algorithm alg in algorithms)
        {
            clusteringResults.Add(alg.AlgCode, new Dictionary<int, int[][]?>());

            foreach (int tick in ticks)
            {
                string location = $"{TestIOHelpers.ResultLocation(dataPath, scen.ToString(), tick, alg)}{downAlgo}.txt";
                
                if (!File.Exists(location))
                {
                    clusteringResults[alg.AlgCode].Add(tick, null);
                    continue;
                }
                
                clusteringResults[alg.AlgCode].Add(tick, TestIOHelpers.LoadClasses(location).Select(MathX.Parse.ParseRowInt).ToArray());
            }
        }

        return clusteringResults;
    }

    public static int[][]? LoadReference(string dataPath, string downAlgo)
    {
        string referenceResultLocation = TestIOHelpers.ReferenceResultLocation(dataPath, downAlgo) + ".txt";
        
        if (!File.Exists(referenceResultLocation)) return null;
        
        return TestIOHelpers.LoadClasses(TestIOHelpers.ReferenceResultLocation(dataPath, downAlgo) + ".txt")
            .Select(MathX.Parse.ParseRowInt)
            .ToArray();
    }

    public static void WriteContamination(string location, UniClusterDataset data)
    {
        string decFile = $"{location}{data.Data}_DEC.ts";
        data.ToSkTimeLine().FileWriteAllLines(decFile);
    }

    public static void WriteDownstream(string path, int[][] result)
    {
        TestIOHelpers.DumpClasses(result.Select(row => row.StringJoin(" ")), path);
    }
}

public sealed class TaskMvClassification : IDownstreamTask<TaskMvClassification, MvClassConfig, ScenarioMultivariate, MultivarDataset, string[]>
{
    public static MultivarDataset LoadData(string data, MvClassConfig config)
    {
        string trainFile = $"{config.DataSourcePath(data)}{data}_TRAIN.ts";
        string testFile = $"{config.DataSourcePath(data)}{data}_TEST.ts";

        if (!File.Exists(trainFile) || !File.Exists(testFile))
        {
            Console.WriteLine($"File {trainFile} or {testFile} doesn't exist");
            Console.WriteLine($"Data = {data}");
            Console.WriteLine("Operation aborted since one or more pieces of data for the test isn't present");
            Console.WriteLine("Make sure tha data is suitable for the task (classification)");
            throw new InvalidOperationException("No data file found in the expect location.");
        }

        List<string> headers = new(); //read only once from train file
        List<MultivarSeries> trainSeries = new();

        foreach (var line in IOTools.EnumerateAllLines(trainFile))
        {
            if (String.IsNullOrEmpty(line))
                continue;

            if (!line.StartsWith("%") && !line.StartsWith("#") && !line.StartsWith("@"))
            {
                trainSeries.Add(MultivarSeries.FromSktime(line));
            }
            else
            {
                // header/metadata
                headers.Add(line);
            }
        }

        List<MultivarSeries> testSeries = LoadMvClassDataFile(testFile);
        
        MultivarDataset dataset = new(data, headers, trainSeries, testSeries);

        if (config.PerformNormalization)
        {
            foreach (MultivarSeries series in dataset.Train)
            {
                (double[] mean, double[] stddev) = MathX.BatchMeanStddev(series.Matrix);
                MathX.NormalizeMatrix(ref series.Matrix, mean, stddev);
            }
            
            foreach (MultivarSeries series in dataset.Test)
            {
                (double[] mean, double[] stddev) = MathX.BatchMeanStddev(series.Matrix);
                MathX.NormalizeMatrix(ref series.Matrix, mean, stddev);
            }
        }

        if (config.EnableTestSubSample) // injection of subsampling process
        {
            Random rndSubSample = new(config.SeedTestSubSample);
            int subSampleLength = Math.Min(Math.Max(dataset.Test.Count / 2, 50), dataset.Test.Count);
            dataset = new MultivarDataset(data, headers, dataset.Train, rndSubSample.RandomSample(dataset.Test, subSampleLength).ToList());
        }

        return dataset;
    }

    private static List<MultivarSeries> LoadMvClassDataFile(string file)
    {
        List<MultivarSeries> series = new();

        foreach (var line in IOTools.EnumerateAllLines(file))
        {
            if (String.IsNullOrEmpty(line))
                continue;

            if (!line.StartsWith("%") && !line.StartsWith("#") && !line.StartsWith("@"))
            {
                series.Add(MultivarSeries.FromSktime(line));
            }
        }

        return series;
    }

    public static Dictionary<string, Dictionary<int, MultivarDataset>> LoadDecontaminatedData(MultivarDataset data, string location, int[] ticks, ImmutableList<Algorithm> algorithms)
    {
        Dictionary<string, Dictionary<int, MultivarDataset>> algorithmRecoveries = new();

        foreach (Algorithm alg in algorithms)
        {
            algorithmRecoveries.Add(alg.AlgCode, new Dictionary<int, MultivarDataset>());

            foreach (int tick in ticks)
            {
                string trainFile = $"{TestIOHelpers.ContaminatedLocationInstance(location, tick, alg)}{data.Data}_TRAIN.ts";
                string testFile = $"{TestIOHelpers.ContaminatedLocationInstance(location, tick, alg)}{data.Data}_TEST.ts";

                if (!File.Exists(trainFile))
                {
                    Console.WriteLine($"File {trainFile} doesn't exist");
                    Console.WriteLine($"Data = {data.Data}; Tick = {tick}; Algorithm = {alg.AlgCode}");
                    Console.WriteLine("Operation aborted since one or more pieces of contaminated data requested for the test isn't cached");
                    throw new InvalidOperationException("Contaminated data isn't cached.");
                }

                MultivarDataset dataset = new(data.Data, data.Headers, LoadMvClassDataFile(trainFile), LoadMvClassDataFile(testFile));

                algorithmRecoveries[alg.AlgCode].Add(tick, dataset);
            }
        }

        return algorithmRecoveries;
    }

    public static Dictionary<string, Dictionary<int, string[]?>> LoadDownstreamResults(string dataPath,
        ScenarioMultivariate scen, int[] ticks,
        ImmutableList<Algorithm> algorithms, string downAlgo)
    {
        Dictionary<string, Dictionary<int, string[]?>> algorithmClassifications = new();

        foreach (Algorithm alg in algorithms)
        {
            algorithmClassifications.Add(alg.AlgCode, new Dictionary<int, string[]?>());

            foreach (int tick in ticks)
            {
                string location = $"{TestIOHelpers.ResultLocation(dataPath, scen.ToString(), tick, alg)}{downAlgo}.txt";
                
                if (!File.Exists(location))
                {
                    algorithmClassifications[alg.AlgCode].Add(tick, null);
                    continue;
                }
                
                algorithmClassifications[alg.AlgCode].Add(tick, TestIOHelpers.LoadClasses(location));
            }
        }

        return algorithmClassifications;
    }

    public static string[]? LoadReference(string dataPath, string downAlgo)
    {
        string referenceResultLocation = TestIOHelpers.ReferenceResultLocation(dataPath, downAlgo) + ".txt";
        
        if (!File.Exists(referenceResultLocation)) return null;
        
        return TestIOHelpers.LoadClasses(referenceResultLocation);
    }

    public static void WriteContamination(string location, MultivarDataset data)
    {
        string trainFile = $"{location}{data.Data}_TRAIN.ts";
        string testFile = $"{location}{data.Data}_TEST.ts";

        data.Headers.Concat(data.Train.Select(x => x.ToSkTimeLine())).FileWriteAllLines(trainFile);
        data.Headers.Concat(data.Test.Select(x => x.ToSkTimeLine())).FileWriteAllLines(testFile);
    }

    public static void WriteDownstream(string path, string[] result)
    {
        TestIOHelpers.DumpClasses(result, path);
    }
}
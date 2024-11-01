using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CleanIMP.Algorithms.Analysis;
using CleanIMP.Algorithms.Imputation;
using CleanIMP.Config;
using CleanIMP.Utilities;

namespace CleanIMP.Testing;

//
// This is a shared portion of IO, valid across multiple tasks
//

// [!] partial class
// see TestRoutines.cs for this type's documentation
public static partial class TestRoutines<TTask, TConfig, TScenario, TData, TDown, TMetric>
    where TTask : IDownstreamTask<TTask, TConfig, TScenario, TData, TDown>
    where TConfig : TaskConfig<TScenario>
    where TScenario : IScenario<TScenario>
    where TData : IDataset<TData, TConfig, TScenario, TDown>
    where TMetric : IMetric<TMetric, TDown>
{
    /// <summary>
    /// A static class with the collection of IO functionality that is reusable by all downstream tasks.
    /// Depends on the generic types of the container class.
    /// </summary>
    public static class TestIO
    {
        // Paths
        public static string ContaminatedLocation(TConfig config, string data, TScenario scen)
            => $"{config.DataWorkPath(data)}decontaminated/{scen}/";
        public static string ContaminatedLocation(TConfig config, string data, TScenario scen, int tick, Algorithm alg)
            => $"{config.DataWorkPath(data)}decontaminated/{scen}/{tick}/{alg.AlgCode}_";
        public static string ReferenceResultLocation(string dataPath, string downstreamAlgo)
            => $"{dataPath}reference/{downstreamAlgo}";
        
        // Actions
        
        public static void CreateContaminatedLocation(TConfig config, string data, TScenario scen, int tick)
        {
            Directory.CreateDirectory($"{config.DataWorkPath(data)}decontaminated/");
            Directory.CreateDirectory($"{config.DataWorkPath(data)}decontaminated/{scen}/");
            Directory.CreateDirectory($"{config.DataWorkPath(data)}decontaminated/{scen}/{tick}/");
        }

        public static void CreateResultLocation(TConfig config, string data, TScenario scen, int tick, Algorithm alg)
        {
            Directory.CreateDirectory($"{config.DataWorkPath(data)}results/");
            Directory.CreateDirectory($"{config.DataWorkPath(data)}results/{scen}/");
            Directory.CreateDirectory($"{config.DataWorkPath(data)}results/{scen}/{tick}/");
            Directory.CreateDirectory($"{config.DataWorkPath(data)}results/{scen}/{tick}/{alg.AlgCode}/");
        }

        // Assist
        
        public static bool HasDownstreamReference(string dataPath, string algorithm)
            => File.Exists(ReferenceResultLocation(dataPath, algorithm) + ".txt")
               && File.Exists(ReferenceResultLocation(dataPath, algorithm) + ".runtime");
    }
}

/// <summary>
/// A static class with the collection of IO functionality that is reusable by some downstream tasks.
/// Does not depend on the generic types related to experiments, but can have those explicitly instead.
/// </summary>
public static class TestIOHelpers
{
    public static string ContaminatedLocationInstance(string location, int tick, Algorithm alg)
        => $"{location}{tick}/{alg.AlgCode}_";
    
    public static List<UnivarSeries> LoadUniClassDataFile(string file)
    {
        List<UnivarSeries> series = new();

        foreach (var line in IOTools.EnumerateAllLines(file))
        {
            if (String.IsNullOrEmpty(line) || line.StartsWith("%") || line.StartsWith("#") || line.StartsWith("@"))
                continue;

            series.Add(UnivarSeries.FromSktime(line));
        }

        return series;
    }

    public static (List<string>, List<UnivarSeries>) LoadUniClassDataFileExtended(string file)
    {
        List<string> headers = new();
        List<UnivarSeries> series = new();
        
        foreach (var line in IOTools.EnumerateAllLines(file))
        {
            if (String.IsNullOrEmpty(line))
                continue;

            if (!line.StartsWith("%") && !line.StartsWith("#") && !line.StartsWith("@"))
            {
                series.Add(UnivarSeries.FromSktime(line));
            }
            else
            {
                // header/metadata
                headers.Add(line);
            }
        }

        return (headers, series);
    }
    
    public static string[] LoadClasses(string path)
        => IOTools.EnumerateAllLines(path).WhereNOT(String.IsNullOrEmpty).ToArray();
    
    public static void DumpClasses(IEnumerable<string> classes, string path)
        => IOTools.FileWriteAllLines(path, classes);
    
    public static void DumpRuntime(long runtime, string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        IOTools.FileWriteAllText(path, runtime.ToString());
    }

    public static long LoadRuntimeFile(string filePath) => Int64.Parse(File.ReadAllText(filePath).Trim());

    public static string ReferenceResultLocation(string dataPath, string downstreamAlgo)
        => $"{dataPath}reference/{downstreamAlgo}";
    
    public static string ResultLocation(string dataPath, string scen, int tick, Algorithm alg)
        => $"{dataPath}results/{scen}/{tick}/{alg.AlgCode}/";
}

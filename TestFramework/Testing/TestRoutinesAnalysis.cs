using System;
using System.Collections.Generic;
using System.Linq;
using CleanIMP.Algorithms.Analysis;
using CleanIMP.Config;
using CleanIMP.Utilities.Mathematical;
using CleanIMP.Utilities;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Testing;

// This file contains only the methods of partial class TestRoutines<> that are related to analysis.
// This is the place to add new analysis routines - upstream, downstream or analysis of datasets independent of experiments.

// To understand what happens, the entry points are RunUpstreamAnalysis() and RunDownstreamAnalysis() function for resp. type of analysis.
// Their structure is very similar, however it can not be identical due to logistical issues (reliance on predictable UpstreamMetric vs unpredictable TMetric for downstream).
// They rely on LoadUpstreamTransform() and LoadDownstreamTransform() functions to load the data and apply the necessary metric(s).
// However, those two functions return a type with an identical structure.
// So when it comes to processing and aggregating the results they use the same function RunInstance().
// Most variations of how the analysis is performed (and how output is structured) are built on top the AggregationType enum.
// Thus, all of those are applicable to all tasks, both upstream and downstream, it includes LaTeX-output for charts, so e.g. output for tables is easy to add.
// New outputs are analogous to the existing LaTeX one, just create another enum and handle it in a switch in RunInstance()
// Reference-related analytics are much more streamlined since there's comparatively very little data (no scenarios and upstream).

// [!] partial class
// see TestRoutines.cs for this type's documentation
public static partial class TestRoutines<TTask, TConfig, TScenario, TData, TDown, TMetric>
    where TTask : IDownstreamTask<TTask, TConfig, TScenario, TData, TDown>
    where TConfig : TaskConfig<TScenario>
    where TScenario : IScenario<TScenario>
    where TData : IDataset<TData, TConfig, TScenario, TDown>
    where TMetric : IMetric<TMetric, TDown>
{
    public static void RunAnalysis(TConfig config, string[] parameters)
    {
        if (parameters.Length < 2)
        {
            Console.WriteLine("Incorrect format of analysis parameters, expected at least two arguments.");
            Console.WriteLine("Format: job_to_analyse:metric1,metric2:aggregate_method");
            Console.WriteLine("of types str:str[]:bool");
            return;
        }
        
        // metric validation happens inside the resp. algorithm analysis routine
        string job = parameters[0].ToLower();
        string[] metrics = parameters[1].Split(',');
        // ReSharper disable once SimplifyConditionalTernaryExpression
        AggregationType aggregate = GetAggregation(parameters.Length > 2 ? parameters[2] : "all");

        if (job == "upstream")
        {
            RunUpstreamAnalysis(config, metrics, aggregate);
        }
        else if (job == "downstream")
        {
            RunDownstreamAnalysis(config, metrics, aggregate);
        }
        else if (job == "allticks")
        {
            RunDownstreamAllTicks(config, metrics);
        }
        else if (job == "bydata")
        {
            RunDownstreamByData(config, metrics, aggregate);
        }
        else if (job == "reference")
        {
            RunDownstreamReference(config, metrics);
        }
        else if (job == "referencert")
        {
            RunDownstreamReferenceRt(config);
        }
        else if (job == "datadump")
        {
            RunDataDump(config, metrics.First());
        }
        else if (job == "simpledump")
        {
            RunDataDumpSimple(config, metrics.First());
        }
        else if (job == "datachar")
        {
            RunDataChar(config);
        }
        else
        {
            Console.WriteLine("Unknown job name to analyse.");
        }
    }
    
    //
    // Jobs: upstream & downstream
    //
    
    private static void RunUpstreamAnalysis(TConfig config, string[] metrics, AggregationType aggrType)
    {
        foreach (string metricStr in metrics)
        {
            UpstreamMetric metric = metricStr.ToLower() == "default" ? UpstreamMetric.Default : UpstreamMetric.GetMetricByName(metricStr);
            Console.WriteLine($"% Metric = {metric.MeasureName}");
            Console.WriteLine();

            string allAlgsStr = config.Algorithms.Select(alg => alg.AlgCode).StringJoin(",");
            if (IsAggregated(aggrType))
                Console.WriteLine("data,reference," + config.Scenarios.Select(x => x + "," + allAlgsStr).StringJoin(","));
            
            foreach (string data in config.Datasets)
            {
                TData dataset = TTask.LoadData(data, config);
                
                if (IsAggregated(aggrType))
                    Console.Write($"{data},");
                
                //Console.WriteLine();
                for (int i = 0; i < config.Scenarios.Count; i++)
                {
                    TScenario scen = config.Scenarios[i];
                    Dictionary<string, Dictionary<int, double>> transform = LoadUpstreamTransform(config, dataset, scen, metric);
                    RunInstance(transform, dataset, scen, metric.MeasureName, aggrType);
                    if (i != config.Scenarios.Count - 1 && IsAggregated(aggrType)) Console.Write(",");
                }

                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
    
    private static void RunDownstreamAnalysis(TConfig config, string[] metrics, AggregationType aggrType)
    {
        foreach (string downAlgo in config.DownstreamAlgorithms)
        {
            foreach (string metricStr in metrics)
            {
                TMetric metric = metricStr.ToLower() == "default" ? TMetric.Default : TMetric.GetMetricByName(metricStr);
                Console.WriteLine($"% Metric = {metric.MeasureName}; Downstream = {downAlgo}");
                Console.WriteLine();

                string allAlgsStr = config.Algorithms.Select(alg => alg.AlgCode).StringJoin(",");
                
                if (IsAggregated(aggrType))
                    Console.WriteLine("data,reference," + config.Scenarios.Select(x => x + "," + allAlgsStr).StringJoin(","));
                
                foreach (string data in config.Datasets)
                {
                    TData dataset = TTask.LoadData(data, config);
                    TDown? reference = TTask.LoadReference(config.DataWorkPath(data), downAlgo);
                    
                    double referenceMeasure = reference == null ? Double.NaN : metric.Measure(dataset.GetDownstream(), reference);
                    if (IsAggregated(aggrType))
                        Console.Write($"{data},{MaybeRound(referenceMeasure, aggrType)},");
                    
                    //Console.WriteLine();
                    for (int i = 0; i < config.Scenarios.Count; i++)
                    {
                        TScenario scen = config.Scenarios[i];
                        Dictionary<string, Dictionary<int, double>> transform = LoadDownstreamTransform(config, dataset, scen, metric, downAlgo);
                        RunInstance(transform, dataset, scen, metric.MeasureName, aggrType);
                        if (IsAggregated(aggrType) && i != config.Scenarios.Count - 1) Console.Write(",");
                    }

                    Console.WriteLine();
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
    
    private static void RunDownstreamByData(TConfig config, string[] metrics, AggregationType aggrType)
    {
        foreach (string metricStr in metrics)
        {
            TMetric metric = metricStr.ToLower() == "default" ? TMetric.Default : TMetric.GetMetricByName(metricStr);

            foreach (string data in config.Datasets)
            {
                Console.WriteLine($"% Metric = {metric.MeasureName}; Dataset = {data}");
                Console.WriteLine();

                string allAlgsStr = config.Algorithms.Select(alg => alg.AlgCode).StringJoin(",");
            
                if (IsAggregated(aggrType))
                {
                    Console.WriteLine("downstream,reference," + config.Scenarios.Select(x => x + "," + allAlgsStr).StringJoin(","));
                }
                else
                {
                    throw new ArgumentException("ByData analysis doesn't support non-aggregated grouping.");
                }
                
                TData dataset = TTask.LoadData(data, config);
            
                foreach (string downAlgo in config.DownstreamAlgorithms)
                {
                    TDown? reference = TTask.LoadReference(config.DataWorkPath(data), downAlgo);
                
                    double referenceMeasure = reference == null ? Double.NaN : metric.Measure(dataset.GetDownstream(), reference);
                    Console.Write($"{downAlgo},{MaybeRound(referenceMeasure, aggrType)},");
                
                    //Console.WriteLine();
                    for (int i = 0; i < config.Scenarios.Count; i++)
                    {
                        TScenario scen = config.Scenarios[i];
                        Dictionary<string, Dictionary<int, double>> transform = LoadDownstreamTransform(config, dataset, scen, metric, downAlgo);
                        RunInstance(transform, dataset, scen, metric.MeasureName, aggrType);
                        if (IsAggregated(aggrType) && i != config.Scenarios.Count - 1) Console.Write(",");
                    }

                    Console.WriteLine();
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
        Console.WriteLine();
    }
    
    private static void RunDownstreamAllTicks(TConfig config, string[] metrics)
    {
        foreach (string downAlgo in config.DownstreamAlgorithms)
        {
            foreach (string metricStr in metrics)
            {
                TMetric metric = metricStr.ToLower() == "default" ? TMetric.Default : TMetric.GetMetricByName(metricStr);
                Console.WriteLine($"% Metric = {metric.MeasureName}; Downstream = {downAlgo}");
                Console.WriteLine();
                
                for (int i = 0; i < config.Scenarios.Count; i++)
                {
                    TScenario scen = config.Scenarios[i];
                    List<int>? ticks = null;
                    Dictionary<string, Dictionary<int, List<double>>> byTickDict = new();
                    
                    foreach (string data in config.Datasets)
                    {
                        TData dataset = TTask.LoadData(data, config);

                        Dictionary<string, Dictionary<int, double>> transform = LoadDownstreamTransform(config, dataset, scen, metric, downAlgo);

                        ticks ??= scen.Ticks(dataset.TsLen(), dataset.TsCount()).ToList();

                        Dictionary<int, List<double>> results = ticks.ToDictionary(tick => tick, _ => new List<double>());

                        foreach (var kv in transform)
                        {
                            foreach (int tick in kv.Value.Keys)
                            {
                                results[tick].Add(kv.Value[tick]);
                            }
                        }
                        
                        byTickDict.Add(data, results);
                    }

                    if (ticks == null) throw new Exception("Unknown error (no datasets)");

                    foreach (int tick in ticks)
                    {
                        foreach ((string data, Dictionary<int, List<double>>? value) in byTickDict)
                        {
                            List<double> values = value[tick];
                            Console.WriteLine($"{tick},{data},{values.StringJoin(",")}");
                        }
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
    
    private static void RunInstance(Dictionary<string, Dictionary<int, double>> transform, TData dataset, TScenario scen, string metric, AggregationType aggrType)
    {
        int[] ticks = scen.Ticks(dataset.TsLen(), dataset.TsCount()).ToArray();
        
        if (IsAggregated(aggrType))
        {
            string line = $"{scen}";

            foreach (KeyValuePair<string,Dictionary<int,double>> kv in transform)
            {
                // this loop is related to imputation algorithms, hence TKey is string
                double avgId = kv.Value.Average(x => x.Key);
                int maxId = kv.Value.Max(x => x.Key);
                int minId = kv.Value.Min(x => x.Key);
                
                // we will use avgId (or not) depending on how we aggregate
                Func<KeyValuePair<int, double>, bool> predicate = kvp =>
                {
                    return aggrType switch
                    {
                        AggregationType.All => true,
                        AggregationType.AllRound => true,
                        AggregationType.MinimumContamination => kvp.Key == minId,
                        AggregationType.LowContamination => kvp.Key < avgId,
                        AggregationType.HighContamination => kvp.Key >= avgId,
                        AggregationType.MaximumContamination => kvp.Key == maxId,
                        _ => throw new ArgumentException("Invalid or unimplemented aggregation type")
                    };
                };

                //obsolete behaviour
                //double[] finite = kv.Value.Where(predicate).Select(x => x.Value).WhereNot(Double.IsNaN).ToArray();
                //double avg = finite.Length == 0 ? Double.NaN : finite.Average();
                
                double avg = kv.Value.Where(predicate).Select(x => x.Value).Average();
                line += $",{MaybeRound(avg, aggrType)}";
            }
            
            Console.Write(line);
        }
        else
        {
            // descriptor
            Console.WriteLine($"% Scenario = {scen}; Data = {dataset.Data}; Metric = {metric}");
            Console.WriteLine();

            if (aggrType == AggregationType.Latex)
            {
                foreach (KeyValuePair<string,Dictionary<int,double>> kv in transform)
                {
                    Console.WriteLine($"Algorithm = {kv.Key}");

                    foreach (KeyValuePair<int, double> kvpair in kv.Value)
                    {
                        Console.WriteLine($"({kvpair.Key}, {kvpair.Value})");
                    }
                    Console.WriteLine();
                }
            }
            else
            {
                // csv header
                Console.WriteLine("algorithm," + ticks.StringJoin(","));
            
                foreach (KeyValuePair<string,Dictionary<int,double>> kv in transform)
                {
                    string line = kv.Key + "," + kv.Value.Values.Select(d => MaybeRound(d, aggrType)).StringJoin(",");
                    Console.WriteLine(line);
                }
            }
            Console.WriteLine();
        }
    }

    private static Dictionary<string, Dictionary<int, double>> LoadUpstreamTransform(TConfig config, TData dataset, TScenario scen, UpstreamMetric metric)
    {
        int[] ticks = scen.Ticks(dataset.TsLen(), dataset.TsCount()).ToArray();
        
        // prepare multi-dimensions
        string location = TestIO.ContaminatedLocation(config, dataset.Data, scen);
        Dictionary<string, Dictionary<int, TData>> algorithmRecoveries = TTask.LoadDecontaminatedData(dataset, location, ticks, config.Algorithms);

        // this will create the same nested dictionary, except UnivarDataset will be replaced by double
        Dictionary<string, Dictionary<int, double>> transform = algorithmRecoveries.Select(dictAlgos =>
        {
            //.AsParallel().WithDegreeOfParallelism(Utils.ParallelExecutionNo(ticks.Length))
            return (dictAlgos.Key, dictAlgos.Value.Select(dictTicks =>
            {
                TData recovered = dictTicks.Value;
                MissingBlock[] blocks = scen.GetContamination(recovered.TsLen(), recovered.TsCount(), dictTicks.Key, config.McarSeed).ToArray();

                return (dictTicks.Key, metric.Measure(dataset.GetUpstream(config, blocks), recovered.GetUpstream(config, blocks)));
            }).ToDictionaryX());
        }).ToDictionary(x => x.Key, x=> x.Item2);

        return transform;
    }

    private static Dictionary<string, Dictionary<int, double>> LoadDownstreamTransform(TConfig config, TData dataset, TScenario scen, TMetric metric, string classifier)
    {
        int[] ticks = scen.Ticks(dataset.TsLen(), dataset.TsCount()).ToArray();
        
        // prepare multi-dimensions
        Dictionary<string, Dictionary<int, TDown?>> algorithmRecoveries = TTask.LoadDownstreamResults(config.DataWorkPath(dataset.Data), scen, ticks, config.Algorithms, classifier);

        // this will create the same nested dictionary, except UnivarDataset will be replaced by double
        Dictionary<string, Dictionary<int, double>> transform = algorithmRecoveries.Select(dictAlgos
            => (dictAlgos.Key,
                dictAlgos.Value.Select(dictTicks
                    => (dictTicks.Key, dictTicks.Value == null ? Double.NaN : metric.Measure(dataset.GetDownstream(), dictTicks.Value))
                ).ToDictionaryX()
        )).ToDictionary(x => x.Key, x=> x.Item2);
        //.AsParallel().WithDegreeOfParallelism(Utils.ParallelExecutionNo(ticks.Length))

        return transform;
    }
    
    //
    // Job: reference
    //

    private static void RunDownstreamReference(TConfig config, string[] metrics)
    {
        string header = "";
        bool first = true;
        List<string> values = new();
        
        foreach (string data in config.Datasets)
        {
            List<string> headers = new() {"dataset"};
            List<string> row = new() {data};
            
            TData dataset = TTask.LoadData(data, config);

            foreach (string downAlgo in config.DownstreamAlgorithms)
            {
                TDown? reference = TTask.LoadReference(config.DataWorkPath(data), downAlgo);
                
                foreach (string metricStr in metrics)
                {
                    TMetric metric = TMetric.GetMetricByName(metricStr);
                    headers.Add($"{downAlgo}-{metric.MeasureName}");
                    double res = reference == null ? Double.NaN : metric.Measure(dataset.GetDownstream(), reference);
                    row.Add($"{res}");
                }
            }

            if (first)
            {
                header = headers.StringJoin(",");
                first = false;
            }
            values.Add(row.StringJoin(","));
        }
        Console.WriteLine(header);

        foreach (string row in values)
        {
            Console.WriteLine(row);
        }
    }

    private static void RunDownstreamReferenceRt(TConfig config)
    {
        string header = "";
        bool first = true;
        List<string> values = new();
        
        foreach (string data in config.Datasets)
        {
            List<string> headers = new() {"dataset"};
            List<string> row = new() {data};

            foreach (string downAlgo in config.DownstreamAlgorithms)
            {
                long? reference = IDownstreamTask<TTask, TConfig, TScenario, TData, TDown>.LoadReferenceRt(config.DataWorkPath(data), downAlgo);
                headers.Add($"{downAlgo}-Runtime");
                double seconds = reference == null ? Double.NaN : reference.Value / (1000.0 * 1000.0);
                row.Add($"{seconds}");
            }

            if (first)
            {
                header = headers.StringJoin(",");
                first = false;
            }
            values.Add(row.StringJoin(","));
        }
        Console.WriteLine(header);

        foreach (string row in values)
        {
            Console.WriteLine(row);
        }
    }
    
    //
    // Jobs: Data dump/char
    //

    private static void RunDataDump(TConfig config, string metric)
    {
        foreach (string data in config.Datasets)
        {
            TData dataset = TTask.LoadData(data, config);
            (string, double)[] dump = dataset.BasicDump();
            (double[] corr, Vector<double>[] ts) = dataset.AdvancedDump(metric);

            Console.WriteLine($"% Data = {data}; Metric = {metric}");

            // basic dump
            
            Console.WriteLine(dump.Select(d => d.Item1).StringJoin(","));
            Console.WriteLine(dump.Select(d => d.Item2).StringJoin(","));
            Console.WriteLine();

            // correlation
            Console.WriteLine("TS#0 correlation vector:");
            Console.WriteLine(corr.Take(10).StringJoin(","));
            Console.WriteLine();
            
            // visuals
            int len = Math.Min(100, ts[0].Count);
            Console.WriteLine(ts[0].Take(len).StringJoin(","));
            Console.WriteLine(ts[1].Take(len).StringJoin(","));
            if (ts.Length > 2)
                Console.WriteLine(ts[2].Take(len).StringJoin(","));
            else
                Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            // for excel default chart size to fit
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();
        }
    }

    private static void RunDataDumpSimple(TConfig config, string metric)
    {
        foreach (string data in config.Datasets)
        {
            TData dataset = TTask.LoadData(data, config);
            (double[] _, Vector<double>[] ts) = dataset.AdvancedDump(metric, false);

            Console.WriteLine($"Data = {data}");
            
            // visuals
            foreach (Vector<double> ts_i in ts)
            {
                Console.WriteLine(ts_i.StringJoin(","));
            }
            
            Console.WriteLine();
        }
    }

    private static void RunDataChar(TConfig config)
    {
        bool first = true;
        foreach (string data in config.Datasets)
        {
            TData dataset = TTask.LoadData(data, config);

            // data char
            (string, double)[] dataFeatures = dataset.BasicDump().Union(dataset.Characterization()).ToArray();

            if (first)
            {
                Console.WriteLine("dataset," + dataFeatures.Select(f => f.Item1).StringJoin(","));
                first = false;
            }
            Console.WriteLine($"{data}," + dataFeatures.Select(f => f.Item2).StringJoin(","));
        }
    }
    
    private enum AggregationType
    {
        None, Latex, All,
        NoneRound, AllRound,
        MinimumContamination, LowContamination, HighContamination, MaximumContamination
    }

    private static AggregationType GetAggregation(string aggrType)
    {
        switch (aggrType.ToLower())
        {
            case "none":
            case "false":
                return AggregationType.None;
            
            case "latex":
                return AggregationType.Latex;
            
            case "all":
            case "true":
                return AggregationType.All;
            
            case "none-round":
                return AggregationType.NoneRound;
            
            case "all-round":
                return AggregationType.AllRound;
            
            case "min":
                return AggregationType.MinimumContamination;
            
            case "low":
                return AggregationType.LowContamination;
            
            case "high":
                return AggregationType.HighContamination;
            
            case "extreme":
            case "maximum":
                return AggregationType.MaximumContamination;
            
            default:
                throw new ArgumentException("Invalid or unimplemented aggregation type for analytics");
        }
    }

    private static bool IsAggregated(AggregationType aggrType) => aggrType != AggregationType.None && aggrType != AggregationType.Latex && aggrType != AggregationType.NoneRound;

    private static bool IsRounded(AggregationType aggrType) => aggrType == AggregationType.NoneRound || aggrType == AggregationType.AllRound;

    private static double MaybeRound(double d, AggregationType aggrType) => IsRounded(aggrType) ? Math.Round(d, 2) : d;
}
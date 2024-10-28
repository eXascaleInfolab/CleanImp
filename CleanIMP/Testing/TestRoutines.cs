using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CleanIMP.Algorithms.Analysis;
using CleanIMP.Algorithms.Imputation;
using CleanIMP.Config;
using CleanIMP.Utilities;

namespace CleanIMP.Testing;

/// <summary>
/// This is the primary generic class for experiments and their analysis. It describes experiments' behaviors without specifics.
/// Ideally, adding new tasks, scenarios, metrics etc. should never involve changes to this file.
/// Only experiment behavior changes should involve editing this file.
/// All other things are handled within instances of generic classes/interfaces that this class depends on.
/// See classes/interfaces the types depend on ('where' type constraints). Also, see <see cref="Program"/> (Program.cs) for how to use those generic types.
/// </summary>
/// <typeparam name="TTask">Type for downstream task, main description of task-specific behaviors (contamination/decontamination, working with data)</typeparam>
/// <typeparam name="TConfig">Type for experiment configuration for a given task.</typeparam>
/// <typeparam name="TScenario">Scenario types used by the task for the experiments.</typeparam>
/// <typeparam name="TData">Type for the dataset container.</typeparam>
/// <typeparam name="TDown">Type for the container of downstream results.</typeparam>
/// <typeparam name="TMetric">Metric used to evaluate the downstream results.</typeparam>
public static partial class TestRoutines<TTask, TConfig, TScenario, TData, TDown, TMetric>
    where TTask : IDownstreamTask<TTask, TConfig, TScenario, TData, TDown>
    where TConfig : TaskConfig<TScenario>
    where TScenario : IScenario<TScenario>
    where TData : IDataset<TData, TConfig, TScenario, TDown>
    where TMetric : IMetric<TMetric, TDown>
{
    //
    // Result-producing routines
    //
    
    public static void ExecuteExperiment(TConfig config)
    {
        // At this point basically everything is validated: data, configurations etc.
        // There should be no surprises except if one of the datasets is malformed.
        
        foreach (TScenario scen in config.Scenarios)
        {
            foreach (string data in config.Datasets)
            {
                if (config.PerformContamination) RunContamination(data, scen, config);
                if (config.PerformEvaluation) RunDownstream(data, scen, config);
            }
        }
    }

    //
    // Testing process
    //

    private static void RunContamination(string data, TScenario scen, TConfig config)
    {
        ImmutableList<Algorithm> algos = config.Algorithms;
        
        //
        // Step 1 - Load data
        //

        TData dataset = TTask.LoadData(data, config);

        // prepare multi-dimensions
        int[] ticks = scen.Ticks(dataset.TsLen(), dataset.TsCount()).ToArray();
        
        //
        // Step 2 - Contaminate & decontaminate data, then dump on disk
        //

        Console.WriteLine($"Task = {config.CurrentTask.ToLongTaskString()}; Job = contamination; Data = {data}; Scenario = {scen}");

        foreach (Algorithm alg in algos)
        {
            int parallel = Utils.ParallelExecutionNo(ticks.Length, alg.UseParallel);
            
            Console.WriteLine($"Algorithm: {alg.AlgCode}" + (parallel > 1 ? $" (will run parallel over {parallel})" : ""));

            ticks.AsParallel().WithDegreeOfParallelism(parallel).ForAll(tick =>
            {
                TData ds = dataset.Clone();

                ds.ContaminateData(config, scen, tick);
                
                if (ds.TsLen() != dataset.TsLen() || ds.TsCount() != dataset.TsCount())
                {
                    Console.WriteLine("Mismatch! Contamination process altered dataset structure. Aborting.");
                    Environment.Exit(-1);
                }

                ds.RecoverData(config, alg);
                
                if ((ds.TsLen() != dataset.TsLen() || ds.TsCount() != dataset.TsCount()) && !alg.AlgCodeBase.ToLower().StartsWith("dni")) //DNI exception
                {
                    Console.WriteLine("Mismatch! Decontamination process altered dataset structure. Aborting.");
                    Environment.Exit(-1);
                }

                TestIO.CreateContaminatedLocation(config, data, scen, tick);
                string location = TestIO.ContaminatedLocation(config, data, scen, tick, alg);

                TTask.WriteContamination(location, ds);
            });
        }

        Console.WriteLine("Contamination job complete");
    }
    
    private static void RunDownstream(string data, TScenario scen, TConfig config)
    {
        ImmutableList<Algorithm> algos = config.Algorithms;
        ImmutableList<string> downAlgos = config.DownstreamAlgorithms;
        
        //
        // Step 1 - Load cached data
        //

        TData dataset = TTask.LoadData(data, config);

        // prepare multi-dimensions
        int[] ticks = scen.Ticks(dataset.TsLen(), dataset.TsCount()).ToArray();
        string location = TestIO.ContaminatedLocation(config, data, scen);
        Dictionary<string, Dictionary<int, TData>> algorithmRecoveries = TTask.LoadDecontaminatedData(dataset, location, ticks, config.Algorithms);

        //
        // Step 2 - Classify
        //

        Console.WriteLine($"Task = {config.CurrentTask.ToLongTaskString()}; Job = evaluation; Data = {data}; Scenario = {scen}");

        // 2.1 - produce reference (if doesn't exist) before doing anything else
        System.IO.Directory.CreateDirectory($"{config.DataWorkPath(data)}reference/");

        foreach (string calg in downAlgos)
        {
            if (config.Reference == ReferenceBehavior.NoReference)
                break;
            
            if (!TestIO.HasDownstreamReference(config.DataWorkPath(data), calg) || config.Reference == ReferenceBehavior.ReferenceOnlyReplace)
            {
                Console.WriteLine($"No reference result for dataset {data} detected - will run raw downstream reference on {calg}.");

                (long rt, TDown res) = dataset.RunDownstream(config, calg, 0);
                
                Console.WriteLine($"Reference runtime = {rt} microseconds ({((double)rt) / (1000 * 1000)} seconds)");
                
                TTask.WriteDownstream(TestIOHelpers.ReferenceResultLocation(config.DataWorkPath(data), calg) + ".txt", res);
                TestIOHelpers.DumpRuntime(rt, TestIOHelpers.ReferenceResultLocation(config.DataWorkPath(data), calg) + ".runtime");
            }
        }

        if (config.Reference is ReferenceBehavior.ReferenceOnly or ReferenceBehavior.ReferenceOnlyReplace)
        {
            Console.WriteLine("Evaluation job complete (reference only)");
            return;
        }

        // 2.2 - run the remaining tests
        foreach (Algorithm alg in algos)
        {
            Console.WriteLine($"Algorithm: {alg.AlgCode}");
            int parallel = config.GetDownstreamParallel(ticks.Length);
            
            ticks.AsParallel().WithDegreeOfParallelism(parallel).ForAll(tick =>
            {
                foreach (string downAlgo in downAlgos)
                {
                    TData decontaminated = algorithmRecoveries[alg.AlgCode][tick];
                    
                    (_, TDown res) = decontaminated.RunDownstream(config, downAlgo, tick);

                    TestIO.CreateResultLocation(config, data, scen, tick, alg);
                    string resultLocation = TestIOHelpers.ResultLocation(config.DataWorkPath(data), scen.ToString()!, tick, alg);

                    TTask.WriteDownstream($"{resultLocation}{downAlgo}.txt", res);
                }
            });
            if (parallel > 1) Console.WriteLine($"Parallel execution over {parallel} threads.");
        }

        Console.WriteLine("Evaluation job complete");
    }
}

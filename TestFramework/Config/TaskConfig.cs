using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using CleanIMP.Algorithms.Imputation;
using CleanIMP.Testing;
using CleanIMP.Utilities;

namespace CleanIMP.Config;

/// <summary>
/// Base class for configurations. Each downstream task has its own subclass derived from here.
/// Base class contains information that is shared between all tasks, mostly upstream information and general setup of the experiment.
/// All configuration parameters are generally immutable, i.e. we don't dynamically decide what to run and what not to run.
/// Every field in base class + subclasses is taken directly from the config file.
/// Some fields have defaults (e.g. behavior), some fields don't (e.g. datasets, algorithms).
/// </summary>
/// <typeparam name="TScenario">Scenario type derived from an interface, to handle different contamination for different dataset structure (which is dependent on the downstream task)</typeparam>
public abstract class TaskConfig<TScenario>
    where TScenario : IScenario<TScenario>
{
    //
    // Core parameters
    //
    public readonly Task CurrentTask;
    
    public readonly string WorkingDirectory = "./";
    public readonly string DataSource;
    
    // Behavior
    public readonly bool PerformContamination = false;
    public readonly bool PerformEvaluation = false;
    public readonly bool PerformNormalization = true;
    public readonly bool ParallelizeDownstream = true;

    public readonly ReferenceBehavior Reference = ReferenceBehavior.Both;
    
    // Test setup
    public readonly ImmutableList<Algorithm> Algorithms = ImmutableList.Create<Algorithm>();
    public readonly ImmutableList<string> DownstreamAlgorithms = ImmutableList.Create<string>();
    public readonly ImmutableList<string> Datasets = ImmutableList.Create<string>();
    public readonly ImmutableList<TScenario> Scenarios = ImmutableList.Create<TScenario>();
    
    // [WARNING] Changing critical testing parameters completely invalidates any cached results.
    public readonly int McarSeed = 0x9_F99_C17; //only used in scenarios marked as mcar-*
    // For the base class, this is only the case for McarSeed, but there's more in the subclasses.
    // A hash is employed for (in)validation to warn (and refuse to run) if current results use a different setup.
    // However, this is not bulletproof. Caution is advised when changing such parameters in the config AND behavior in the code.
    // Make sure different configurations are never merged together.

    // Instance members
    public string DataWorkPath(string data) => WorkingDirectory + data + "/";
    public string DataSourcePath(string data) => DataSource + data + "/";

    protected TaskConfig(Dictionary<string, string> configFileParams, Task task)
    {
        CurrentTask = task;
        
        if (configFileParams.ContainsKey("workingdir"))
        {
            string workingDir = configFileParams.Consume("workingdir").Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            if (!workingDir.EndsWith("/")) workingDir += "/";

            WorkingDirectory = workingDir;
        }

        if (configFileParams.ContainsKey("datasource"))
        {
            string dataSource = configFileParams.Consume("datasource").Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            if (!dataSource.EndsWith("/")) dataSource += "/";

            DataSource = dataSource;
        }
        else // basically the only "else" in this function since it employs a fallback
        {
            DataSource = WorkingDirectory;
        }
        
        if (configFileParams.ContainsKey("mcarseed"))
        {
            McarSeed = Convert.ToInt32(configFileParams.Consume("mcarseed"));
        }

        if (configFileParams.ContainsKey("performcontamination"))
        {
            PerformContamination = Convert.ToBoolean(configFileParams.Consume("performcontamination"));
        }

        if (configFileParams.ContainsKey("performevaluation"))
        {
            PerformEvaluation = Convert.ToBoolean(configFileParams.Consume("performevaluation"));
        }

        if (configFileParams.ContainsKey("performnormalization"))
        {
            PerformNormalization = Convert.ToBoolean(configFileParams.Consume("performnormalization"));
        }

        if (configFileParams.ContainsKey("parallelizedownstream"))
        {
            ParallelizeDownstream = Convert.ToBoolean(configFileParams.Consume("parallelizedownstream"));
        }

        if (configFileParams.ContainsKey("reference"))
        {
            switch (configFileParams.Consume("reference").ToLower())
            {
                case "referenceonly":
                    Reference = ReferenceBehavior.ReferenceOnly;
                    break;
                
                case "referenceonlyreplace":
                case "referencereplace":
                    Reference = ReferenceBehavior.ReferenceOnlyReplace;
                    break;
                
                case "noreference":
                case "referencenone":
                    Reference = ReferenceBehavior.NoReference;
                    break;
                
                case "both":
                    Reference = ReferenceBehavior.Both;
                    break;
                
                default:
                    throw new ApplicationException("Unrecognized parameter value for reference key.");
            }
        }

        if (configFileParams.ContainsKey("algorithms"))
        {
            string algorithms = configFileParams.Consume("algorithms");
            Algorithms = algorithms
                .Split(',')
                .Select(s => s.Trim())
                .Select(AlgorithmFactory.ConstructAlgorithm)
                .ToImmutableList();
        }

        if (configFileParams.ContainsKey("downstreamalgorithms"))
        {
            string downstreamAlgorithms = configFileParams.Consume("downstreamalgorithms");
            DownstreamAlgorithms = downstreamAlgorithms
                .Split(',')
                .Select(s => s.Trim())
                .Distinct()
                .ToImmutableList();
        }

        if (configFileParams.ContainsKey("datasets"))
        {
            string dataSets = configFileParams.Consume("datasets");
            Datasets = dataSets
                .Split(',')
                .Select(s => s.Trim())
                .WhereNOT(String.IsNullOrEmpty)
                .Distinct()
                .ToImmutableList();
        }

        if (configFileParams.ContainsKey("scenarios"))
        {
            string[] scen = configFileParams.Consume("scenarios").Split(',').Select(x => x.Trim().ToLower()).ToArray();
            Scenarios = scen.Distinct().Select(TScenario.CreateByName).ToImmutableList();
            
            if (scen.Length != Scenarios.Count)
            {
                throw new ArgumentException("One or more provided scenarios in the config are not valid or duplicated.");
            }
        }
    }

    public int GetDownstreamParallel(int tasks = 0)
    {
        if (!ParallelizeDownstream)
        {
            return 1;
        }

        return Utils.ParallelExecutionNo(tasks);
    }

    public virtual bool ConfigValidation(bool analysis)
    {
        if (!PerformContamination && !PerformEvaluation && !analysis) // just QoL, if analysis is selected - we're fine
        {
            Console.WriteLine("Contamination and evaluation jobs are both disabled.");
            Console.WriteLine("The resulting run is set to do nothing. Aborting procedure.");
            return false;
        }
        
        if (Datasets.Count == 0)
        {
            Console.WriteLine("List of datasets to use is empty. Aborting procedure.");
            return false;
        }
        
        if (Algorithms.Count == 0)
        {
            Console.WriteLine("List of imputation algorithms to use is empty. Aborting procedure.");
            return false;
        }

        if (Algorithms.Select(alg => alg.AlgCode).Distinct().Count() != Algorithms.Count)
        {
            // this error triggers if some algorithm does not output distinct AlgCodes for distinct parametrization
            // this is NOT a mistake by the validator, this is arguably even more important than dupe runs
            // because identical AlgCode values mean that the results from those instances will overwrite each other
            // do NOT disable/override this validation under any circumstances - it's guaranteed data corruption
            // reconfigure AlgCode generation in the algorithm class instead
            Console.WriteLine("List of imputation algorithms contains duplicates (algorithms with matching parameters). Aborting procedure.");
            return false;
        }

        if (DownstreamAlgorithms.Count == 0 && PerformEvaluation)
        {
            Console.WriteLine("List of downstream algorithms is empty with evaluation task enabled. Aborting procedure.");
            return false;
        }

        if (Scenarios.Count == 0)
        {
            Console.WriteLine("List of imputation scenarios to use is empty. Aborting procedure.");
            return false;
        }
        
        if (!Directory.Exists(DataSource))
        {
            Console.WriteLine("Data source points to a non-existent location. Aborting procedure.");
            return false;
        }

        return true;
    }

    public abstract int InvalidationHash();
}

/// <summary>
/// A class with methods that help with the task and configuration management (text constants, validation etc.)
/// </summary>
public static class TaskHelpers
{
    public static Task GetTask(string task)
        => task.ToLower() switch
        {
            "mvclass" => Task.MultivariateTsClassification,
            "forecast" => Task.Forecasting,
            "uniclass" => Task.UnivariateTsClassification,
            "unicluster" => Task.TimeSeriesClustering,
            _ => throw new ArgumentException($"Unknown downstream task: {task}")
        };

    public static string ToLongTaskString(this Task task) => task switch
    {
        Task.MultivariateTsClassification => "Multivariate Time Series Classification",
        Task.UnivariateTsClassification => "Univariate Time Series Classification",
        Task.TimeSeriesClustering => "Time Series Clustering",
        Task.Forecasting => "Forecasting",
        _ => throw new ArgumentOutOfRangeException(nameof(task), task, null)
    };

    public static void ValidateConfig<TScenario>(TaskConfig<TScenario> config, bool analysis)
        where TScenario : IScenario<TScenario>
    {
        // Step 1 - validate integrity of the configuration itself
        if (!config.ConfigValidation(analysis))
        {
            // error message is printed from inside the validation function, nothing else to be done here
            Environment.Exit(-1);
        }
        
        Console.WriteLine($"Config loaded and validated for task [{config.CurrentTask.ToLongTaskString()}].");

        // Step 2 - validate experiment setup (with the help of a token) in the current work directory.
        // This is done to ensure that experiments using different setup
        // (e.g. random seed for mcar experiments, train/test behavior etc.)
        // are never merged together in a single working directory leading to wrong analysis of those results.
        string tokenPath = $"{config.WorkingDirectory}dcb_token";
        string configToken = config.InvalidationHash().ToString();

        if (!File.Exists(tokenPath))
        {
            if (!Directory.Exists(config.WorkingDirectory))
                Directory.CreateDirectory(config.WorkingDirectory);
            
            File.WriteAllText(tokenPath, configToken);
            Console.WriteLine($"No token found in the working directory, claiming the folder with hash {configToken}");
        }
        else
        {
            string currentToken = File.ReadAllText(tokenPath);
            if (currentToken.Trim() != configToken)
            {
                Console.WriteLine("Token mismatch in the working directory chosen for the experiment.");
                Console.WriteLine($"Current token = {currentToken}; Config token = {configToken}");
                Console.WriteLine("Please ensure the experiment setup matches the data in the working directory.");
                Console.WriteLine($"If necessary reset the token manually by deleting \"{tokenPath}\" file. ONLY DO THIS IF YOU KNOW WHAT YOU ARE DOING!");
                Environment.Exit(-1);
            }
            Console.WriteLine("Work directory token matches current configuration, proceeding...");
        }
    }
}

public enum Task
{
    MultivariateTsClassification, UnivariateTsClassification, TimeSeriesClustering, Forecasting
}

/// <summary>
/// Behavior of "reference" runs. Reference runs are done on uncontaminated datasets.
/// This means that for each dataset and each downstream algorithm there is only one reference result (runtime+metric).
/// Only applicable downstream.
/// </summary>
public enum ReferenceBehavior
{
    ReferenceOnly, // Skip downstream experiments on decontaminated data and only run reference. Skips run which already have results.
    ReferenceOnlyReplace, // Same but replace reference results even if those were already produced.
    NoReference, // Skip reference and only run downstream experiments on decontaminated data.
    Both // Run experiments on decontaminated data and reference. Reference replacement behavior is the same as ReferenceOnly. Downstream experiments are always replaced.
}
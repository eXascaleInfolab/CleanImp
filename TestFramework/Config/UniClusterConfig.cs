using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CleanIMP.Testing;
using CleanIMP.Utilities;

namespace CleanIMP.Config;

public sealed class UniClusterConfig : TaskConfig<ScenarioUnivariate>
{
    //
    // Experiment run parameters
    //
    //none

    //
    // Experiment setup
    // [WARNING] Critical testing parameters
    //

    public int Runs { get; } = 1;

    //
    // Constructor
    //
    public UniClusterConfig(Dictionary<string, string> configFileParams)
        : base(configFileParams, Task.TimeSeriesClustering)
    {
        int lastCount = configFileParams.Count + 1;// protection from infinite loop
        while (configFileParams.Count != 0 && configFileParams.Count < lastCount)
        {
            lastCount = configFileParams.Count;
            
            string key = configFileParams.Keys.First();
            switch (key)
            {
                case "runs":
                    Runs = Int32.Parse(configFileParams.Consume(key));
                    break;
                
                default: throw new ArgumentException($"Unexpected configuration parameter {key}.");
            }
        }

        if (configFileParams.Count != 0)
        {
            throw new ApplicationException("Unexpected problem while parsing config file.");
        }
    }

    //
    // Functions
    //
    public override bool ConfigValidation(bool analysis)
    {
        if (!base.ConfigValidation(analysis))
        {
            //base class will output the problem
            return false;
        }

        foreach (string data in Datasets)
        {
            string path = $"{DataSource}{data}/";
            if (!File.Exists($"{path}{data}_TRAIN.ts") || !File.Exists($"{path}{data}_TEST.ts"))
            {
                Console.WriteLine($"Dataset {data} cannot be found, either the whole folder or some files are missing.");
                return false;
            }
        }
        
        return true;
    }

    public override int InvalidationHash()
    {
        int normHash = 22717 * (PerformNormalization ? 7523 : 1);

        int seedsHash = 58171 * McarSeed;
        int runsHash = 35099 * Runs;

        return seedsHash ^ normHash ^ runsHash;
    }
}

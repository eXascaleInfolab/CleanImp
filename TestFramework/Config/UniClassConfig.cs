using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CleanIMP.Testing;
using CleanIMP.Utilities;

namespace CleanIMP.Config;

public sealed class UniClassConfig : TaskConfig<ScenarioUnivariate>
{
    //
    // Experiment run parameters
    //
    //none

    //
    // Experiment setup
    // [WARNING] Critical testing parameters
    //

    public readonly bool EnableTestSubSample = false;
    public readonly int SeedTestSubSample = 0x9_A9F_3B1; //not used unless `EnableTestSubSample` is set to `true`

    public readonly bool ContaminateTrainSet = true;
    public readonly bool ContaminateTestSet = false;

    public readonly bool ImputeByClass = true;

    //
    // Constructor
    //
    public UniClassConfig(Dictionary<string, string> configFileParams)
        : base(configFileParams, Task.UnivariateTsClassification)
    {
        int lastCount = configFileParams.Count + 1;// protection from infinite loop
        while (configFileParams.Count != 0 && configFileParams.Count < lastCount)
        {
            lastCount = configFileParams.Count;
            
            string key = configFileParams.Keys.First();
            switch (key)
            {
                case "contaminatetrain":
                    ContaminateTrainSet = Convert.ToBoolean(configFileParams.Consume(key));
                    break;
                
                case "contaminatetest":
                    ContaminateTestSet = Convert.ToBoolean(configFileParams.Consume(key));
                    break;
                
                case "subsample":
                    EnableTestSubSample = Convert.ToBoolean(configFileParams.Consume(key));
                    break;
                
                case "imputebyclass":
                    ImputeByClass = Convert.ToBoolean(configFileParams.Consume(key));
                    break;
                
                case "subsampleseed":
                    SeedTestSubSample = Convert.ToInt32(configFileParams.Consume(key));
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

        if (!(ContaminateTrainSet || ContaminateTestSet))
        {
            Console.WriteLine("Contamination job is set to not contaminate both train and test set.");
            Console.WriteLine("The resulting run will be completely useless. Aborting procedure.");
            return false;
        }

        foreach (string data in Datasets)
        {
            string path = $"{DataSource}{data}/";
            if (!File.Exists($"{path}{data}_TRAIN.ts") || !File.Exists($"{path}{data}_TRAIN.ts"))
            {
                Console.WriteLine($"Dataset {data} cannot be found, either the whole folder or some files are missing.");
                return false;
            }
        }
        
        return true;
    }

    public override int InvalidationHash()
    {
        int contHash = 32603 * (ContaminateTrainSet ? 5231 : 1 ) + 42937 * (ContaminateTestSet ? 8807 : 1);
        int subSampleHash = 4079 * (EnableTestSubSample ? 1483 : 1);
        int byClassHash = 1033 * (ImputeByClass ? 9949 : 1);
        int normHash = 631 * (PerformNormalization ? 14779 : 1);

        int seedsHash = 18191 * SeedTestSubSample + 21821 * McarSeed;

        return contHash ^ subSampleHash ^ seedsHash ^ byClassHash ^ normHash;
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CleanIMP.Testing;
using CleanIMP.Utilities;

namespace CleanIMP.Config;

public sealed class ForecastConfig : TaskConfig<ScenarioMultivariate>
{
    //
    // Experiment run parameters
    //
    //none

    //
    // Experiment setup
    // [WARNING] Critical testing parameters
    //
    public readonly int ForecastWindow = 10; // %

    //
    // Constructor
    //
    public ForecastConfig(Dictionary<string, string> configFileParams)
        : base(configFileParams, Task.Forecasting)
    {
        int lastCount = configFileParams.Count + 1;// protection from infinite loop
        while (configFileParams.Count != 0 && configFileParams.Count < lastCount)
        {
            lastCount = configFileParams.Count;
            
            string key = configFileParams.Keys.First();
            switch (key)
            {
                case "fcwindow":
                    ForecastWindow = Convert.ToInt32(configFileParams.Consume(key));
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
            if (!File.Exists($"{path}{data}_matrix.txt"))
            {
                Console.WriteLine($"Dataset {data} cannot be found, either the whole folder or some files are missing.");
                return false;
            }
        }
        
        return true;
    }

    public override int InvalidationHash()
    {
        int seedsHash = 7283 * McarSeed;
        int wndHash = 15289 * ForecastWindow;
        int normHash = 6199 * (PerformNormalization ? 3347 : 1);

        return seedsHash ^ wndHash ^ normHash;
    }
}
using System;
using System.Diagnostics;
using System.Linq;
using CleanIMP.Testing;
using CleanIMP.Utilities;

namespace CleanIMP.Algorithms.Downstream;

public static class MultivariateClassification
{
    //
    // Constants/Variables
    //

    private const string SkTimeLocation = "../external_code/sktime/";
    private const string DataFolder = "data/";

    //
    // Main API
    //

    public static (long, string[]) RunClassification(MultivarDataset dataset, string classificationAlgorithm, int slot = 0)
    {
        // step 1 - store data
        string trainFile = SkTimeLocation + DataFolder + $"dataset_TRAIN_{slot}.ts";
        string testFile = SkTimeLocation + DataFolder + $"dataset_TEST_{slot}.ts";

        dataset.Headers.Concat(dataset.Train.Select(x => x.ToSkTimeLine())).FileWriteAllLines(trainFile);
        dataset.Headers.Concat(dataset.Test.Select(x => x.ToSkTimeLine())).FileWriteAllLines(testFile);

        // step 2 - run
        Stopwatch sw = new();
        sw.Start();
        string[] output = Utils.RunOutputProcess(Utils.PythonExec, $"classify.py {classificationAlgorithm} {slot}", SkTimeLocation).ToArray();
        sw.Stop();

        if (output.Length == 0 || output.All(String.IsNullOrEmpty))
        {
            throw new ApplicationException("Classifier has not returned a valid classification (0 entries), aborting further execution.");
        }

        return ((long)(sw.Elapsed.TotalMilliseconds * 1000), output);
    }
}
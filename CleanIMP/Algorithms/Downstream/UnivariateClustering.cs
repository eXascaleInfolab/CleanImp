using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using CleanIMP.Testing;
using CleanIMP.Utilities;
using CleanIMP.Utilities.Mathematical;

namespace CleanIMP.Algorithms.Downstream;

public static class UnivariateClustering
{
    //
    // Constants/Variables
    //

    private const string ClusteringLocation = "../external_code/clustering/";
    private const string DataFolder = "data/";

    //
    // Main API
    //

    public static (long, int[][]) RunClustering(UniClusterDataset dataset, string clusteringAlgorithm, int runs, int slot = 0)
    {
        Stopwatch sw = new();
        
        // step 1 - store data
        string inputFile = ClusteringLocation + DataFolder + $"dataset_{slot}.ts";

        dataset.ToSkTimeLine().FileWriteAllLines(inputFile);

        string command = Utils.PythonExec;
            
        // step 2 - run
        sw = new();
        sw.Start();
        IEnumerable<string> result = Utils.RunOutputProcess(command, $"cluster.py {clusteringAlgorithm} {dataset.ClassCount} {runs} {slot}", ClusteringLocation);
        int[][] output = MathX.Parse.ParseIntMatrix(result);// inside the stopwatch, because process output is lazy enumerated
        sw.Stop();

        if (output.Length == 0 || output.Length * output[0].Length < 5)
        {
            Console.WriteLine("[WARNING] Low output trigger");
        }
        
        Thread.Sleep(250); // chaining gpu calls for some reason breaks

        return ((long)(sw.Elapsed.TotalMilliseconds * 1000), output);
    }
}
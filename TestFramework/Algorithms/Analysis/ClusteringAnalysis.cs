using System;
using System.Linq;
using CleanIMP.Utilities;

namespace CleanIMP.Algorithms.Analysis;

public abstract class ClusteringMetric : IMetric<ClusteringMetric, int[][]>
{
    // int[][] here should be interpreted as "a list of int[]" where int[] is a list of cluster assignments of a particular run of a clustering algorithm
    // the returned measure in an average of individual metrics of all runs
    
    //
    // Constants
    //

    protected const int TP = 0;
    protected const int TN = 1;
    protected const int FP = 2;
    protected const int FN = 3;

    protected const int CONFUSION_SIZE = 4;//# of metrics above
    
    //
    // Static functions
    //
    
    public static readonly ClusteringMetric[] Measures = { new RandIndex(), new AdjustedRandIndex() };
    
    public static ClusteringMetric Default { get; } = Measures.First();
    
    public static ClusteringMetric GetMetricByName(string name)
    {
        ClusteringMetric? um = Measures.FirstOrDefault(m => m.MeasureName.ToLower() == name.ToLower());
        if (um == null)
        {
            throw new ArgumentException($"Clustering metric {name} not found.");
        }

        return um;
    }

    //
    // Abstract class
    //

    // fields
    public abstract string MeasureName { get; }
    
    // functions
    public double Measure(int[] reference, int[] clustering)
    {
        if (reference.Length != clustering.Length)
        {
            Console.WriteLine($"Data mismatch - reference number of classes ({reference.Length}) doesn't match the count of cluster assignments ({clustering.Length})");
            throw new ArgumentException("Class count mismatch between reference and classification");
        }

        return MeasureInternal(reference, clustering);
    }
    public double Measure(int[][] reference, int[][] clusterings)
    {
        int[][] clusterAssignments = clusterings.ArrayTranspose();
        return clusterAssignments.Average(ca => Measure(reference.Select(cl => cl[0]).ToArray(), ca));
    }

    public double MeasureAll(int[] reference, int[][] clusterings)
    {
        int[][] clusterAssignments = clusterings.ArrayTranspose();
        return clusterAssignments.Average(ca => Measure(reference, ca));
    }

    protected abstract double MeasureInternal(int[] reference, int[] clustering);

    protected static long[] GetConfusionMatrix(int[] reference, int[] clustering)
    {
        long[] confusion = new long[4];
        
        foreach ((int i1, int i2) in LinqX.AllPairsDistinct(reference.Length))
        {
            bool refSame = reference[i1] == reference[i2];
            bool clusterSame = clustering[i1] == clustering[i2];

            switch (refSame, clusterSame)
            {
                case (true, true): // true positive == agreement in same
                    confusion[TP]++;
                    break;
                case (false, false): // true negative == agreement in different
                    confusion[TN]++;
                    break;
                case (false, true): // false positive == clustering disagreed with different
                    confusion[FP]++;
                    break;
                case (true, false): // false negative == clustering disagreed with same
                    confusion[FN]++;
                    break;
            }
        }

        return confusion;
    }
}

public class RandIndex : ClusteringMetric
{
    // fields
    public override string MeasureName => "RandIndex";

    // functions
    protected override double MeasureInternal(int[] reference, int[] clustering)
    {
        long[] confusion = GetConfusionMatrix(reference, clustering);

        return (confusion[TP] + confusion[TN]) / (double) (confusion[TP] + confusion[TN] + confusion[FP] + confusion[FN]);
    }
}

public class AdjustedRandIndex : ClusteringMetric
{
    // fields
    public override string MeasureName => "AdjRandIndex";

    // functions
    protected override double MeasureInternal(int[] reference, int[] clustering)
    {
        long[] confusion = GetConfusionMatrix(reference, clustering);
        
        if (confusion[FN] == 0 && confusion[FP] == 0) // no disagreements
            return 1.0;
        
        double nominator = (confusion[TP] * confusion[TN] - confusion[FN] * confusion[FP]);
        double denominator = ((confusion[TP] + confusion[FN]) * (confusion[FN] + confusion[TN]) +
                              (confusion[TP] + confusion[FP]) * (confusion[FP] + confusion[TN]));

        return 2.0 * nominator / denominator;
    }
}

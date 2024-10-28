using System;
using System.Collections.Generic;
using System.Linq;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

using CleanIMP.Utilities;

// ReSharper disable InconsistentNaming
namespace CleanIMP.Algorithms.Analysis;

public abstract class ClassificationMetric : IMetric<ClassificationMetric, string[]>
{
    //
    // Constants
    //
    protected const int TP = 0;
    protected const int TN = 1;
    protected const int FP = 2;
    protected const int FN = 3;
    protected const int SUP = 4; // support, aka # of samples of the class
    private const int CONFUSION_SIZE = 5;//# of metrics above
    
    //
    // Static functions
    //
    
    public static readonly ClassificationMetric[] Measures = { new Accuracy(), new F1Measure(), new Precision(), new Recall(), new MCCMeasure() };

    public static ClassificationMetric Default { get; } = Measures.First();
    
    public static ClassificationMetric GetMetricByName(string name)
    {
        ClassificationMetric? um = Measures.FirstOrDefault(m => m.MeasureName.ToLower() == name.ToLower());
        if (um == null)
        {
            throw new ArgumentException($"Classification metric {name} not found.");
        }

        return um;
    }

    //
    // Abstract class
    //

    // fields
    public abstract string MeasureName { get; }
    
    // functions
    public double Measure(string[] reference, string[] classification)
    {
        if (reference.Length != classification.Length)
        {
            Console.WriteLine($"Data mismatch - reference number of elements ({reference.Length}) doesn't match the classification number of elements ({classification.Length})");
            throw new ArgumentException("Element count mismatch between reference and classification");
        }

        return MeasureInternal(reference, classification);
    }

    protected static (string[], Dictionary<string, int[]>) BuildConfusionMatrix(string[] reference, string[] classification)
    {
        string[] classes = reference.Concat(classification).Distinct().ToArray();
        Dictionary<string, int[]> confusion = classes.ToDictionary(x => x, _ => new int[CONFUSION_SIZE]);

        for (int i = 0; i < reference.Length; i++)
        {
            int[] confusionRef = confusion[reference[i]];
            int[] confusionPred = confusion[classification[i]];
                
            if (reference[i] == classification[i])
            {
                // same confusion matrices
                confusionRef[TP]++;
            }
            else
            {
                // when there's a classification mismatch
                // - it's a FP for what's predicted
                // and
                // - it's a FN for what it actually is
                confusionRef[FN]++;
                confusionPred[FP]++;
            }

            confusionRef[SUP]++;

            // TN values are 0 for all classes
            // not used in precision/recall/f1 calculations
        }

        return (classes, confusion);
    }

    protected static (string[], Matrix<double>) BuildSquareConfusionMatrix(string[] reference, string[] classification)
    {
        string[] classes = reference.Concat(classification).Distinct().ToArray();
        Matrix<double> confusion = Matrix<double>.Build.Dense(classes.Length, classes.Length);

        for (int i = 0; i < reference.Length; i++)
        {
            int refIdx = classes.IndexOf(reference[i]);
            
            if (reference[i] == classification[i])
            {
                confusion[refIdx, refIdx] += 1.0;
            }
            else
            {
                int classIdx = classes.IndexOf(classification[i]);
                confusion[refIdx, classIdx] += 1.0;
            }
        }

        return (classes, confusion);
    }

    protected abstract double MeasureInternal(string[] reference, string[] classification);
}

//
// Metrics
//

public class Accuracy : ClassificationMetric
{
    // fields
    public override string MeasureName => "Accuracy";

    // functions
    protected override double MeasureInternal(string[] reference, string[] classification)
    {
        int count = 0;

        for (int i = 0; i < reference.Length; i++)
        {
            if (reference[i] == classification[i])
            {
                count++;
            }
        }

        return ((double)count) / reference.Length;
    }
}

public class Precision : ClassificationMetric
{
    // fields
    public override string MeasureName => "Precision";

    // functions
    protected override double MeasureInternal(string[] reference, string[] classification)
    {
        (_, Dictionary<string, int[]> confusion) = BuildConfusionMatrix(reference, classification);

        double TPs = 0;
        double FPs = 0;
        int samples = confusion.Values.Sum(x => x[SUP]);

        foreach (int[] conf in confusion.Values)
        {
            double weight = conf[SUP] / (double) samples;
                
            TPs += weight * conf[TP];
            FPs += weight * conf[FP];
        }

        return TPs / (TPs + FPs);
    }
}
    
public class Recall : ClassificationMetric
{
    // fields
    public override string MeasureName => "Recall";

    // functions
    protected override double MeasureInternal(string[] reference, string[] classification)
    {
        (_, Dictionary<string, int[]> confusion) = BuildConfusionMatrix(reference, classification);

        double TPs = 0;
        double FNs = 0;
        int samples = confusion.Values.Sum(x => x[SUP]);

        foreach (int[] conf in confusion.Values)
        {
            double weight = conf[SUP] / (double) samples;
                
            TPs += weight * conf[TP];
            FNs += weight * conf[FN];
        }

        return TPs / (TPs + FNs);
    }
}
    
public class F1Measure : ClassificationMetric
{
    // fields
    public override string MeasureName => "F1";

    // functions
    protected override double MeasureInternal(string[] reference, string[] classification)
    {
        (_, Dictionary<string, int[]> confusion) = BuildConfusionMatrix(reference, classification);

        double TPs = 0;
        double FPs = 0;
        double FNs = 0;
        int samples = confusion.Values.Sum(x => x[SUP]);

        foreach (int[] conf in confusion.Values)
        {
            double weight = conf[SUP] / (double) samples;
                
            TPs += weight * conf[TP];
            FNs += weight * conf[FN];
            FPs += weight * conf[FP];
        }

        double precision = TPs / (TPs + FPs);
        double recall = TPs / (TPs + FNs);

        double f1 = 2 * (precision * recall) / (precision + recall);
        return Double.IsFinite(f1) ? f1 : 0.0;
    }
}
    
public class MCCMeasure : ClassificationMetric
{
    // fields
    public override string MeasureName => "MCC";

    // functions
    protected override double MeasureInternal(string[] reference, string[] classification)
    {
        // Code is an adaptation of https://en.wikipedia.org/wiki/Phi_coefficient#Multiclass_case
        
        (_, Matrix<double> confusion) = BuildSquareConfusionMatrix(reference, classification);
        
        Vector<double> T = Vector<double>.Build.Dense(confusion.EnumerateRows().Select(r => r.Sum()).ToArray()); // foreach class - # of times it has occured
        Vector<double> P = Vector<double>.Build.Dense(confusion.EnumerateColumns().Select(c => c.Sum()).ToArray()); // foreach class - # of times it was predicted
        int c = MathX.AsInt(confusion.Diagonal().Sum()); // total # of correct guesses
        int s = reference.Length; // total # of samples

        int nominator = c * s - MathX.AsInt(T * P);
        double denominator = Math.Sqrt(s * s - MathX.AsInt(P * P)) * Math.Sqrt(s * s - MathX.AsInt(T * T));

        if (Math.Abs(denominator) < 1E-10) // undefined score
        {
            return Double.PositiveInfinity;
            //+Inf is consistent with undefined scores, like pearson on flat data
            //consider returning 0.0 for both, it's basically just as informative as +Inf
        }

        double res = nominator / denominator;
        return res > 0 ? Math.Min(res + 1e-14, 1.0) : Math.Max(res - 1e-14, -1.0);
        // float imprecision can give values like 1.0000000000000002
        // +eps is done to prevent 0.99999999999 instead of 1.0
        // controlling for -1 is not necessary because it's generally very unlikely to occur
        //    update: okay, it in fact occured with -0.99999999, fixed
    }
}

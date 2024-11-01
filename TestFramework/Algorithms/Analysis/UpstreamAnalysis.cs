using System;
using System.Linq;
using CleanIMP.Utilities.Mathematical.ThirdPartyPorts;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

namespace CleanIMP.Algorithms.Analysis;

public abstract class UpstreamMetric : IMetric<UpstreamMetric, Vector<double>>
{
    //
    // static functions
    //

    public static readonly UpstreamMetric[] Measures =
        { new RmseUpstream(), new MaeUpstream(), new PearsonUpstream(), new SpearmanUpstream(),
            new KraskovMutualInformationUpstream(3), new KraskovMutualInformationUpstream(5) };

    public static UpstreamMetric Default { get; } = Measures.First();
    
    public static UpstreamMetric GetMetricByName(string name)
    {
        UpstreamMetric? um = Measures.FirstOrDefault(m => m.MeasureName.ToLower() == name.ToLower());
        if (um == null)
        {
            throw new ArgumentException($"Upstream metric {name} not found.");
        }

        return um;
    }

    //
    // Abstract class
    //
    
    // fields
    public abstract string MeasureName { get; }

    // functions
    public double Measure(Vector<double> vector1, Vector<double> vector2)
    {
        return MeasureInternal(vector1, vector2);
    }
    protected abstract double MeasureInternal(Vector<double> reference, Vector<double> upstream);
}

//
// Instances of measures
//

public class RmseUpstream : UpstreamMetric
{
    // fields
    public override string MeasureName => "RMSE";

    // functions
    protected override double MeasureInternal(Vector<double> reference, Vector<double> upstream)
    {
        if (reference.Count == 0) return 0.0;
        
        double sqrsum = 0.0;

        for (int i = 0; i < reference.Count; i++)
        {
            double diff = reference[i] - upstream[i];
            sqrsum += diff * diff;
        }

        return Math.Sqrt(sqrsum / reference.Count);
    }
}

public class MaeUpstream : UpstreamMetric
{
    // fields
    public override string MeasureName => "MAE";

    // functions
    protected override double MeasureInternal(Vector<double> reference, Vector<double> upstream)
    {
        if (reference.Count == 0) return 0.0;

        double sum = 0.0;

        for (int i = 0; i < reference.Count; i++)
        {
            double diff = reference[i] - upstream[i];
            sum += Math.Abs(diff);
        }

        return sum / reference.Count;
    }
}

public class PearsonUpstream : UpstreamMetric
{
    // fields
    public override string MeasureName => "CorrPearson";
    
    protected override double MeasureInternal(Vector<double> reference, Vector<double> upstream)
    {
        if (reference.Count == 0) return 0.0;

        double res = Correlation.Pearson(reference, upstream);
        return Double.IsFinite(res) ? res : Double.PositiveInfinity;
    }
}

public class SpearmanUpstream : UpstreamMetric
{
    // fields
    public override string MeasureName => "CorrSpearman";
    
    protected override double MeasureInternal(Vector<double> reference, Vector<double> upstream)
    {
        if (reference.Count == 0) return 0.0;
        
        double res = Correlation.Spearman(reference, upstream);
        return Double.IsFinite(res) ? res : Double.PositiveInfinity;
    }
}

public class KraskovMutualInformationUpstream : UpstreamMetric
{
    public override string MeasureName => $"KraskovMI-k{_k}";

    private readonly int _k;

    public KraskovMutualInformationUpstream(int k = 1)
    {
        if (k < 1)
        {
            throw new ArgumentException($"Value of k ({k}) can't be lower than 1.");
        }
        _k = k;
    }

    protected override double MeasureInternal(Vector<double> reference, Vector<double> upstream)
    {
        if (reference.Count == 0) return 0.0;
        double mi = MutualInformation.KraskovMutualInfo(reference.AsArray() ?? reference.ToArray(), upstream.AsArray() ?? upstream.ToArray(), _k);
        return mi;
    }
}
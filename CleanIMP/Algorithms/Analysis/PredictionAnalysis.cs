using System;
using System.Linq;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Analysis;

public abstract class ForecastMetric : IMetric<ForecastMetric, Vector<double>>
{
    //
    // static functions
    //

    public static readonly ForecastMetric[] Measures =
    {
        new RMSE(), new RMSE(12), new RMSE(24), new RMSE(36), new RMSE(48),new RMSE(60),
        new Pearson(), new Pearson(12), new Pearson(24), new Pearson(36), new Pearson(48),new Pearson(60),
        new SMAPE(), new SMAPE(12), new SMAPE(24), new SMAPE(36), new SMAPE(48), new SMAPE(60),
    };

    public static ForecastMetric Default { get; } = Measures.First();
    
    public static ForecastMetric GetMetricByName(string name)
    {
        //string[] measureParams = 
        ForecastMetric? fm = Measures.FirstOrDefault(m => m.MeasureName.ToLower() == name.ToLower());
        if (fm == null)
        {
            throw new ArgumentException($"Forecasting metric {name} not found.");
        }

        return fm;
    }

    //
    // Abstract class
    //
    
    // fields
    public abstract string MeasureName { get; }
    protected readonly int Horizon;
    
    // constructor

    protected ForecastMetric(int horizon = 0)
    {
        Horizon = horizon == 0 ? Int32.MaxValue : horizon;
    }

    // functions
    protected string HorizonStr => Horizon == Int32.MaxValue ? "" : Horizon.ToString();
    
    public double Measure(Vector<double> reference, Vector<double> forecast)
    {
        if (reference.Count != forecast.Count)
        {
            Console.WriteLine($"dataset mismatch - reference vector size ({reference.Count}) doesn't match the forecast size ({forecast.Count})");
            throw new ArgumentException("Vector size mismatch between reference and forecast");
        }

        if (forecast.Any(v => !Double.IsFinite(v))) return Double.PositiveInfinity;

        return MeasureInternal(reference, forecast);
    }

    protected abstract double MeasureInternal(Vector<double> reference, Vector<double> forecast);
}

public class RMSE : ForecastMetric
{
    // fields
    public override string MeasureName => "RMSE" + HorizonStr;

    // constructor
    public RMSE(int horizon = 0) : base(horizon)
    { }

    // functions
    protected override double MeasureInternal(Vector<double> reference, Vector<double> forecast)
    {
        double sqrSum = 0.0;
        int len = Math.Min(reference.Count, Horizon);

        for (int i = 0; i < len; i++)
        {
            double diff = reference[i] - forecast[i];
            sqrSum += diff * diff;
        }

        return Math.Sqrt(sqrSum / len);
    }
}

public class Pearson : ForecastMetric
{
    // fields
    public override string MeasureName => "Pearson" + HorizonStr;

    // constructor
    public Pearson(int horizon = 0) : base(horizon)
    { }

    // functions
    protected override double MeasureInternal(Vector<double> reference, Vector<double> forecast)
    {
        int len = Math.Min(reference.Count, Horizon);
        double corr = Correlation.Pearson(reference.SubVector(0, len), forecast.SubVector(0, len));
        return Double.IsNaN(corr) ? 0.0 : corr;
    }
}

public class SMAPE : ForecastMetric
{
    // fields
    public override string MeasureName => "SMAPE" + HorizonStr;

    // constructor
    public SMAPE(int horizon = 0) : base(horizon)
    { }

    // functions
    protected override double MeasureInternal(Vector<double> reference, Vector<double> forecast)
    {
        double mapeSum = 0.0;
        int len = Math.Min(reference.Count, Horizon);

        for (int i = 0; i < len; i++)
        {
            double diff = Math.Abs(reference[i] - forecast[i]);
            double sum = Math.Abs(reference[i]) + Math.Abs(forecast[i]);
            // edge cases; both values are >= 0, so one-sided comparisons are safe
            if (sum < 1E-5 && diff < 1E-5)
            {
                diff = 0;
                sum = 1;
            }
            else if (sum < 1E-5) // *in theory* this should never trigger since by definition diff >= sum
            {
                // but in case it does...
                diff = 2.0; //2.0 will be divided back
                sum = 1.0;
                // the idea is that if *all* values are like this, the whole thing becomes 1/n [end] * n [loop] * 2/2 [2.0 /2.0] = 1.0
            }
            mapeSum += diff / (sum / 2);
        }

        return mapeSum / len;
    }
}

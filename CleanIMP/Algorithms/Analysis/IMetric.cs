namespace CleanIMP.Algorithms.Analysis;

/// <summary>
/// Base type for evaluation metrics, both upstream and downstream.
/// Measuring function requires two instances on data where one is considered ground truth.
/// </summary>
/// <typeparam name="T">Type of data that is evaluated by a given metric.</typeparam>
public interface IMetric<out TMetric, in T>
    where TMetric : IMetric<TMetric, T>
{
    // fields
    public string MeasureName { get; }

    public double Measure(T reference, T data);

    // static functions
    public static abstract TMetric Default { get; }

    public static abstract TMetric GetMetricByName(string name);
}

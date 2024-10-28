using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CleanIMP.Algorithms.DataCharacterization;
using CleanIMP.Algorithms.Downstream;
using CleanIMP.Algorithms.Imputation;
using CleanIMP.Config;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using CleanIMP.Utilities;

namespace CleanIMP.Testing;

//
// Abstract type definitions
//

/// <summary>
/// Base interface for an abstract dataset container.
/// Derived classes should handle the fields and methods related to an arbitrary dataset's usage for experiments.
/// </summary>
/// <typeparam name="TData">Self-referential type implementing this interface</typeparam>
/// <typeparam name="TConfig">Type for experiment configuration for a given task.</typeparam>
/// <typeparam name="TScenario">Scenario types used by the task for the experiments.</typeparam>
/// <typeparam name="TDown">Type for the container of downstream results.</typeparam>
public interface IDataset<out TData, in TConfig, in TScenario, TDown>
    where TData : IDataset<TData, TConfig, TScenario, TDown>
    where TConfig : TaskConfig<TScenario>
    where TScenario : IScenario<TScenario>
{
    // fields
    string Data { get; }
    
    // methods
    int TsLen();

    int TsCount();
    
    TData Clone();

    TDown GetDownstream();

    Vector<double> GetUpstream(TConfig config, IEnumerable<MissingBlock> missingBlocks);
    
    (string, double)[] BasicDump();
    
    (double[], Vector<double>[]) AdvancedDump(string metric, bool legacy = true);

    (string, double)[] Characterization();

    void ContaminateData(TConfig config, TScenario scen, int tick);

    void RecoverData(TConfig config, Algorithm alg);

    (long, TDown) RunDownstream(TConfig config, string downAlgo, int slot);
}

//
// Univariate classification data types
//

public struct UnivarDataset : IDataset<UnivarDataset, UniClassConfig, ScenarioUnivariate, string[]>
{
    // fields (interface)
    public string Data { get; }
    
    // fields (native)
    public List<string> Headers { get; }
    public List<UnivarSeries> Train { get; private init; }
    public List<UnivarSeries> Test { get; private init; }

    // constructors
    public UnivarDataset(string data, List<string> headers, List<UnivarSeries> train, List<UnivarSeries> test)
    {
        Data = data;
        Train = train;
        Test = test;
        Headers = headers;
    }

    // interface implementations
    public UnivarDataset Clone()
    {
        return this with {Train = Train.Select(x => x.Clone()).ToList(), Test = Test.Select(x => x.Clone()).ToList()};
    }

    public int TsLen() => Train.First().Vector.Count;

    public int TsCount() => Train.Count; // this doesn't work if test set is contaminated

    public (string, double)[] BasicDump()
    {
        List<(string, double)> dump = new()
        {
            ("rows", Train.First().Vector.Count),
            ("samples_train", Train.Count),
            ("samples_test", Test.Count),
            ("classes", Train.Select(x => x.TrueClass).Distinct().Count()),
        };

        return dump.ToArray();
    }

    public (double[], Vector<double>[]) AdvancedDump(string metric, bool legacy = true)
    {
        if (!legacy) return AdvancedDumpNew(metric);
        
        Matrix<double> ts;
            
        if (metric.ToLower() == "inclass")
        {
            ts = Train.GroupBy(t => t.TrueClass).First().Select(group => group.Vector).Take(10).ToMatrix();
        }
        else
        {
            ts = Train.GroupBy(t => t.TrueClass).Select(group => group.First().Vector).ToMatrix();
        }
        
        double[] corr = Enumerable.Range(1, ts.ColumnCount - 1).Select(i => Correlation.Pearson(ts.Column(0), ts.Column(i))).ToArray();

        return (corr, ts.EnumerateColumns().Take(3).ToArray());
    }

    public (double[], Vector<double>[]) AdvancedDumpNew(string metric)
    {
        Matrix<double> ts;
            
        if (metric.ToLower() == "inclass")
        {
            ts = Train.GroupBy(t => t.TrueClass).First().Select(elem => elem.Vector).Take(4).ToMatrix();
        }
        else
        {
            ts = Train.GroupBy(t => t.TrueClass).Select(group => group.First().Vector).Take(4).ToMatrix();
        }
        
        double[] corr = Enumerable.Range(1, ts.ColumnCount - 1).Select(i => Correlation.Pearson(ts.Column(0), ts.Column(i))).ToArray();

        return (corr, ts.EnumerateColumns().ToArray());
    }

    public (string, double)[] Characterization()
        => UnivarCharacterization.ExtractDatasetFeatures(this);

    public string[] GetDownstream() => Test.Select(x => x.TrueClass).ToArray();

    public Vector<double> GetUpstream(UniClassConfig config, IEnumerable<MissingBlock> missingBlocks)
    {
        List<Vector<double>> vec = new();
        
        foreach (MissingBlock block in missingBlocks)
        {
            if (config.ContaminateTrainSet)
            {
                vec.Add(Train[block.Column].Vector.GetBlock(block));
            }
            if (config.ContaminateTestSet)
            {
                throw new NotImplementedException();
                //vec.Add(Test[block.Column].Vector.GetBlock(block));
            }
        }

        return Vector<double>.Build.Dense(vec.Flatten().ToArray());
    }

    public void ContaminateData(UniClassConfig config, ScenarioUnivariate scen, int tick)
    {
        int testCount = Test.Count;

        foreach (MissingBlock block in scen.GetContamination(TsLen(), TsCount(), tick, config.McarSeed))
        {
            if (config.ContaminateTrainSet)
            {
                Train[block.Column].Contaminate(block);
            }
            if (config.ContaminateTestSet)
            {
                Test[block.Column].Contaminate(block);
            }
        }

        // sanity check: contamination process
        // [!] exceptionally here and only covers test set, the rest (train & len) is done outside
        if (Test.Count != testCount || Train.First().Vector.Count != Test.First().Vector.Count)
        {
            Console.WriteLine("Mismatch! Contamination process altered dataset structure. Aborting.");
            Environment.Exit(-1);
        }
    }

    public void RecoverData(UniClassConfig config, Algorithm alg)
    {
        int testCount = Test.Count;
        
        alg.RecoverDataset(ref this, config.ImputeByClass);

        // sanity check: decontamination process
        // [!] exceptionally here and only covers test set, the rest (train & len) is done outside
        if (Test.Count != testCount || Train.First().Vector.Count != Test.First().Vector.Count)
        {
            Console.WriteLine("Mismatch! Decontamination process altered dataset structure. Aborting.");
            Environment.Exit(-1);
        }
    }

    public (long, string[]) RunDownstream(UniClassConfig config, string downAlgo, int slot)
        => UnivariateClassification.RunClassification(Headers, this, downAlgo, slot);
}

public class UnivarSeries
{
    // internals & building
    public Vector<double> Vector;
    public readonly string TrueClass;

    // construction
    private UnivarSeries(Vector<double> vector, string trueClass)
    {
        Vector = vector;
        TrueClass = trueClass.ToLower();
    }

    public UnivarSeries Clone() => new (Vector.Clone(), TrueClass);

    public static UnivarSeries FromSktime(string skTimeLine)
    {
        string[] entry = skTimeLine.Split(":");
        string[] series = entry[0].Split(',');
        int len = series.Length;

        var vec = MathX.Zeros(len);

        for (int j = 0; j < len; j++)
        {
            vec[j] = Double.Parse(series[j]);
        }

        return new UnivarSeries(vec, entry[1]);
    }

    // operations & output
    public void Contaminate(MissingBlock block)//column no. is ignored from this point on
    {
        TimeSeries.RemoveBlock(ref Vector, block);
    }

    public string ToSkTimeLine()
    {
        StringBuilder sb = new();

        for (int i = 0; i < Vector.Count - 1; i++)
        {
            sb.Append(Vector.Storage[i] + ",");
        }
        sb.Append(Vector.Storage[Vector.Count - 1] + ":" + TrueClass);

        return sb.ToString();
    }
}

//
// Forecasting data types
//

public struct ForecastDataset : IDataset<ForecastDataset, ForecastConfig, ScenarioMultivariate, Vector<double>>
{
    public string Data { get; }
    public Matrix<double> Train { get; private set; }
    public Vector<double> Forecast { get; private init; }
    public readonly int Season;

    public ForecastDataset(string data, Matrix<double> train, Vector<double> forecast, int season)
    {
        Data = data;
        Train = train;
        Forecast = forecast;
        Season = season;
    }

    public ForecastDataset Clone()
    {
        return this with { Train = Train.Clone(), Forecast = Forecast.Clone() };
    }

    public int TsLen() => Train.RowCount;

    public int TsCount() => Train.ColumnCount;

    public (string, double)[] BasicDump()
    {
        List<(string, double)> dump = new()
        {
            ("rows", TsLen()),
            ("columns", TsCount()),
            ("test-len", Forecast.Count),
            ("season", Season)
        };

        return dump.ToArray();
    }

    public (double[], Vector<double>[]) AdvancedDump(string metric, bool legacy = true)
    {
        Matrix<double> ts = Train;
        double[] corr = Enumerable.Range(1, ts.ColumnCount - 1).Select(i => Correlation.Pearson(ts.Column(0), ts.Column(i))).ToArray();

        return (corr, Train.EnumerateColumns().Take(3).ToArray());
    }

    public (string, double)[] Characterization() =>
        BasicDump().Union(MultivarCharacterization.ExtractDatasetFeatures(this)).ToArray();

    public Vector<double> GetDownstream() => Forecast;

    public Vector<double> GetUpstream(ForecastConfig config, IEnumerable<MissingBlock> missingBlocks)
        => TimeSeries.FormMonolithicBlocks(Train, missingBlocks.ToArray());

    public void ContaminateData(ForecastConfig config, ScenarioMultivariate scen, int tick)
    {
        Matrix<double> matrix = Train;
        TimeSeries.RemoveBlocks(ref matrix, scen.GetContamination(TsLen(), TsCount(), tick, config.McarSeed));
        Train = matrix;
    }

    public void RecoverData(ForecastConfig config, Algorithm alg)
    {
        Matrix<double> matrix = Train;
        alg.RecoverMatrix(ref matrix);
        Train = matrix;
    }

    public (long, Vector<double>) RunDownstream(ForecastConfig config, string downAlgo, int slot)
    {
        return Forecasting.RunForecast(Train, Season, Forecast.Count, downAlgo, slot);
    }
}

//
// Univariate clustering data types
//
public readonly struct UniClusterDataset : IDataset<UniClusterDataset, UniClusterConfig, ScenarioUnivariate, int[][]>
{
    public string Data { get; }
    public List<string> Headers { get; }
    public Vector<double>[] Series { get; private init; }
    public int[] Classes { get; private init; }
    public int ClassCount { get; }

    public UniClusterDataset(UnivarDataset uniVarDataset)
    {
        Data = uniVarDataset.Data;
        Headers = uniVarDataset.Headers;
        
        Series = uniVarDataset.Train
            .Select(ts => ts.Vector)
            .Concat(uniVarDataset.Test
                    .Select(ts => ts.Vector))
            .ToArray();

        Dictionary<string, int> dict = new();
        Classes = new int[Series.Length];

        for (int i = 0; i < uniVarDataset.Train.Count; i++)
        {
            UnivarSeries series = uniVarDataset.Train[i];
            dict.TryAdd(series.TrueClass, dict.Count);
            Classes[i] = dict[series.TrueClass];
        }

        for (int i = 0; i < uniVarDataset.Test.Count; i++)
        {
            UnivarSeries series = uniVarDataset.Test[i];
            dict.TryAdd(series.TrueClass, dict.Count);
            Classes[i + uniVarDataset.Train.Count] = dict[series.TrueClass];
        }
        
        ClassCount = dict.Count;
    }

    public UniClusterDataset(string data, List<string> headers, Vector<double>[] series, int classCount)
    {
        Data = data;
        Headers = headers;
        Series = series;
        Classes = new int[series.Length];
        ClassCount = classCount;
    }

    public UniClusterDataset Clone()
    {
        Vector<double>[] deepCopySeries = Series.Select(v => v.Clone()).ToArray();
        int[] deepCopyClasses = Classes.Select(i => i).ToArray();
        return this with {Series = deepCopySeries, Classes = deepCopyClasses};
    }

    public int TsLen() => Series.First().Count;

    public int TsCount() => Series.Length;

    public (string, double)[] BasicDump()
    {
        List<(string, double)> dump = new()
        {
            ("classes", ClassCount),
            ("samples", Series.Length),
            ("rows", Series.First().Count)
        };

        return dump.ToArray();
    }

    public (double[], Vector<double>[]) AdvancedDump(string metric, bool legacy = true)
    {
        Matrix<double> ts;
        int[] classes = Classes; //linq

        if (metric.ToLower() == "inclass")
        {
            ts = Series.Select((e, i) => (e, classes[i])).GroupBy(e => e.Item2).First().Select(group => group.e).Take(10).ToMatrix();
        }
        else
        {
            ts = Series.Select((e, i) => (e, classes[i])).GroupBy(e => e.Item2).Select(group => group.First().e).ToMatrix();
        }
        
        double[] corr = Enumerable.Range(1, ts.ColumnCount - 1).Select(i => Correlation.Pearson(ts.Column(0), ts.Column(i))).ToArray();

        return (corr, ts.EnumerateColumns().Take(3).ToArray());
    }

    public (string, double)[] Characterization()
        => throw new NotImplementedException();

    public int[][] GetDownstream() => Classes.Select(cl => new[] {cl}).ToArray();

    public Vector<double> GetUpstream(UniClusterConfig config, IEnumerable<MissingBlock> missingBlocks)
        => TimeSeries.FormMonolithicBlocks(Series, missingBlocks.ToArray());

    public void ContaminateData(UniClusterConfig config, ScenarioUnivariate scen, int tick)
    {
        foreach (MissingBlock block in scen.GetContamination(TsLen(), TsCount(), tick, config.McarSeed))
        {
            TimeSeries.RemoveBlock(ref Series[block.Column], block);
        }
    }

    public void RecoverData(UniClusterConfig config, Algorithm alg)
    {
        alg.RecoverDataset(this);
    }

    public (long, int[][]) RunDownstream(UniClusterConfig config, string downAlgo, int slot)
    {
        return UnivariateClustering.RunClustering(this, downAlgo, config.Runs);
    }
    
    public IEnumerable<string> ToSkTimeLine()
    {
        foreach (string header in Headers)
        {
            yield return header;
        }

        foreach (Vector<double> vec in Series)
        {
            StringBuilder sb = new();
            for (int i = 0; i < vec.Count - 1; i++)
            {
                sb.Append(vec.Storage[i] + ",");
            }
            sb.Append(vec.Storage[vec.Count - 1] + ":0");
            yield return sb.ToString();
        }
    }
    
    public static Vector<double> FromSktimeNoClass(string skTimeLine)
    {
        string[] entry = skTimeLine.Split(":");
        string[] series = entry[0].Split(',');
        int len = series.Length;

        Vector<double> vec = MathX.Zeros(len);

        for (int j = 0; j < len; j++)
        {
            vec[j] = Double.Parse(series[j]);
        }

        return vec;
    }
}


//
// Multivariate classification data types
//

public readonly struct MultivarDataset : IDataset<MultivarDataset, MvClassConfig, ScenarioMultivariate, string[]>
{
    public string Data { get; }
    public List<string> Headers { get; }
    public List<MultivarSeries> Train { get; private init; }
    public List<MultivarSeries> Test { get; private init; }

    public MultivarDataset(string data, List<string> headers, List<MultivarSeries> train, List<MultivarSeries> test)
    {
        Headers = headers;
        Data = data;
        Train = train;
        Test = test;
    }

    public MultivarDataset Clone() =>
        this with
        {
            Train = Train.Select(x => x.Clone()).ToList(),
            Test = Test.Select(x => x.Clone()).ToList()
        };

    public int TsLen() => Train.First().Matrix.RowCount;

    public int TsCount() => Train.First().Matrix.ColumnCount;

    public (string, double)[] BasicDump()
    {
        List<(string, double)> dump = new()
        {
            ("classes", Train.Select(x => x.TrueClass).Distinct().Count()),
            ("samples_train", Train.Count),
            ("samples_test", Test.Count),
            ("rows", Train.First().Matrix.RowCount),
            ("columns", Train.First().Matrix.ColumnCount)
        };

        return dump.ToArray();
    }

    public (double[], Vector<double>[]) AdvancedDump(string metric, bool legacy = true)
    {
        Matrix<double> ts;
        
        if (metric.ToLower() == "inclass")
        {
            ts = Train.GroupBy(t => t.TrueClass).First().Select(group => group.Matrix.Column(0)).Take(10).ToMatrix();
        }
        else if (metric.ToLower() == "inmatrix")
        {
            ts = Train.First().Matrix;
        }
        else
        {
            ts = Train.GroupBy(t => t.TrueClass).Select(group => group.First().Matrix.Column(0)).ToMatrix();
        }
        
        double[] corr = Enumerable.Range(1, ts.ColumnCount - 1).Select(i => Correlation.Pearson(ts.Column(0), ts.Column(i))).ToArray();

        return (corr, ts.EnumerateColumns().Take(3).ToArray());
    }

    public (string, double)[] Characterization()
        => throw new NotImplementedException();

    public string[] GetDownstream() => Test.Select(x => x.TrueClass).ToArray();

    public Vector<double> GetUpstream(MvClassConfig config, IEnumerable<MissingBlock> missingBlocks)
    {
        List<Vector<double>> vec = new();
        
        foreach (MissingBlock block in missingBlocks)
        {
            if (config.ContaminateTrainSet)
            {
                vec.AddRange(Train.Select(series => series.Matrix.GetBlock(block)));
            }
            if (config.ContaminateTestSet)
            {
                vec.AddRange(Test.Select(series => series.Matrix.GetBlock(block)));
            }
        }

        return Vector<double>.Build.Dense(vec.Flatten().ToArray());
    }

    public void ContaminateData(MvClassConfig config, ScenarioMultivariate scen, int tick)
    {
        int testCount = Test.Count;
        
        if (config.ContaminateTrainSet)
        {
            Train.ForEach(
                ts => ts.Contaminate(scen.GetContamination(ts.Matrix.RowCount, ts.Matrix.ColumnCount, tick, config.McarSeed))
            );
        }

        if (config.ContaminateTestSet)
        {
            Test.ForEach(
                ts => ts.Contaminate(scen.GetContamination(ts.Matrix.RowCount, ts.Matrix.ColumnCount, tick, config.McarSeed))
            );
        }

        // sanity check: contamination process
        // [!] exceptionally here and only covers test set, the rest (train & len) is done outside
        if (Test.Count != testCount || Train.First().Matrix.ColumnCount != Test.First().Matrix.ColumnCount || Train.First().Matrix.RowCount != Test.First().Matrix.RowCount)
        {
            Console.WriteLine("Mismatch! Contamination process altered dataset structure. Aborting.");
            Environment.Exit(-1);
        }
    }

    public void RecoverData(MvClassConfig config, Algorithm alg)
    {
        alg.RecoverDataset(this);
    }

    public (long, string[]) RunDownstream(MvClassConfig config, string downAlgo, int slot)
    {
        return MultivariateClassification.RunClassification(this, downAlgo, slot);
    }
}
public class MultivarSeries
{
    // internals & building
    public Matrix<double> Matrix;
    public readonly string TrueClass;

    private MultivarSeries(Matrix<double> matrix, string trueClass)
    {
        Matrix = matrix;
        TrueClass = trueClass.ToLower();
    }

    public static MultivarSeries FromSktime(string skTimeLine)
    {
        string[] mxCols = skTimeLine.Split(":");
        int columns = mxCols.Length - 1;
        string[] row = mxCols[0].Split(',');
        int rows = row.Length;

        var mat = MathX.Zeros(rows, columns);

        for (int i = 0; i < columns; i++)
        {
            if (i != 0) row = mxCols[i].Split(',');

            if (rows != row.Length)
                Console.WriteLine("FAIL");

            for (int j = 0; j < rows; j++)
            {
                mat[j, i] = Double.Parse(row[j]);
            }
        }

        return new MultivarSeries(mat, mxCols[columns]);
    }

    // operations & output
    public void Contaminate(IEnumerable<MissingBlock> blocks)
    {
        TimeSeries.RemoveBlocks(ref Matrix, blocks);
    }

    public string ToSkTimeLine()
    {
        StringBuilder sb = new();

        for (int j = 0; j < Matrix.ColumnCount; j++)
        {
            for (int i = 0; i < Matrix.RowCount - 1; i++)
            {
                sb.Append(Matrix.Storage[i, j] + ",");
            }
            sb.Append(Matrix.Storage[Matrix.RowCount - 1, j] + ":");
        }
        sb.Append(TrueClass);

        return sb.ToString();
    }

    public MultivarSeries Clone() => new (Matrix.Clone(), TrueClass);
}

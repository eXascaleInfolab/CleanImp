using System;
using System.Collections.Generic;
using System.Linq;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class StmvlAlgorithm : Algorithm
{
    public override string AlgCodeBase => "STMVL";
    protected override string Suffix => "";
        
    // algo params
    private const double Alpha = 2.0;
    private const double Gamma = 0.85;
    private const int WindowSize = 7;

    public StmvlAlgorithm()
    {
        UseParallel = Algorithm.ParallelFull;
    }
    
    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        StmvlInternal stmvl = new(input, Alpha, Gamma, WindowSize);

        input = stmvl.Run();
    }
}
    
internal class StmvlInternal
{
    // fields

    private readonly int _rowCount;
    private readonly int _columnCount;
    private readonly double _alpha;
    private readonly double _gamma;
    private readonly int _windowSize;

    private readonly Matrix<double> _missingMatrix;
    private readonly Matrix<double> _predictMatrix;
    private readonly Matrix<double> _temporaryMatrix;

    private readonly Dictionary<ValueTuple<int, int>, double> _distanceDic;
    private readonly Dictionary<int, List<int>> _stationDic;

    private const double EarthRadius = 6378137.0; //earth radius
    private const int TemporalThreshold = 5;
    private const int ViewCount = 4;
        
    // constructor
    internal StmvlInternal(Matrix<double> matrix, double alpha, double gamma, int windowSize)
    {
        _rowCount = matrix.RowCount;
        _columnCount = matrix.ColumnCount;
            
        _alpha = alpha;
        _gamma = gamma;
        _windowSize = windowSize;
        _predictMatrix = matrix; //ref
        _missingMatrix = matrix.Clone();
        _temporaryMatrix = matrix.Clone();

        double[] latitude = new double[_columnCount];
        double[] longitude = new double[_columnCount];

        decimal lat = new(39.954047);
        decimal lng = new(116.348991);

        for (int i = 0; i < _columnCount; i++)
        {
            latitude[i] = (double) lat;
            longitude[i] = (double) lng;

            if (i % 2 == 0)
            {
                lat += new Decimal(0.01);
            }
            else
            {
                lng += new Decimal(0.01);   
            }
        }

        _distanceDic = new Dictionary<ValueTuple<int, int>, double>();
        _stationDic = new Dictionary<int, List<int>>();

        for (int i = 0; i < _columnCount; i++)
        {
            Dictionary<int, double> distanceDicTemp = new Dictionary<int, double>();

            for (int j = 0; j < _columnCount; j++)
            {
                if (i != j)
                {
                    double dis = GeoDistance(latitude[i], longitude[i], latitude[j], longitude[j]);
                    _distanceDic.Add(new ValueTuple<int, int>(i, j), dis);
                    distanceDicTemp.Add(j, dis);
                }
            }

            _stationDic.Add(i, new List<int>());

            foreach (var p in distanceDicTemp)
            {
                _stationDic[i].Add(p.Key);
            }
        }
    }
        
    // functions
    public Matrix<double> Run()
    {
        // process
        InitializeMissing();
            
        DoStmvl(FourView(_columnCount, GenerateTrainingCase()));

        return _predictMatrix;
    }

    private double ComputeSpatialWeight(double dis)
    {
        double ws = Math.Pow((1 / dis), _alpha);
        return ws;
    }

    private double ComputeTemporalWeight(int timespan)
    {
        double wt = _gamma * Math.Pow((1 - _gamma), (timespan - 1));
        return wt;
    }
        
    private void DoStmvl(Matrix<double> equation)
    {
        for (int i = 0; i < _rowCount; i++)
        {
            for (int j = 0; j < _columnCount; j++)
            {
                if (Double.IsNaN(_missingMatrix[i, j]))
                {
                    Mvl(i, j, equation);
                }
            }
        }

        // predictMatrix already contains the imputed values
    }

    private void InitializeMissing()
    {
        for (int i = 0; i < _rowCount; i++)
        {
            for (int j = 0; j < _columnCount; j++)
            {
                if (Double.IsNaN(_missingMatrix[i, j]))
                {
                    GlobalViewCombine(i, j);
                }
            }
        }
    }

    private void GlobalViewCombine(int i, int j)
    {
        double resultIdw = Idw(i, j, _missingMatrix);
        double resultSes = Ses(i, j, _missingMatrix);

        if (!Double.IsNaN(resultIdw) && !Double.IsNaN(resultSes))
        {
            _temporaryMatrix[i, j] = (resultIdw + resultSes) / 2; // simple combine equally.
        }
        else if (!Double.IsNaN(resultSes))
        {
            _temporaryMatrix[i, j] = resultSes;
        }
        else if (!Double.IsNaN(resultIdw))
        {
            _temporaryMatrix[i, j] = resultIdw;
        }
    }

    private void Mvl(int i, int j, Matrix<double> equation)
    {
        double resultUcf = Ucf(i, j, _temporaryMatrix);
        double resultIcf = Icf(i, j, _temporaryMatrix);
        double resultIdw = Idw(i, j, _temporaryMatrix);
        double resultSes = Ses(i, j, _temporaryMatrix);

        if (!Double.IsNaN(resultUcf) && !Double.IsNaN(resultIcf) && !Double.IsNaN(resultIdw) && !Double.IsNaN(resultSes))
        {
            double result = equation[j, 0] * resultUcf + equation[j, 1] * resultIdw + equation[j, 2] * resultIcf + equation[j, 3] * resultSes + equation[j, 4];
            _predictMatrix[i, j] = result;
        }
        else
        {
            _predictMatrix[i, j] = _temporaryMatrix[i, j];
        }
    }

    private double Ucf(int ti, int tj, Matrix<double> dataMatrix)
    {
        Dictionary<double, double> candiDic = new Dictionary<double, double>();

        foreach (var jj in _stationDic[tj])
        {
            if (!Double.IsNaN(dataMatrix[ti, jj]))
            {
                double sim = CalUserEuclideanSim(tj, jj, ti, dataMatrix);

                if (sim != 0)
                {
                    if (candiDic.ContainsKey(dataMatrix[ti, jj]))
                    {
                        candiDic[dataMatrix[ti, jj]] += sim;
                    }
                    else
                    {
                        candiDic.Add(dataMatrix[ti, jj], sim);
                    }
                }
            }
        }

        double spatialPredict = Double.NaN;
        if (candiDic.Count != 0)
        {
            double weightSum = candiDic.Values.Sum();
            double tempSpatialPredict = 0;
            foreach (var p in candiDic)
            {
                tempSpatialPredict += p.Key * p.Value / weightSum;
            }
            spatialPredict = tempSpatialPredict;
        }

        return spatialPredict;
    }

    private double CalUserEuclideanSim(int tj, int jj, int ti, Matrix<double> dataMatrix)
    {
        double similarity = 0;
        double offset = 0;
        int nt = 0;

        int upRow = ti - _windowSize / 2;
        if (upRow < 0)
        {
            upRow = 0;
        }

        int downRow = ti + _windowSize / 2;
        if (downRow >= _rowCount)
        {
            downRow = _rowCount - 1;
        }

        for (int i = upRow; i < downRow; i++)
        {
            if (!Double.IsNaN(dataMatrix[i, tj]) && !Double.IsNaN(dataMatrix[i, jj]))
            {
                offset += Math.Pow((dataMatrix[i, tj] - dataMatrix[i, jj]), 2);
                nt++;
            }
        }

        if (nt > 0 && offset > 0)
        {
            double avgDis = Math.Sqrt(offset) / nt;
            similarity = 1 / (avgDis);
        }

        return similarity;
    }

    private double Icf(int ti, int tj, Matrix<double> dataMatrix)
    {
        Dictionary<double, double> candiDic = new Dictionary<double, double>();

        int upRow = ti - _windowSize / 2;
        if (upRow < 0)
        {
            upRow = 0;
        }

        int downRow = ti + _windowSize / 2;
        if (downRow >= _rowCount)
        {
            downRow = _rowCount - 1;
        }

        for (int ii = upRow; ii < downRow; ii++)
        {
            if (ii == ti)
                continue;

            if (!Double.IsNaN(dataMatrix[ii, tj]))
            {
                double sim = CalItemEuclideanSim(ti, ii, tj, dataMatrix);
                if (sim != 0)
                {
                    if (candiDic.ContainsKey(dataMatrix[ii, tj]))
                    {
                        candiDic[dataMatrix[ii, tj]] += sim;
                    }
                    else
                    {
                        candiDic.Add(dataMatrix[ii, tj], sim);
                    }
                }
            }
        }

        double temporalPredict = Double.NaN;
        if (candiDic.Count != 0)
        {
            double weightSum = candiDic.Values.Sum();
            double tempTemporalPredict = 0;
            foreach (var p in candiDic)
            {
                tempTemporalPredict += p.Key * p.Value / weightSum;
            }
            temporalPredict = tempTemporalPredict;
        }

        return temporalPredict;
    }

    private double CalItemEuclideanSim(int ti, int ii, int tj, Matrix<double> dataMatrix)
    {
        double similarity = 0;
        double offset = 0;
        int ns = 0;

        foreach (var jj in _stationDic[tj])
        {
            if (!Double.IsNaN(dataMatrix[ti, jj]) && !Double.IsNaN(dataMatrix[ii, jj]))
            {
                offset += Math.Pow((dataMatrix[ti, jj] - dataMatrix[ii, jj]), 2);
                ns++;
            }
        }

        if (ns > 0 && offset > 0)
        {
            double avgDis = Math.Sqrt(offset) / ns;
            similarity = 1 / (avgDis);
        }

        return similarity;
    }

    private double Ses(int ti, int tj, Matrix<double> dataMatrix)
    {
        Dictionary<double, double> candiDic = new Dictionary<double, double>();

        for (int i = 1; i <= TemporalThreshold; i++)
        {
            int ii = ti - i;
            if (ii >= 0 && !Double.IsNaN(dataMatrix[ii, tj]))
            {
                double weight = ComputeTemporalWeight(Math.Abs(i));
                double value = dataMatrix[ii, tj];

                if (candiDic.ContainsKey(value))
                {
                    candiDic[value] += weight;
                }
                else
                {
                    candiDic.Add(value, weight);
                }
            }

            ii = ti + i;
            if (ii < _rowCount && !Double.IsNaN(dataMatrix[ii, tj]))
            {
                double weight = ComputeTemporalWeight(Math.Abs(i));
                double value = dataMatrix[ii, tj];

                if (candiDic.ContainsKey(value))
                {
                    candiDic[value] += weight;
                }
                else
                {
                    candiDic.Add(value, weight);
                }
            }
        }

        double temporalPredict = Double.NaN;
        if (candiDic.Count > 0)
        {
            double weightSum = candiDic.Values.Sum();
            double tempTemporalPredict = 0;
            foreach (var q in candiDic)
            {
                tempTemporalPredict += q.Key * q.Value / weightSum;
            }
            temporalPredict = tempTemporalPredict;
        }
        return temporalPredict;
    }

    private double Idw(int ti, int tj, Matrix<double> dataMatrix)
    {
        Dictionary<double, double> candiDic = new Dictionary<double, double>();

        foreach (var jj in _stationDic[tj])
        {
            double dis = _distanceDic[new ValueTuple<int, int>(tj, jj)];
            if (!Double.IsNaN(dataMatrix[ti, jj]))
            {
                double weight = ComputeSpatialWeight(dis);
                double value = dataMatrix[ti, jj];

                if (candiDic.ContainsKey(value))
                {
                    candiDic[value] += weight;
                }
                else
                {
                    candiDic.Add(value, weight);
                }
            }
        }

        double spatialPredict = Double.NaN;
        if (candiDic.Count != 0)
        {
            double weightSum = candiDic.Values.Sum();
            double tempSpatialPredict = 0;
            foreach (var q in candiDic)
            {
                tempSpatialPredict += q.Key * q.Value / weightSum;
            }
            spatialPredict = tempSpatialPredict;
        }

        return spatialPredict;
    }

    private Matrix<double>[] GenerateTrainingCase()
    {
        Matrix<double>[] trainingMatrices = new Matrix<double>[_columnCount];
            
        for (int j = 0; j < _columnCount; j++)
        {
            int caseCount = 0;

            for (int i = 0; i < _rowCount; i++)
            {
                if (!Double.IsNaN(_missingMatrix[i, j]))
                {
                    if (CheckContextData(i, j) == 1)
                    {
                        caseCount++;
                    }
                }
            }

            trainingMatrices[j] = MathX.Zeros(caseCount, ViewCount + 1);
            int counter = 0;

            for (int i = 0; i < _rowCount; i++)
            {
                if (!Double.IsNaN(_missingMatrix[i, j]))
                {
                    if (CheckContextData(i, j) == 1)
                    {
                        OutputCase(i, j, trainingMatrices[j], counter);
                        counter++;
                    }
                }
            }
        }

        return trainingMatrices;
    }

    private int CheckContextData(int ti, int tj)
    {
        int count = 0;
        for (int j = 0; j < _columnCount; j++)
        {
            if (Double.IsNaN(_missingMatrix[ti, j]))
                count++;
        }
        if (count > (_columnCount / 2))
            return 0;

        int si = ti - _windowSize / 2;
        int ei = ti + _windowSize / 2;
        if (si < 0)
            si = 0;
        if (ei >= _rowCount)
            ei = _rowCount - 1;

        count = 0;

        for (int i = si; i <= ei; i++)
        {
            if (Double.IsNaN(_missingMatrix[i, tj]))
                count++;
        }

        if (count > ((ei - si + 1) / 2))
            return 0;

        return 1;
    }

    private void OutputCase(int i, int j, Matrix<double> trainingMatrix, int position)
    {
        double resultUcf = Ucf(i, j, _temporaryMatrix);
        double resultIcf = Icf(i, j, _temporaryMatrix);
        double resultIdw = Idw(i, j, _temporaryMatrix);
        double resultSes = Ses(i, j, _temporaryMatrix);

        if (!Double.IsNaN(resultUcf) && !Double.IsNaN(resultIcf) && !Double.IsNaN(resultIdw) && !Double.IsNaN(resultSes))
        {
            trainingMatrix[position, 0] = _missingMatrix[i, j];
            trainingMatrix[position, 1] = resultUcf;
            trainingMatrix[position, 2] = resultIdw;
            trainingMatrix[position, 3] = resultIcf;
            trainingMatrix[position, 4] = resultSes;
        }
        else // some case is recorded as training case, but not all views have results. //skip it, it do not have any bad influence.
        { } 
    }
        
    //SPT
    private static void Sqt2(Matrix<double> x, Vector<double> y, double[] a, double[] dt, double[] v)
    {
        int m = x.RowCount; //row count
        int n = x.ColumnCount; //column count
            
        //double p;
        double[] b = new double[(m + 1) * (m + 1)];
        int mm = m + 1;
        b[mm * mm - 1] = n;
        for (int j = 0; j <= m - 1; j++)
        {
            double p = 0.0;
            for (int i = 0; i <= n - 1; i++)
                p = p + x[j, i];
            b[m * mm + j] = p;
            b[j * mm + m] = p;
        }
        for (int i = 0; i <= m - 1; i++)
        for (int j = i; j <= m - 1; j++)
        {
            double p = 0.0;
            for (int k = 0; k <= n - 1; k++)
                p = p + x[i, k] * x[j, k];
            b[j * mm + i] = p;
            b[i * mm + j] = p;
        }
        a[m] = 0.0;
        for (int i = 0; i <= n - 1; i++)
            a[m] = a[m] + y[i];
        for (int i = 0; i <= m - 1; i++)
        {
            a[i] = 0.0;
            for (int j = 0; j <= n - 1; j++)
                a[i] = a[i] + x[i, j] * y[j];
        }
        Chlk(b, mm, 1, a);
        double yy = 0.0;
        for (int i = 0; i <= n - 1; i++)
        {
            yy = yy + y[i] / n;
        }
        double q = 0.0, e = 0.0, u = 0.0;
        for (int i = 0; i <= n - 1; i++)
        {
            double p = a[m];
            for (int j = 0; j <= m - 1; j++)
                p = p + a[j] * x[j, i];
            q = q + (y[i] - p) * (y[i] - p);
            e = e + (y[i] - yy) * (y[i] - yy);
            u = u + (yy - p) * (yy - p);
        }
        double s = Math.Sqrt(q / n);
        double r = Math.Sqrt(1.0 - q / e);
        for (int j = 0; j <= m - 1; j++)
        {
            double p = 0.0;
            for (int i = 0; i <= n - 1; i++)
            {
                double pp = a[m];
                for (int k = 0; k <= m - 1; k++)
                    if (k != j)
                        pp = pp + a[k] * x[k, i];
                p = p + (y[i] - pp) * (y[i] - pp);
            }
            v[j] = Math.Sqrt(1.0 - q / p);
        }
        dt[0] = q;
        dt[1] = s;
        dt[2] = r;
        dt[3] = u;
    }

    private static void Chlk(double[] a, int n, int m, double[] d)
    {
        int i, j, k, u, v;
        if (a[0] < Single.Epsilon)
        {
            Console.WriteLine("fail");
        }
        a[0] = Math.Sqrt(a[0]);
        for (j = 1; j <= n - 1; j++)
            a[j] = a[j] / a[0];
        for (i = 1; i <= n - 1; i++)
        {
            u = i * n + i;
            for (j = 1; j <= i; j++)
            {
                v = (j - 1) * n + i;
                a[u] = a[u] - a[v] * a[v];
            }
            if (a[u] < Single.Epsilon)
            {
                Console.WriteLine("fail");
            }
            a[u] = Math.Sqrt(a[u]);
            if (i != (n - 1))
            {
                for (j = i + 1; j <= n - 1; j++)
                {
                    v = i * n + j;
                    for (k = 1; k <= i; k++)
                        a[v] = a[v] - a[(k - 1) * n + i] * a[(k - 1) * n + j];
                    a[v] = a[v] / a[u];
                }
            }
        }
        for (j = 0; j <= m - 1; j++)
        {
            d[j] = d[j] / a[0];
            for (i = 1; i <= n - 1; i++)
            {
                u = i * n + i;
                v = i * m + j;
                for (k = 1; k <= i; k++)
                    d[v] = d[v] - a[(k - 1) * n + i] * d[(k - 1) * m + j];
                d[v] = d[v] / a[u];
            }
        }
        for (j = 0; j <= m - 1; j++)
        {
            u = (n - 1) * m + j;
            d[u] = d[u] / a[n * n - 1];
            for (k = n - 1; k >= 1; k--)
            {
                u = (k - 1) * m + j;
                for (i = k; i <= n - 1; i++)
                {
                    v = (k - 1) * n + i;
                    d[u] = d[u] - a[v] * d[i * m + j];
                }
                v = (k - 1) * n + k - 1;
                d[u] = d[u] / a[v];
            }
        }
    }
        
    // Geo stuff
    private static double RadToDeg(double d)
    {
        return d * Math.PI / 180.0;
    }

    private static double GeoDistance(double lat1, double lng1, double lat2, double lng2)
    {
        double radLat1 = RadToDeg(lat1);
        double radLat2 = RadToDeg(lat2);
        double a = radLat1 - radLat2;
        double b = RadToDeg(lng1) - RadToDeg(lng2);

        double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2)));
        s *= EarthRadius;
        return s;
    }
    
    // FourView
    private static Matrix<double> FourView(int sensorCount, Matrix<double>[] trainingMatrices)
    {
        Matrix<double> equation = MathX.Zeros(sensorCount, ViewCount + 1);

        for (int j = 0; j < sensorCount; j++)
        {
            double[] a = new double[ViewCount + 1];
            double[] v = new double[ViewCount];
            double[] dt = new double[ViewCount];
            Matrix<double> x = trainingMatrices[j].SubMatrix(
                0, trainingMatrices[j].RowCount,
                1, trainingMatrices[j].ColumnCount - 1
            ).Transpose();
            Vector<double> y = trainingMatrices[j].Column(0);

            Sqt2(x, y, a, dt, v);

            for (int i = 0; i < a.Length; i++)
            {
                equation[j, i] = a[i];
            }
        }

        return equation;
    }
}
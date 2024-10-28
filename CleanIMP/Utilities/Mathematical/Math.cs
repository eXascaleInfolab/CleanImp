using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

namespace CleanIMP.Utilities.Mathematical;

/// <summary>
/// A static class for math utility functions (relying on MathNet types).
/// </summary>
public static class MathX
{
    //
    // Basics and constructors
    //
    
    public const double ZeroEps = 1E-7;
    
    public static Matrix<double> Zeros(int n, int m) => Matrix<double>.Build.Dense(n, m);
    
    public static Vector<double> Zeros(int n) => Vector<double>.Build.Dense(n);
    public static Vector<double> Ones(int n) => Vector<double>.Build.Dense(n, 1.0);
    
    public static Matrix<double> Identity(int n, int m) => Matrix<double>.Build.DenseIdentity(n ,m);

    //
    // Converters
    //
    
    public static Matrix<double> ToMatrix(this IEnumerable<Vector<double>> columns) =>
        Matrix<double>.Build.DenseOfColumnVectors(columns);

    public static int AsInt(double number)
    {
        return (int) Math.Round(number);
    }

    //
    // Parsers
    //

    public static class Parse
    {
        public static int[] ParseRowInt(string inputRow)
            => inputRow.Split(' ').Select(Int32.Parse).ToArray();

        public static double[][] ParseMatrix(IEnumerable<string> input)
        {
            double[][] listMatrix = input.WhereNOT(String.IsNullOrEmpty).Select(x => x.Split(' ').Select(Double.Parse).ToArray()).ToArray();
        
            return listMatrix;
        }

        public static int[][] ParseIntMatrix(IEnumerable<string> input)
        {
            int[][] listMatrix = input.WhereNOT(String.IsNullOrEmpty).Select(Parse.ParseRowInt).ToArray();
        
            return listMatrix;
        }
    }

    //
    // Read-writers
    //
    
    public static Matrix<double> LoadMatrixFile(string file)
    {
        double[][] listMatrix = Parse.ParseMatrix(IOTools.EnumerateAllLines(file));
        
        return Matrix<double>.Build.DenseOfRowArrays(listMatrix);
    }
    
    public static Vector<double> LoadVectorFile(string file)
    {
        double[] listMatrix = IOTools.EnumerateAllLines(file).Select(Double.Parse).ToArray();
        
        return Vector<double>.Build.DenseOfArray(listMatrix);
    }
    
    public static Vector<double> LoadVectorFile(string file, int count)
    {
        double[] listMatrix = IOTools.EnumerateAllLines(file).Select(Double.Parse).Take(count).ToArray();
        
        return Vector<double>.Build.DenseOfArray(listMatrix);
    }
    
    public static IEnumerable<string> ExportMx(this Matrix<double> matrix)
    {
        for (int i = 0; i < matrix.RowCount; i++)
        {
            yield return matrix.Row(i).StringJoin();
        }
    }

    public static IEnumerable<string> ExportVec(this Vector<double> matrix) =>
        matrix.Select(x => x.ToString(CultureInfo.InvariantCulture));
    
    //
    // Combinatorics
    //
    
    public static int CountUniquePairs(long n) => (int)((n * (n - 1)) / 2);
    public static int PairToFlatIndexUnique(int i, int j, int n) => SumN(n - 1) - SumN(n - i - 1) + j - i - 1;
    public static int SumN(long n) => (int)(n * (n + 1) / 2);

    //
    // Get-Set-ers
    //
    
    public static Matrix<double> FromRowIndices(Matrix<double> mat, ICollection<int> indices)
    {
        Matrix<double> result = Zeros(indices.Count, mat.ColumnCount);

        for (int i = 0; i < indices.Count; i++)
        {
            result.SetRow(i, mat.Row(indices.ElementAt(i)));
        }

        return result;
    }
    
    public static Vector<double> FromIndices(Vector<double> vec, ICollection<int> indices)
    {
        Vector<double> result = Zeros(indices.Count);

        for (int i = 0; i < indices.Count; i++)
        {
            result[i] = vec[indices.ElementAt(i)];
        }

        return result;
    }

    public static void SetAtIndices(Vector<double> vec, ICollection<int> indices, Vector<double> data)
    {
        for (int i = 0; i < indices.Count; i++)
        {
            vec[indices.ElementAt(i)] = data[i];
        }
    }

    //
    // Stats
    //
    
    public static double SampleStddev(this ICollection<double> sample, double mean)
    {
        double stddev = sample.Sum(val => (val - mean) * (val - mean));
        return Math.Sqrt(stddev / (sample.Count - 1));
    }

    public static (double, double) BatchMeanStddev(Vector<double> vector)
    {
        (double mean, double stddev) = vector.MeanStandardDeviation();

        if (Double.IsNaN(stddev)) stddev = 1.0;
        if (stddev < ZeroEps) stddev = 1.0;
        
        return (mean, stddev);
    }

    public static (double[], double[]) BatchMeanStddev(Matrix<double> matrix)
    {
        double[] mean = new double[matrix.ColumnCount];
        double[] stddev = new double[matrix.ColumnCount];

        for (int j = 0; j < matrix.ColumnCount; ++j)
        {
            (mean[j], stddev[j]) = matrix.Column(j).MeanStandardDeviation();

            if (Double.IsNaN(stddev[j])) stddev[j] = 1.0;
            if (stddev[j] < ZeroEps) stddev[j] = 1.0;
        }
        
        return (mean, stddev);
    }
        
    public static void NormalizeMatrix(ref Matrix<double> matrix, double[] mean, double[] stddev)
    {
        for (int i = 0; i < matrix.RowCount; ++i)
        {
            for (int j = 0; j < matrix.ColumnCount; ++j)
            {
                matrix[i, j] = (matrix[i, j] - mean[j]) / stddev[j];
            }
        }
    }

    public static void NormalizeVector(ref Vector<double> vector, double mean, double stddev)
    {
        for (int j = 0; j < vector.Count; ++j)
        {
            vector[j] = (vector[j] - mean) / stddev;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using Accord.Statistics.Kernels;
using CleanIMP.Testing;
using CleanIMP.Utilities.Mathematical.ThirdPartyPorts;
using MathNet.Numerics.Statistics;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Utilities.Mathematical;

/// <summary>
/// A static class for time series utility functions (relying on MathNet types).
/// </summary>
public static class TimeSeries
{
    public static List<MissingBlock> DetectMissingBlocks(this Matrix<double> matrix)
    {
        List<MissingBlock> blocks = new();

        for (int j = 0; j < matrix.ColumnCount; ++j)
        {
            bool missingBlock = false;
            int start = 0;

            for (int i = 0; i < matrix.RowCount; ++i)
            {
                if (Double.IsNaN(matrix[i, j]))
                {
                    if (!missingBlock)
                    {
                        missingBlock = true;
                        start = i;
                    }
                }
                else
                {
                    if (missingBlock)
                    {
                        //finalize block
                        missingBlock = false;
                        blocks.Add(new MissingBlock(j, start, i - start));
                    }
                }
            }

            if (missingBlock)
            {
                blocks.Add(new MissingBlock(j, start, matrix.RowCount - start));
            }
        }

        return blocks;
    }

    public static void RemoveBlocks(ref Matrix<double> matrix, IEnumerable<MissingBlock> missingBlocks)
    {
        foreach (var block in missingBlocks)
        {
            for (int i = block.Start; i < block.Start + block.Length; i++)
            {
                matrix[i, block.Column] = Double.NaN;
            }
        }
    }

    public static void RemoveBlock(ref Vector<double> vector, MissingBlock block)
    {
        for (int i = block.Start; i < block.Start + block.Length; i++)
        {
            vector[i] = Double.NaN;
        }
    }

    public static int MissingTotal(this IEnumerable<MissingBlock> missingBlocks) => missingBlocks.Sum(mb => mb.Length);

    public static Vector<double> GetBlock(this Matrix<double> mat, MissingBlock block)
    {
        Vector<double> vec = MathX.Zeros(block.Length);
    
        for (int i = 0; i < block.Length; i++)
        {
            vec[i] = mat[block.Start + i, block.Column];
        }
    
        return vec;
    }

    public static Vector<double> GetBlock(this Vector<double> vec, MissingBlock block)
    {
        Vector<double> result = MathX.Zeros(block.Length);
    
        for (int i = 0; i < block.Length; i++)
        {
            result[i] = vec[block.Start + i];
        }
    
        return vec;
    }

    public static void SetBlock(ref Matrix<double> mat, MissingBlock block, Vector<double> vec)
    {
        for (int i = 0; i < block.Length; i++)
        {
            mat[block.Start + i, block.Column] = vec[i];
        }
    }
    
    public static Vector<double> FormMonolithicBlocks(Matrix<double> reference, MissingBlock[] blocks)
    {
        if (blocks.Length == 1)
        {
            return reference.GetBlock(blocks[0]);
        }
        
        int total = blocks.Sum(b => b.Length);
        double[] refBlocks = new double[total];

        int current = 0;
        foreach (MissingBlock block in blocks)
        {
            for (int i = block.Start; i < block.Start + block.Length; i++)
            {
                refBlocks[current] = reference[i, block.Column];
                current++;
            }
        }

        return Vector<double>.Build.Dense(refBlocks);
    }

    public static IEnumerable<MissingBlock> MergeConsecutive(List<MissingBlock> blocks)
    {
        blocks.Sort((b1, b2) =>
        {
            if (b1.Column < b2.Column) return -1;
            if (b1.Column > b2.Column) return 1;
            // forcefully b1.c == b2.c
            if (b1.Start < b2.Start) return -1;
            if (b1.Start > b2.Start) return 1;
            return 0; // forcefully b1.s == b2.s
        });
        
        // merge consecutive blocks
        for (int i = 0; i < blocks.Count; i++)
        {
            // check for last element to ensure we always have 1 look-ahead
            // check if it's not the next column - no merger is possible between different columns
            if (i == blocks.Count - 1 || blocks[i].Column != blocks[i+1].Column)
            {
                yield return blocks[i];
                continue;
            }

            (MissingBlock mb, MissingBlock next) = (blocks[i], blocks[i + 1]);
            
            // from here on we know that 1) mb/next are in the same column 2) mb.start < next.start because sorted
            if (next.Start == mb.Start + mb.Length)
            {
                // merge
                blocks[i] = new MissingBlock(mb.Column, mb.Start, mb.Length + next.Length);
                blocks.RemoveAt(i + 1);
                i--; // this will make it process the same block again
            }
            else
            {
                yield return mb;
            }
        }
    }
    
    public static Vector<double> FormMonolithicBlocks(Vector<double>[] reference, MissingBlock[] blocks)
    {
        if (blocks.Length == 1)
        {
            return reference[blocks[0].Column].GetBlock(blocks[0]);
        }
        
        int total = blocks.Sum(b => b.Length);
        double[] refBlocks = new double[total];

        int current = 0;
        foreach (MissingBlock block in blocks)
        {
            for (int i = block.Start; i < block.Start + block.Length; i++)
            {
                refBlocks[current] = reference[block.Column][i];
                current++;
            }
        }

        return Vector<double>.Build.Dense(refBlocks);
    }

    public static Matrix<double> SimilarityMatrix(UnivarDataset dataset, string metric = "default")
    {
        int series = dataset.Train.Count;

        Matrix<double> correlations = MathX.Zeros(series, series);

        for (int i = 0; i < series; i++)
        {
            for (int j = i + 1; j < series; j++)
            {
                //double corr = Metric(dataset.Series[i], dataset.Series[j], "pearson");
                double corr = SimilarityMeasure(dataset.Train[i].Vector, dataset.Train[j].Vector, metric);
                correlations[i, j] = correlations[j, i] = corr;
            }
        }

        return correlations;
    }

    public static Matrix<double> SimilarityMatrix(Matrix<double> dataset, string metric = "default")
    {
        int series = dataset.ColumnCount;

        Matrix<double> correlations = MathX.Zeros(series, series);

        for (int i = 0; i < series; i++)
        {
            for (int j = i + 1; j < series; j++)
            {
                double corr = SimilarityMeasure(dataset.Column(i), dataset.Column(j), metric);
                correlations[i, j] = correlations[j, i] = corr;
            }
        }

        return correlations;
    }

    public static double SimilarityMeasure(Vector<double> series1, Vector<double> series2, string metric = "default")
    {
        switch (metric)
        {
            case "pearson":
            case "default":
                double pearson = Correlation.Pearson(series1, series2);
                return pearson;
            
            case "dtw":
                DynamicTimeWarping dtw = new DynamicTimeWarping(series1.Count / 10);
                double dist = dtw.Distance(series1.AsArray() ?? series1.ToArray(),
                    series2.AsArray() ?? series2.ToArray());
                return -dist;
            
            case "sbd":
                return KShape.ShapeBasedSimilarity(series1, series2);
            
            default:
                throw new ArgumentException("Unknown TS similarity metric");
        }
    }
}

public readonly struct MissingBlock
{
    public readonly int Column;
    public readonly int Start;
    public readonly int Length;

    public MissingBlock(int column, int start, int length)
    {
        Column = column;
        Start = start;
        Length = length;
    }

    public static implicit operator MissingBlock(ValueTuple<int, int, int> tuple) => new(tuple.Item1, tuple.Item2, tuple.Item3);
    
    public IEnumerable<int> RowIndices => Enumerable.Range(Start, Length);

    public override string ToString() => $"({Column}, {Start}, {Length})";
}
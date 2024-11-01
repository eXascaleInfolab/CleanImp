using System;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class MeanImputeAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "MeanImp";
    protected override string Suffix => "";

    public MeanImputeAlgorithm()
    {
        UseParallel = Algorithm.ParallelFull;
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        double[] columnMean = new double[input.ColumnCount];
        
        for (int j = 0; j < input.ColumnCount; j++)
        {
            double mean = 0.0;
            int cnt = 0;
            for (int i = 0; i < input.RowCount; i++)
            {
                if (Double.IsFinite(input[i, j]))
                {
                    mean += input[i, j];
                    cnt++;
                }
            }

            columnMean[j] = mean / cnt;
        }

        for (int j = 0; j < input.ColumnCount; j++)
        {
            for (int i = 0; i < input.RowCount; i++)
            {
                if (!Double.IsFinite(input[i, j]))
                {
                    input[i, j] = columnMean[j];
                }
            }
        }
    }
}
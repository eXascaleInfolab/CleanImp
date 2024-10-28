using System;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class HorizontalMeanImputeAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "HMeanImp";
    protected override string Suffix => "";

    public HorizontalMeanImputeAlgorithm()
    {
        UseParallel = Algorithm.ParallelFull;
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        double[] rowMean = new double[input.RowCount];

        for (int i = 0; i < input.RowCount; i++)
        {
            double mean = 0.0;
            int cnt = 0;
            for (int j = 0; j < input.ColumnCount; j++)
            {
                if (Double.IsFinite(input[i, j]))
                {
                    mean += input[i, j];
                    cnt++;
                }
            }

            rowMean[i] = mean / cnt;
        }

        for (int j = 0; j < input.ColumnCount; j++)
        {
            for (int i = 0; i < input.RowCount; i++)
            {
                if (!Double.IsFinite(input[i, j]))
                {
                    input[i, j] = rowMean[i];
                }
            }
        }
    }
}
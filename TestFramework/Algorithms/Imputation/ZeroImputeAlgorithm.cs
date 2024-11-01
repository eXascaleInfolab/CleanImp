using System;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class ZeroImputeAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "ZeroImp";
    protected override string Suffix => "";
    
    public ZeroImputeAlgorithm()
    {
        UseParallel = Algorithm.ParallelFull;
    }

    public ZeroImputeAlgorithm(int useParallel) : this()
    {
        UseParallel = useParallel;
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        for (int j = 0; j < input.ColumnCount; j++)
        {
            for (int i = 0; i < input.RowCount; i++)
            {
                if (!Double.IsFinite(input[i, j]))
                {
                    input[i, j] = 0.0;
                }
            }
        }
    }
}
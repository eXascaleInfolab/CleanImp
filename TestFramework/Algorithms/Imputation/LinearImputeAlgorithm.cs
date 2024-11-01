using System.Collections.Generic;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class LinearImputeAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "LinearImp";
    protected override string Suffix => "";

    public LinearImputeAlgorithm()
    {
        UseParallel = Algorithm.ParallelFull;
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        List<MissingBlock> missingBlocks = input.DetectMissingBlocks();
        ImputationHelpers.Interpolate(ref input, missingBlocks, false);
    }
}
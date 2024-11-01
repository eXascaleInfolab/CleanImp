using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class Univariate1NNAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "1NNImp";
    protected override string Suffix => "";
    
    public Univariate1NNAlgorithm()
    {
        UseParallel = Algorithm.ParallelFull;
    }

    public Univariate1NNAlgorithm(int useParallel) : this()
    {
        UseParallel = useParallel;
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        foreach (MissingBlock block in input.DetectMissingBlocks())
        {
            if (block.Start == 0 && block.Length == input.RowCount)
            {
                foreach (int i in block.RowIndices) input[i, block.Column] = 0.0;
            }
            else if (block.Start == 0)
            {
                double val = input[block.Length, block.Column];
                foreach (int i in block.RowIndices) input[i, block.Column] = val;
            }
            else if (block.Start + block.Length == input.RowCount)
            {
                double val = input[input.RowCount - block.Length - 1, block.Column];
                foreach (int i in block.RowIndices) input[i, block.Column] = val;
            }
            else
            {
                int half = block.Length / 2;
                double valLeft = input[block.Start - 1, block.Column];
                double valRight = input[block.Start + block.Length, block.Column];

                foreach (int i in block.RowIndices)
                {
                    if (i < block.Start + half) input[i, block.Column] = valLeft;
                    else  input[i, block.Column] = valRight;
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class DoNotImputeAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "DNI";
    protected override string Suffix => "";

    public DoNotImputeAlgorithm()
    {
        UseParallel = Algorithm.ParallelFull;
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        int[] missingRows = MissingRows(input);

        int newLen = input.RowCount - missingRows.Length;
        if (newLen == 0)
        {
            Console.WriteLine("[WARNING] DNI reduced the input matrix to nothing.");
            Environment.Exit(-1);
            input = MathX.Zeros(0, 0);
        }
        
        ReMergeMatrix(ref input, missingRows);
    }

    public static int[] MissingRows(Matrix<double> input)
    {
        List<MissingBlock> missingBlocks = input.DetectMissingBlocks();
        List<int> missingRows = new();

        foreach (MissingBlock missingBlock in missingBlocks)
        {
            missingRows.AddRange(missingBlock.RowIndices);
        }

        return missingRows.Distinct().OrderBy(x => x).ToArray();
    }

    public static void ReMergeMatrix(ref Matrix<double> input, int[] missingRows)
    {
        int newLen = input.RowCount - missingRows.Length;
        Matrix<double> dniMat = MathX.Zeros(newLen, input.ColumnCount);

        for (int i = 0, j = 0; i < input.RowCount; i++)
        {
            if (j < missingRows.Length && missingRows[j] == i) // first conditional is for cases when we ran out of missing rows
            {
                j++;
                continue;
            }
            dniMat.SetRow(i - j, input.Row(i));
        }

        input = dniMat;
    }
}
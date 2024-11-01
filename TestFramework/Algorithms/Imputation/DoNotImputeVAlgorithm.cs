using System;
using System.Collections.Generic;
using System.Linq;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class DoNotImputeVAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "DNI-V";
    protected override string Suffix => "";

    public DoNotImputeVAlgorithm()
    {
        UseParallel = Algorithm.ParallelFull;
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        int[] missingColumns = MissingColumns(input);

        int newCol = input.ColumnCount - missingColumns.Length;
        if (newCol == 0)
        {
            Console.WriteLine("[WARNING] DNI reduced the input matrix to nothing.");
            Environment.Exit(-1);
            input = MathX.Zeros(0, 0);
        }
        
        ReMergeMatrix(ref input, missingColumns);
    }

    public static int[] MissingColumns(Matrix<double> input)
    {
        List<MissingBlock> missingBlocks = input.DetectMissingBlocks();
        List<int> missingColumns = new();

        foreach (MissingBlock missingBlock in missingBlocks)
        {
            missingColumns.Add(missingBlock.Column);
        }

        return missingColumns.Distinct().OrderBy(x => x).ToArray();
    }

    public static void ReMergeMatrix(ref Matrix<double> input, int[] missingRows)
    {
        int newCol = input.ColumnCount - missingRows.Length;
        Matrix<double> dniMat = MathX.Zeros(input.RowCount, newCol);

        for (int i = 0, j = 0; i < input.ColumnCount; i++)
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
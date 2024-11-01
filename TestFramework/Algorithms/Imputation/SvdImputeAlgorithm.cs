using System;
using System.Collections.Generic;
using CleanIMP.Utilities.Interop;
using MathNet.Numerics.LinearAlgebra;

// ReSharper disable InconsistentNaming

namespace CleanIMP.Algorithms.Imputation;

public sealed class SvdImputeAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "SvdImp";
    protected override string Suffix => _rank > 0 ? $"K{_rank}" : "Auto";
        
    private const int MaxIters = 100;
    private const double Threshold = 0.00001;
    private readonly int _rank;

    public SvdImputeAlgorithm(int rank = 3)
    {
        if (rank < 0) throw new Exception("Invalid rank value for SvdImp algorithm");
            
        _rank = rank;

        UseParallel = 2; // run with threads = max/2
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        List<int>[] indices = new List<int>[input.ColumnCount];

        // Init list with missing values and fill them with 0s
        for (int j = 0; j < input.ColumnCount; j++)
        {
            indices[j] = new List<int>();

            for (int i = 0; i < input.RowCount; i++)
            {
                if (!Double.IsFinite(input[i, j]))
                {
                    indices[j].Add(i);

                    input[i, j] = 0.0;
                }
            }
        }
        
        int truncation = _rank == 0 ? 1 : Math.Min(_rank, input.ColumnCount - 1);

        // Solve
        for (int iter = 0; iter < MaxIters; iter++)
        {
            int currRank = iter < 20 ? Math.Min((int)Math.Pow(2, iter), truncation) : truncation;

            (Matrix<double> U, Matrix<double> S, Matrix<double> VT) = ArmaSvdEcon.TruncatedSvd(input, currRank);
            Matrix<double> X_reconstructed = U * S * VT;

            bool conv = Converged(input, X_reconstructed, Threshold, indices);

            for (int i = 0; i < input.ColumnCount; ++i)
            {
                foreach (int j in indices[i])
                {
                    input[j, i] = X_reconstructed[j, i];
                }
            }

            if (conv)
            {
                break;
            }

            ++iter;
        }
    }

    private static bool Converged(Matrix<double> X_old, Matrix<double> X_new, double threshold, List<int>[] indices)
    {
        double delta = 0.0;
        double old_norm = 0.0;

        for (int i = 0; i < X_old.ColumnCount; ++i)
        {
            foreach (int j in indices[i])
            {
                old_norm += X_old[j, i] * X_old[j, i];
                double diff = X_old[j, i] - X_new[j, i];
                delta += diff * diff;
            }
        }

        return old_norm > Double.Epsilon && (delta / old_norm) < threshold;
    }
}
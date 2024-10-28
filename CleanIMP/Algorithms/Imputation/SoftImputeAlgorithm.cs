using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using CleanIMP.Utilities.Interop;

// ReSharper disable InconsistentNaming

namespace CleanIMP.Algorithms.Imputation;

public sealed class SoftImputeAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "SoftImp";
    protected override string Suffix => _rank > 0 ? $"K{_rank}" : "Auto";
        
    private const int MaxIters = 100;
    private const double Threshold = 0.00001;
    private readonly int _rank;

    public SoftImputeAlgorithm(int rank = 3)
    {
        if (rank < 0) throw new Exception("Invalid rank value for SoftImp algorithm");
            
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

        double shrinkage_value = Double.NaN;
        
        // Solve
        for (int iter = 0; iter < MaxIters; iter++)
        {
            Matrix<double> X_reconstructed = SvdStep(input, ref shrinkage_value, truncation);

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

    private static Matrix<double> SvdStep(Matrix<double> X, ref double shrinkage_value, int max_rank)
    {
        (Matrix<double> U, Matrix<double> S, Matrix<double> VT) = ArmaSvdEcon.TruncatedSvd(X, max_rank);

        if (Double.IsNaN(shrinkage_value))
        {
            shrinkage_value = S[0, 0] / 50;
        }

        int cnt = 0;
        for (int i = 0; i < S.ColumnCount; i++)
        {
            S[i, i] = Math.Max(S[i, i] - shrinkage_value, 0.0);

            if (S[i, i] > 0)
                cnt++;
        }

        int rank = Math.Min(max_rank, cnt);
        
        Matrix<double> X_reconstructed =
            U.SubMatrix(0, U.RowCount, 0, rank)
            * S.SubMatrix(0, rank, 0, rank)
            * VT.SubMatrix(0, rank, 0, VT.ColumnCount);

        return X_reconstructed;
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
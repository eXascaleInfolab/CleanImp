using System;
using System.Collections.Generic;
using System.Linq;

using MathNet.Numerics.LinearAlgebra;

using CleanIMP.Utilities.Mathematical;
// ReSharper disable InconsistentNaming

namespace CleanIMP.Algorithms.Imputation;

public sealed class CentroidDecompositionAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "CdRec";
        
    protected override string Suffix => rank > 0 ? $"K{rank}" : $"Auto{DetectionShortName(detectionMethod)}";
        
    private const double EpsPrecision = 1E-6;
    private const int MaxIterations = 100;
    private const double EPS = Single.Epsilon * 10;
    private readonly int rank;

    private readonly CDRankDetectionMethod detectionMethod;

    private string DetectionShortName(CDRankDetectionMethod method) => method switch
    {
        CDRankDetectionMethod.EntropyCentroidValues => "Centr",
        CDRankDetectionMethod.EntropyRelevanceNorm => "RNorm",
        CDRankDetectionMethod.EntropySingularAppx => "Sing",
        _ => throw new ArgumentException("Invalid or unimplemented rank detection method.")
    };
        
    // constructor
    public CentroidDecompositionAlgorithm(int _rank = 3, CDRankDetectionMethod _detectionMethod = CDRankDetectionMethod.EntropyCentroidValues)
    {
        if (_rank < 0) throw new Exception("Invalid truncation value for CdRec algorithm");
            
        rank = _rank;
        detectionMethod = _detectionMethod;
        
        UseParallel = 2; // run with threads = max/2
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        List<MissingBlock> missingBlocks = input.DetectMissingBlocks();

        int totalMbSize = missingBlocks.Sum(mblock => mblock.Length);

        ImputationHelpers.Interpolate(ref input, missingBlocks);
        
        int iter = 0;
        double delta = 99.0;

        List<Vector<double>> signVectors = new();
            
        int truncation = rank == 0 ? 1 : Math.Min(rank, input.ColumnCount - 1);
            
        List<double>? centroidValues = null;

        while (++iter <= MaxIterations && delta >= EpsPrecision)
        {
            (Matrix<double> L, Matrix<double> R) = PerformDecomposition(input, truncation, ref signVectors, ref centroidValues);

            Matrix<double> recover = L * R.Transpose();

            delta = 0.0;

            foreach (var mblock in missingBlocks)
            {
                for (int i = mblock.Start; i < mblock.Start + mblock.Length; ++i)
                {
                    double diff = input[i, mblock.Column] - recover[i, mblock.Column];
                    delta += Math.Abs(diff);

                    input[i, mblock.Column] = recover[i, mblock.Column];
                    recover[i, mblock.Column] = 0.0;
                }
            }

            delta /= (double)totalMbSize;
        }
    }

    private static (Matrix<double>, Matrix<double>) PerformDecomposition(Matrix<double> matrix, int truncation, ref List<Vector<double>> signVectors, ref List<double>? centroidValues)
    {
        Matrix<double> X = matrix.Clone();
        Matrix<double> Load = MathX.Zeros(matrix.RowCount, matrix.ColumnCount);
        Matrix<double> Rel = MathX.Zeros(matrix.ColumnCount, matrix.ColumnCount);

        for (int i = 0; i < truncation; i++)
        {
            Vector<double> Z;

            if (signVectors.Count > i) // this means this has a value at i-th position
            {
                Z = signVectors[i];
            }
            else
            {
                Z = MathX.Ones(matrix.RowCount);
                signVectors.Add(Z);
            }
                
            FindLocalSignVector(X, ref Z);

            // C_*i = X^T * Z
            Vector<double> Rel_i = X.Transpose() * Z;

            // R_*i = C_*i / ||C_*i||
            double centroid = Rel_i.L2Norm();
            if (centroidValues != null)
            {
                if (centroid < EPS) // incomplete rank, don't even proceed with current iteration
                {
                    truncation = i;
                    break;
                }

                centroidValues.Add(centroid);
            }

            Rel_i /= centroid;

            // R = Append(R, R_*i)
            Rel.SetColumn(i, Rel_i);

            // L_*i = X * R
            Vector<double> Load_i = X * Rel_i;

            // L = Append(L, L_*i)
            Load.SetColumn(i, Load_i);

            // X := X - L_*i * R_*i^T
            X -= Load_i.OuterProduct(Rel_i);

            //refresh Z
            signVectors[i] = Z;
        }

        return (Load.SubMatrix(0, matrix.RowCount, 0, truncation), Rel.SubMatrix(0, matrix.ColumnCount, 0, truncation));
    }


    private static void FindLocalSignVector(Matrix<double> mx, ref Vector<double> Z)
    {
        Vector<double> direction;

        // init to {+1}^n
        {
            direction = (mx.Transpose() * Z);
        }

        // 2+ pass - update to Z

        bool flipped;
        double lastNorm = // cache the current value of (||D||_2)^2 to avoid recalcs
            direction.DotProduct(direction) + EPS; // eps to avoid "parity flip"

        do
        {
            flipped = false;

            for (int i = 0; i < mx.RowCount; ++i)
            {
                double signDouble = Z[i] * 2;
                double gradFlip = 0.0;

                for (int j = 0; j < mx.ColumnCount; ++j)
                {
                    double localMod = direction[j] - signDouble * mx[i, j];
                    gradFlip += localMod * localMod;
                }

                if (gradFlip > lastNorm) // net positive from flipping
                {
                    flipped = true;
                    Z[i] *= -1;
                    lastNorm = gradFlip + EPS;

                    for (int j = 0; j < mx.ColumnCount; ++j)
                    {
                        direction[j] -= signDouble * mx[i, j];
                    }
                }
            }
        } while (flipped);
    }
}

public enum CDRankDetectionMethod
{
    EntropyCentroidValues, EntropySingularAppx, EntropyRelevanceNorm, Energy
}
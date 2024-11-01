using System;
using System.Linq;
using CleanIMP.Utilities.Interop;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

// ReSharper disable InconsistentNaming

namespace CleanIMP.Algorithms.Imputation;

public sealed class SVTAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "SVT";
    protected override string Suffix => "";
    
    private const int MaxIter = 100;

    public SVTAlgorithm()
    {
        UseParallel = 2; // run with threads = max/2
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> X)
    {
        // construct matrix aux helpers
        int observed = X.Enumerate().Count(Double.IsFinite);
        Vector<double> b = MathX.Zeros(observed);
        (int, int)[] omega = new (int, int)[observed];
        
        for (int i = 0, cnt = 0; i < X.RowCount; i++)
        {
            for (int j = 0; j < X.ColumnCount; j++)
            {
                if (Double.IsFinite(X[i, j]))
                {
                    omega[cnt] = (i, j);
                    b[cnt] = X[i, j];
                    cnt++;
                }
                else
                {
                    X[i, j] = 0.0; //this is how in matlab/c++ code sparse matrix behaves
                }
            }
        }
        
        // parameters & init process
        (double tau, double delta, double k0) = Parametrize(X);
        double normb = X.FrobeniusNorm();
        X *= k0 * delta;
        
        // recovery
        (Matrix<double> U, Vector<double> W, Matrix<double> V) = ArmaSvdEcon.SvdEconDecomposition(X);
        
        int rank = Math.Max(1, W.Count(sv => sv > tau));

        U = U.SubMatrix(0, X.RowCount, 0, rank);
        Matrix<double> S = Matrix<double>.Build.DiagonalOfDiagonalVector(W.SubVector(0, rank));
        Matrix<double> VT = V.Transpose().SubMatrix(0, rank, 0, X.ColumnCount);
        
        for (int k = 0; k < MaxIter; ++k)
        {
            if (k != 0)
            {
                (U, W, V) = ArmaSvdEcon.SvdEconDecomposition(X);
                
                rank = Math.Max(1, W.Count(sv => sv > tau));
                
                U = U.SubMatrix(0, X.RowCount, 0, rank);
                S = Matrix<double>.Build.DiagonalOfDiagonalVector(W.SubVector(0, rank));
                VT = V.Transpose().SubMatrix(0, rank, 0, X.ColumnCount);
            }
            
            for (int i = 0; i < rank; ++i)
            {
                S[i, i] -= tau;
            }

            Matrix<double> Xnew = U * S * VT;
            double relRes = 0.0;
            for (int i = 0; i < omega.Length; i++)
            {
                double diff = b[i] - Xnew[omega[i].Item1, omega[i].Item2];
                X[omega[i].Item1, omega[i].Item2] += delta * diff;
                relRes += diff * diff;
            }
            
            relRes = Math.Sqrt(relRes) / normb;
            
            if (relRes < 1E-4)
            {
                break;
            }
            if (relRes > 1e5)
            {
                Console.WriteLine("SVT: Divergence");
                break;
            }
        }
        
        // replace original matrix with the recovered
        X = U * S * VT;
    }

    public (double, double, double) Parametrize(Matrix<double> X)
    {
        int n1 = X.RowCount;
        int n2 = X.ColumnCount;
        int r = 3;
        
        if (n2 > 30)
        {
            r = (n2 - 1) / 10;
            r++;
        }
        
        double p = Math.Min(5 * (r * (n1 + n2 - r)), Math.Round(.99 * (double)(n1 * n2))) / (double)(n1 * n2);
        double tau = 5 * Math.Sqrt((double)(n1 * n2)) * 0.2; // modified from 5 * sqrt(...)
        double delta = 1.2 / p;
        
        double k0 = Math.Ceiling(tau / (delta * X.L2Norm()));

        return (tau, delta, k0);
    }
}
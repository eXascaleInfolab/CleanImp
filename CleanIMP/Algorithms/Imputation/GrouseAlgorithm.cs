using System;
using System.Collections.Generic;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

// ReSharper disable InconsistentNaming

namespace CleanIMP.Algorithms.Imputation;

public sealed class GrouseAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "GROUSE";
    protected override string Suffix => $"K{_rank}";
    
    private const int MaxCycles = 5;
    private const double StepSize = 0.1;
    private readonly int _rank;

    public GrouseAlgorithm(int rank = 3)
    {
        if (rank < 1) throw new Exception("Invalid rank value for GROUSE algorithm");
            
        _rank = rank;

        UseParallel = Algorithm.ParallelFull;
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
                if (Double.IsFinite(input[i, j]))
                {
                    indices[j].Add(i);
                }
            }
        }
        
        Matrix<double> U = Matrix<double>.Build.Random(input.RowCount, _rank, 1921).GramSchmidt().Q;
        
        for (int outiter = 0; outiter < MaxCycles; ++outiter)
        {
            for (int k = 0; k < input.ColumnCount; ++k)
            {
                // Pull out the relevant indices and revealed entries for this column
                List<int> idx = indices[k];
                Vector<double> currentCol = input.Column(k);
                Vector<double> v_Omega = MathX.FromIndices(currentCol, idx);
                Matrix<double> U_Omega = MathX.FromRowIndices(U, idx);
                
                // Predict the best approximation of v_Omega by u_Omega.
                // That is, find weights to minimize ||U_Omega*weights-v_Omega||^2
                Vector<double> weights = U_Omega.Solve(v_Omega);
                
                double norm_weights = weights.L2Norm();
                
                // Compute the residual not predicted by the current estmate of U.
                Vector<double> p = U_Omega * weights;
                Vector<double> residual = v_Omega - p;
                double norm_residual = residual.L2Norm();
                
                // This step-size rule is given by combining Edelman's geodesic
                // projection algorithm with a diminishing step-size rule from SGD.  A
                // different step size rule could suffice here...
                {
                    double sG = norm_residual*norm_weights;
                    if (norm_residual < 0.000000001)
                    {
                        sG = 0.000000001 * norm_weights;
                    }

                    double t = StepSize * sG / (double) ((outiter) * input.ColumnCount + k + 1);
        
                    // Take the gradient step.
                    if (t < (Math.PI / 2.0)) // drop big steps
                    {
                        double alpha = (Math.Cos(t) - 1.0) / Math.Pow(norm_weights, 2);
                        double beta = Math.Sin(t) / sG;
            
                        Vector<double> step = U * (alpha * weights);
            
                        MathX.SetAtIndices(step, idx, (beta * residual));
            
                        U += step.OuterProduct(weights);
                    }
                }
            }
        }
        
        // generate R
        
        Matrix<double> R = MathX.Zeros(input.ColumnCount, _rank);
        
        for (int k = 0; k < input.ColumnCount; ++k)
        {
            // Pull out the relevant indices and revealed entries for this column
            List<int> idx = indices[k];
            Vector<double> currentCol = input.Column(k);
            Vector<double> v_Omega = MathX.FromIndices(currentCol, idx);
            Matrix<double> U_Omega = MathX.FromRowIndices(U, idx);
            
            // solve a simple least squares problem to populate R
            Vector<double> sol = U_Omega.Solve(v_Omega);
        
            for (int i = 0; i < sol.Count; ++i)
            {
                R[k, i] = sol[i];
            }
        }
        
        Matrix<double> recon = U * R.Transpose();
        
        for (int j = 0; j < input.ColumnCount; ++j)
        {
            for (int i = 0; i < input.RowCount; ++i)
            {
                if (Double.IsNaN(input[i, j]))
                {
                    input[i, j] = recon[i, j];
                }
            }
        }
    }
}
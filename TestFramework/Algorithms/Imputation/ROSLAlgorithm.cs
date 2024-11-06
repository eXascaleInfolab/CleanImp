using System;
using System.Collections.Generic;
using System.Linq;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public class ROSLAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "ROSL";
    protected override string Suffix => $"K{_rank}";
    
    private readonly int _rank;

    public ROSLAlgorithm(int rank = 3)
    {
        if (rank < 1) throw new Exception("Invalid rank value for ROSL algorithm");
        
        _rank = rank;

        UseParallel = 2; // run with threads = max/2
    }
    
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        ROSL.ROSL_Recovery(ref input, _rank);
    }
}

internal class ROSL
{
    // User parameters
    private int R, maxIter;
    private double lambda, tol;

    // Basic parameters
    private int rank, roslIters;
    private double mu;

    // Matrices
    private Matrix<double> D, A, E, alpha, Z, Etmp, error;

    private Random rand = new(18931);

    private ROSL()
    {
        D = Matrix<double>.Build.Dense(0, 0);
        A = Matrix<double>.Build.Dense(0, 0);
        E = Matrix<double>.Build.Dense(0, 0);
        alpha = Matrix<double>.Build.Dense(0, 0);
        Z = Matrix<double>.Build.Dense(0, 0);
        Etmp = Matrix<double>.Build.Dense(0, 0);
        error = Matrix<double>.Build.Dense(0, 0);
    }

    public static void ROSL_Recovery(ref Matrix<double> input, int rank)
    {
        ROSL rosl = new()
        {
            R = rank,
            lambda = 0.06,
            tol = 1E-4,
            maxIter = 50
        };

        List<MissingBlock> missingBlocks = input.DetectMissingBlocks();
        ImputationHelpers.Interpolate(ref input, missingBlocks);

        double err = 99.0;
        int iter = 0;

        while (err >= 1E-6 && ++iter < 100)
        {
            rosl.InexactALM_ROSL(input);

            int sharedDim = new[] { rosl.rank, rosl.D.ColumnCount, rosl.alpha.RowCount }.Min();

            Matrix<double> reconstruction = rosl.D.SubMatrix(0, rosl.D.RowCount, 0, sharedDim)
                                            * rosl.alpha.SubMatrix(0, sharedDim, 0, rosl.alpha.ColumnCount);

            err = 0.0;
            foreach (MissingBlock mblock in missingBlocks)
            {
                foreach (int i in mblock.RowIndices)
                {
                    double lastVal = input[i, mblock.Column];
                    double newVal = reconstruction[i, mblock.Column];

                    err += (lastVal - newVal) * (lastVal - newVal);

                    input[i, mblock.Column] = reconstruction[i, mblock.Column];
                }
            }

            err = Math.Sqrt(err);

            if (iter > 7 && err > 5.0) break; // divergence
        }
    }

    private void InexactALM_ROSL(Matrix<double> X)
    {
        int m = X.RowCount;
        int n = X.ColumnCount;

        // Initialize A, Z, E, Etmp and error
        A = X.Clone();
        D = MathX.Zeros(m, R);
        alpha = MathX.Zeros(R, n);
        
        Z = MathX.Zeros(m, n);
        E = MathX.Zeros(m, n);
        Etmp = MathX.Zeros(m, n);
        error = MathX.Zeros(m, n);

        // Initialize alpha randomly
        alpha.MapInplace(_ => rand.NextDouble(), Zeros.Include);

        double infnorm, fronorm;
        infnorm = X.Enumerate().Max();
        fronorm = X.FrobeniusNorm();

        // These are tunable parameters
        double rho, mubar;
        mu = 10 * lambda / infnorm;
        rho = 1.5;
        mubar = mu * 1E7;

        double stopcrit;

        for (int i = 0; i < maxIter; i++)
        {
            // Error matrix and intensity thresholding
            Etmp = X + Z - A;
            E = Etmp.Clone().PointwiseAbs() - lambda / mu;
            E.MapInplace(val => val > .0 ? val : .0);
            E.PointwiseMultiply(Etmp.PointwiseSign());

            // Perform the shrinkage
            LowRankDictionaryShrinkage(X);

            // Update Z
            Z = (Z + X - A - E) / rho;
            mu = (mu * rho < mubar) ? mu * rho : mubar;

            // Calculate stop criterion
            stopcrit = (X - A - E).FrobeniusNorm() / fronorm;
            roslIters = i + 1;

            // Exit if stop criteria is met
            if (stopcrit < tol)
            {
                return;
            }
        }

        // Report convergence warning
        Console.WriteLine("WARNING: ROSL did not converge in " + roslIters + " iterations");
    }

    private void LowRankDictionaryShrinkage(Matrix<double> X)
    {
        // Get current rank estimate
        rank = D.ColumnCount;

        // Thresholding
        double alphanormthresh;
        Vector<double> alphanorm = MathX.Zeros(rank);

        // Norms
        double dnorm;

        // Loop over columns of D
        for (int i = 0; i < rank; i++)
        {
            // Compute error and new D(:,i)
            D.ClearColumn(i);
            error = (X + Z - E) - (D * alpha);
            D.SetColumn(i, error * alpha.Row(i));
            dnorm = D.Column(i).L2Norm();

            // Shrinkage
            if (dnorm > 0.0)
            {
                // Gram-Schmidt on D
                for (int j = 0; j < i; j++)
                {
                    D.SetColumn(i, D.Column(i) - D.Column(j) * D.Column(j).DotProduct(D.Column(i)));
                }

                // Normalize
                D.SetColumn(i, D.Column(i) / D.Column(i).L2Norm());

                // Compute alpha(i,:)
                alpha.SetRow(i, D.Column(i) * error);

                // Magnitude thresholding
                alphanorm[i] = alpha.Row(i).L2Norm();
                alphanormthresh = (alphanorm[i] - 1 / mu > 0.0) ? alphanorm[i] - 1 / mu : 0.0;
                alpha.SetRow(i, alpha.Row(i) * (alphanormthresh / alphanorm[i]));
                alphanorm[i] = alphanormthresh;
            }
            else
            {
                alpha.ClearRow(i);
                alphanorm[i] = 0.0;
            }
        }

        // Delete the zero bases
        int[] alphaindices = alphanorm.EnumerateIndexed().Where(t => t.Item2 != 0.0).Select(t => t.Item1).ToArray();
        D = Matrix<double>.Build.DenseOfColumns(D.EnumerateColumnsIndexed().Where(t => alphaindices.Contains(t.Item1)).Select(t => t.Item2));
        alpha = Matrix<double>.Build.DenseOfRows(alpha.EnumerateRowsIndexed().Where(t => alphaindices.Contains(t.Item1)).Select(t => t.Item2));

        // Update A
        A = D * alpha;
    }
}

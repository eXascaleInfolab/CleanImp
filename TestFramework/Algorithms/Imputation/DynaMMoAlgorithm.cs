using System.Collections.Generic;
using System.Linq;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;
// ReSharper disable InconsistentNaming

namespace CleanIMP.Algorithms.Imputation;

public sealed class DynaMMoAlgorithm : Algorithm
{
    // fields
    public override string AlgCodeBase => "DynaMMo";
    protected override string Suffix => H == 3 ? "" : $"K{H}";
    
    private int maxIter = 100;
    private bool FAST = true;
    private int H = 3;

    private const int Seed = 18931;

    public DynaMMoAlgorithm(int k = 3)
    {
        H = k;
        UseParallel = Algorithm.ParallelFull;
    }

    // functions
    protected override void RecoverInternal(ref Matrix<double> X)
    {
        X = X.Transpose();
        
        int N = X.ColumnCount;
        int M = X.RowCount;
    
        // lambda for stopping condition
        bool IsTiny(Matrix<double> sigma) => sigma.L1Norm() < 1e-10 || sigma.Diagonal().Any(e => e < 1e-10);

        // get number of hidden variables
    
        if (H == 0)
        {
            H = M;
        }
    
        // get the initial model
        DynaMMoModel model = new()
        {
            A = MathX.Identity(H, H) + Matrix<double>.Build.Random(H, H, Seed),
            C = MathX.Identity(M, H) + Matrix<double>.Build.Random(M, H, Seed + 1),
            Q = MathX.Identity(H, H),
            R = MathX.Identity(M, M),
            mu0 = Vector<double>.Build.Random(H, Seed + 2),
            Q0 = MathX.Identity(H, H)
        };

        List<MissingBlock> blocks = X.DetectMissingBlocks();
        ImputationHelpers.Interpolate(ref X, blocks);

        int iter = 0;
    
        while ((iter < maxIter) && (!(IsTiny(model.Q0) || IsTiny(model.Q) || IsTiny(model.R))))
        {
            iter++;
            
            (Vector<double>[] mu, Matrix<double>[] V, Matrix<double>[] P) = forward(X, ref model, N);
            (Vector<double>[] Ez, Matrix<double>[] Ezz, Matrix<double>[] Ez1z) = backward(mu, V, P, ref model, N);
            MLE_lds(N, M, X, ref model, Ez, Ezz, Ez1z);
            
            Matrix<double> Y = model.C * Matrix<double>.Build.DenseOfColumnVectors(Ez);

            foreach (MissingBlock block in blocks)
            {
                foreach (int i in block.RowIndices)
                {
                    X[i, block.Column] = Y[i, block.Column];
                }
            }
        }

        X = X.Transpose();
    }
    
    (Vector<double>[], Matrix<double>[], Matrix<double>[]) forward(Matrix<double> X, ref DynaMMoModel model, int N)
    {
        Matrix<double> Ih = MathX.Identity(H, H);
        
        //predicted mean for hidden variable z
        Vector<double>[] mu = new Vector<double>[N];
        Matrix<double>[] V = new Matrix<double>[N];
        Matrix<double>[] P = new Matrix<double>[N];
        
        mu[0] = model.mu0;
        V[0] = model.Q0;
        
        // dummy init since it's used only under a conditional and it can be not initialized
        Matrix<double> invR = Matrix<double>.Build.Dense(1, 1);
        Matrix<double> invRC = invR;
        Matrix<double> invCRC = invR;
        if (FAST)
        {
            invR = model.R.Inverse();
            invRC = invR * model.C;
            invCRC = model.C.Transpose() * invRC;
        }
        
        for (int i = 0; i < N; ++i)
        {
            Matrix<double> KP;
            if (i == 0)
            {
                KP = model.Q0;
                mu[i] = model.mu0;
            }
            else
            {
                P[i - 1] = model.A * V[i - 1] * model.A.Transpose() + model.Q;
                KP = P[i - 1];
                mu[i] = model.A * mu[i - 1];
            }
            
            Matrix<double> invSig;
            if (FAST)
            {
                Matrix<double> sol = (KP.Inverse() + invCRC).Transpose().Solve(invRC.Transpose()).Transpose();
                invSig = invR - sol * invRC.Transpose();
            }
            else
            {
                Matrix<double> sigma_c = model.C * KP * model.C.Transpose() + model.R;
                invSig = sigma_c.Inverse();
            }
            
            Matrix<double> K = KP * model.C.Transpose() * invSig;
            Vector<double> u_c = model.C * mu[i];
            Vector<double> delta = X.Column(i) - u_c;
            mu[i] = mu[i] + K * delta;
            V[i] = (Ih - K * model.C) * KP;
        }
        
        return (mu, V, P);
    }

    (Vector<double>[], Matrix<double>[], Matrix<double>[]) backward(Vector<double>[] mu, Matrix<double>[] V, Matrix<double>[] P, ref DynaMMoModel model, int N)
    {
        Vector<double>[] Ez = new Vector<double>[N];
        Matrix<double>[] Ezz = new Matrix<double>[N];
        Matrix<double>[] Ez1z = new Matrix<double>[N];
        
        Ez[N - 1] = mu[N - 1];
        Matrix<double> Vhat = V[N - 1].Clone();
        Ezz[N - 1] = Vhat + Ez[N - 1].OuterProduct(Ez[N - 1]);
        
        for (int ii = N - 1; ii > 0; --ii)
        {
            int i = ii - 1;
            
            Matrix<double> J = P[i].Transpose().Solve((V[i] * model.A.Transpose()).Transpose()).Transpose();
            Ez[i] = mu[i] + J * (Ez[i + 1] - model.A * mu[i]);
            Ez1z[i] = Vhat * J.Transpose() + Ez[i + 1].OuterProduct(Ez[i]);
            Vhat = V[i] + J * (Vhat - P[i]) * J.Transpose();
            Ezz[i] = Vhat + Ez[i].OuterProduct(Ez[i]);
        }
        
        return (Ez, Ezz, Ez1z);
    }

    void MLE_lds(int N, int M, Matrix<double> X, ref DynaMMoModel model, Vector<double>[] Ez, Matrix<double>[] Ezz, Matrix<double>[] Ez1z)
    {
        Matrix<double> Sz1z = MathX.Zeros(H, H);
        Matrix<double> Szz = MathX.Zeros(H, H);
        Matrix<double> Sxz = MathX.Zeros(M, H);
        
        for (int i = 0; i < N-1; ++i)
        {
            Sz1z += Ez1z[i];
        }
        
        for (int i = 0; i < N; ++i)
        {
            Szz += Ezz[i];
            Sxz += X.Column(i).OuterProduct(Ez[i]);
        }
        
        Matrix<double> SzzN = Szz - Ezz[N - 1]; // sum of E[z, z] from 1 to n-1
        
        model.mu0 = Ez[0];
        
        model.Q0 = Ezz[0] - Ez[0].OuterProduct(Ez[0]);
        model.Q0 = MathX.Identity(H, H) * (model.Q0.Trace() / (double)H);
        
        model.A = SzzN.Transpose().Solve(Sz1z.Transpose()).Transpose();

        {
            double delta = ((
                Szz.Trace()
                - Ezz[0].Trace()
                - 2 * (model.A * Sz1z.Transpose()).Trace()
                + (model.A * SzzN * model.A.Transpose()).Trace()
            ) / (double) (N - 1)) / (double) H;
            
            model.Q = MathX.Identity(H, H) * delta;
        }
        
        model.C = Szz.Transpose().Solve(Sxz.Transpose()).Transpose();

        {
            double delta = (
                (X * X.Transpose()).Trace() - 2 * (model.C * Sxz.Transpose()).Trace() + (model.C * Szz * model.C.Transpose()).Trace()
            ) / (double) N / (double) M;
            
            model.R = MathX.Identity(M, M) * delta;
        }
    }

    private struct DynaMMoModel
    {
        public Matrix<double> A;
        public Matrix<double> C;
        public Matrix<double> Q;
        public Matrix<double> R;
        public Vector<double> mu0;
        public Matrix<double> Q0;
    }
}
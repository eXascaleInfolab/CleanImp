using System;
using System.Collections.Generic;
using System.Linq;
using CleanIMP.Utilities.Mathematical;
using CleanIMP.Utilities;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CleanIMP.Algorithms.Imputation;

public sealed class KNNImputeAlgorithm : Algorithm
{
    public override string AlgCodeBase => "knnimp";
    protected override string Suffix => _neighbors == 3 ? "" : $"-{_neighbors}";

    private readonly ZeroImputeAlgorithm _fallbackImp = new();
        
    // algo params
    private readonly int _neighbors;

    public KNNImputeAlgorithm(int n = 3)
    {
        _neighbors = n;
        UseParallel = Algorithm.ParallelFull;
    }
    
    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        // Step 1 - build the list of complete tuples
        List<int> completeTuples = new();
        List<(int, (int, double)[], int[])> incompleteTuples = new();

        for (int i = 0; i < input.RowCount; i++)
        {
            (int, double)[] finiteSubvector = input.Row(i).EnumerateIndexed().WhereNOT(t => Double.IsNaN(t.Item2)).Select(t => (t.Item1, t.Item2)).ToArray();
            int[] imputeIndices = input.Row(i).EnumerateIndexed().Where(t => Double.IsNaN(t.Item2)).Select(t => t.Item1).ToArray();

            if (finiteSubvector.Length == input.ColumnCount)
            {
                completeTuples.Add(i);
            }
            else
            {
                incompleteTuples.Add((i, finiteSubvector, imputeIndices));
            }
        }

        // Step 1.1 - Fallback in case there is nothing to kNN on
        if (completeTuples.Count == 0)
        {
            Console.WriteLine("No complete tuples - using ZeroImpute fallback");
            _fallbackImp.RecoverMatrix(ref input);
            return;
        }
        
        // Step 2 - kNN
        foreach ((int i, (int, double)[] finiteSubvector, int[] imputeIndices) in incompleteTuples)
        {
            List<(int, double)> kNNs = new();
            double minMaxDist = Double.PositiveInfinity;
            int minMaxIdx = 0;
            Vector<double> imputeVector = new DenseVector(finiteSubvector.Select(t => t.Item2).ToArray());
            
            // Step 2.1 - produce the list of candidates
            foreach (int refRow in completeTuples)
            {
                // Step 2.1.1 create a compatible reference subvector
                Vector<double> refVector = MathX.Zeros(imputeVector.Count);
                for (int j = 0; j < refVector.Count; j++)
                {
                    refVector[j] = input[refRow, finiteSubvector[j].Item1];
                }
                
                // Step 2.1.2 distance
                double dist = (refVector - imputeVector).L2Norm();
                
                // Step 2.1.3 neighborhood
                if (kNNs.Count < _neighbors)
                {
                    kNNs.Add((refRow, dist));
                    if (kNNs.Count == _neighbors)
                    {
                        minMaxDist = kNNs.Max(t => t.Item2);
                        minMaxIdx = kNNs.FindIndex(t => t.Item2 == minMaxDist);
                    }
                }
                else if (dist < minMaxDist)
                {
                    kNNs.RemoveAt(minMaxIdx);
                    kNNs.Add((refRow, dist));
                    minMaxDist = kNNs.Max(t => t.Item2);
                    minMaxIdx = kNNs.FindIndex(t => t.Item2 == minMaxDist);
                }
            }
            
            // Step 2.2 - impute
            foreach (int j in imputeIndices)
            {
                Matrix<double> matrix = input;
                double mean = kNNs.Select(t => t.Item1).Select(idx => matrix[idx, j]).Average();
                input[i, j] = mean;
            }
        }
    }
}

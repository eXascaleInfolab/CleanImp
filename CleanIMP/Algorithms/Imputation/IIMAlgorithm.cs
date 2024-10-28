using CleanIMP.Utilities.Interop;
using MathNet.Numerics.LinearAlgebra;

namespace CleanIMP.Algorithms.Imputation;

public sealed class IIMAlgorithm : Algorithm
{
    public override string AlgCodeBase => "IIM";
    protected override string Suffix => _neighbors == 3 ? "" : $"-{_neighbors}";
        
    // algo params
    private readonly int _neighbors;

    public IIMAlgorithm(int n = 3)
    {
        _neighbors = n;
        UseParallel = Algorithm.ParallelFull;
    }
    
    // functions
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        (_, input) = PythonPipeImpute.PythonIIM(input, _neighbors);
    }
}

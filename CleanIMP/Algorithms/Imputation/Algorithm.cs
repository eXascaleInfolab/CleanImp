using System;
using System.Collections.Generic;
using System.Linq;

using MathNet.Numerics.LinearAlgebra;

using CleanIMP.Testing;
using CleanIMP.Utilities;
using CleanIMP.Utilities.Mathematical;

namespace CleanIMP.Algorithms.Imputation;

/// <summary>
/// An abstract class for upstream imputation techniques.
/// Base class contains different functions related to how different types of datasets (linked to respective downstream tasks) are handled.
/// Derived classes are only required to implement their naming and one function that recovers a single matrix.
/// </summary>
public abstract class Algorithm
{
    //
    // static functions
    //

    public static readonly List<Algorithm> AllAlgos = new()
    {
        // Advanced algorithms
        new CentroidDecompositionAlgorithm(), new SvdImputeAlgorithm(), new SoftImputeAlgorithm(),
        new StmvlAlgorithm(), new GrouseAlgorithm(), new SVTAlgorithm(), new DynaMMoAlgorithm(), new IIMAlgorithm(),
        
        // Simple algorithms
        new MeanImputeAlgorithm(), new HorizontalMeanImputeAlgorithm(), new ZeroImputeAlgorithm(), new Univariate1NNAlgorithm(),
        new LinearImputeAlgorithm(), new KNNImputeAlgorithm(),
        
        // DNI
        new DoNotImputeAlgorithm(), new DoNotImputeVAlgorithm(),
    };

    //
    // Abstract class
    //

    // fields
    public abstract string AlgCodeBase { get; }

    public string AlgCode => AlgCodeBase + Suffix;
    protected abstract string Suffix { get; }

    /// <summary>
    /// 0 - no parallelization
    /// 1 - full parallelization
    /// N >= 2 - run in max/N threads
    /// </summary>
    public int UseParallel { protected init; get; }

    protected const int ParallelNone = 0;
    protected const int ParallelFull = 1;

    // functions
    public void RecoverDataset(MultivarDataset dataset)
    {
        if (this is DoNotImputeAlgorithm || this is DoNotImputeVAlgorithm)
        {
            // this works in theory, but the trade-off in case there is *any* misalignment between multiple multivariate time series is immense
            // it's going to reduce the whole dataset to nothing under almost any typical contamination scenario
            // it's just not worth implementing at all
            throw new NotSupportedException();
        }
        
        foreach (MultivarSeries ts in dataset.Train)
        {
            RecoverMatrix(ref ts.Matrix);
        }

        foreach (MultivarSeries ts in dataset.Test)
        {
            RecoverMatrix(ref ts.Matrix);
        }
    }
    
    public void RecoverDataset(ref UnivarDataset dataset, bool byClass)
    {
        // for DNI we need to align train and test missingness by what *both* have missing
        if (this is DoNotImputeAlgorithm)
        {
            Matrix<double> trainMatrixDni = Matrix<double>.Build.DenseOfColumnVectors(dataset.Train.Select(ts => ts.Vector));
            Matrix<double> testMatrixDni = Matrix<double>.Build.DenseOfColumnVectors(dataset.Test.Select(ts => ts.Vector));
            
            int[] missingRows = DoNotImputeAlgorithm.MissingRows(trainMatrixDni);
            missingRows = missingRows.Union(DoNotImputeAlgorithm.MissingRows(testMatrixDni)).Distinct().OrderBy(x => x).ToArray();
            
            DoNotImputeAlgorithm.ReMergeMatrix(ref trainMatrixDni, missingRows);
            DoNotImputeAlgorithm.ReMergeMatrix(ref testMatrixDni, missingRows);
            
            dataset.Train.ForEach((series, i) => series.Vector = trainMatrixDni.Column(i));
            dataset.Test.ForEach((series, i) => series.Vector = testMatrixDni.Column(i));
            
            return;
        }

        if (this is DoNotImputeVAlgorithm)
        {
            // this one is even simpler than DNI-H because we can just remove series from the Dataset object and that's it
            Matrix<double> trainMatrixDni = Matrix<double>.Build.DenseOfColumnVectors(dataset.Train.Select(ts => ts.Vector));
            Matrix<double> testMatrixDni = Matrix<double>.Build.DenseOfColumnVectors(dataset.Test.Select(ts => ts.Vector));
            
            // remove indices in train of the original object
            int[] missingCols = DoNotImputeVAlgorithm.MissingColumns(trainMatrixDni);
            foreach (int i in missingCols.OrderByDescending(x => x)) // orderby is needed to avoid removing "early" indices that will rearrange the later ones and cause OOB
            {
                dataset.Train.RemoveAt(i);
            }
            
            // same for test
            missingCols = DoNotImputeVAlgorithm.MissingColumns(testMatrixDni);
            foreach (int i in missingCols.OrderByDescending(x => x))
            {
                dataset.Test.RemoveAt(i);
            }
            
            return;
        }
        
        if (byClass)
        {
            foreach (var group in dataset.Train.GroupBy(ts => ts.TrueClass))
            {
                UnivarSeries[] tsList = group.ToArray();
                Matrix<double> matrix = Matrix<double>.Build.DenseOfColumnVectors(tsList.Select(ts => ts.Vector));

                RecoverMatrix(ref matrix);
                tsList.ForEach((series, i) => series.Vector = matrix.Column(i)); //write back, UniVarTs is a reference type
            }
        }
        else
        {
            Matrix<double> trainMatrix = Matrix<double>.Build.DenseOfColumnVectors(dataset.Train.Select(ts => ts.Vector));
            RecoverMatrix(ref trainMatrix);
            dataset.Train.ForEach((series, i) => series.Vector = trainMatrix.Column(i));
        }
        
        Matrix<double> testMatrix = Matrix<double>.Build.DenseOfColumnVectors(dataset.Test.Select(ts => ts.Vector));
        RecoverMatrix(ref testMatrix);
        dataset.Test.ForEach((series, i) => series.Vector = testMatrix.Column(i));
    }
    
    public void RecoverDataset(UniClusterDataset dataset)
    {
        var seriesMatrix = Matrix<double>.Build.DenseOfColumnVectors(dataset.Series);
        RecoverMatrix(ref seriesMatrix);
        for (int i = 0; i < dataset.Series.Length; i++)
        {
            dataset.Series[i] = seriesMatrix.Column(i);
        }
    }

    public void RecoverMatrix(ref Matrix<double> matrix)
    {
        // preliminary check - skip if it doesn't have NaNs
        List<MissingBlock> missingBlocks = matrix.DetectMissingBlocks();
        if (missingBlocks.MissingTotal() == 0)
            return;

        RecoverInternal(ref matrix);

        // post-condition - matrix doesn't contain NaNs
        int finiteVals = matrix.EnumerateRows().Sum(vec => vec.Sum(val => Double.IsFinite(val) ? 1 : 0));
        if (finiteVals != matrix.RowCount * matrix.ColumnCount)
        {
            throw new InvalidOperationException("Recovery algorithm has failed to impute all values");
        }
    }

    /// <summary>
    /// Recovers the missing data from the matrix. MUST operate on the same instance or place the recovery back.
    /// Data will be checked for integrity in the function of the abstract class itself.
    /// </summary>
    /// <param name="input">Matrix containing missing values</param>
    protected abstract void RecoverInternal(ref Matrix<double> input);
}

public sealed class DummyAlgorithm : Algorithm
{
    public override string AlgCodeBase
    {
        get
        {
            if (String.IsNullOrEmpty(_algCode))
                throw new InvalidOperationException("Dummy algorithm doesn't have its code set to a valid string.");
            return _algCode;
        }
    }

    private readonly string _algCode;

    protected override string Suffix => "";

    public DummyAlgorithm(string algCode)
    {
        _algCode = algCode;
    }
    
    protected override void RecoverInternal(ref Matrix<double> input)
    {
        throw new InvalidOperationException("This class isn't supposed to be used for imputation.");
    }
}

public static class AlgorithmFactory
{
    public static Algorithm ConstructAlgorithm(string algoDescription)
    {
        string[] algoSplit = algoDescription.Split(':');

        string algoCode = algoSplit[0].ToLower();
        string? algoParams;
        
        if (algoSplit.Length == 1)
        {
            algoParams = null;
        }
        else if (algoSplit.Length == 2)
        {
            algoParams = algoSplit[1].ToLower();
        }
        else
        {
            Console.WriteLine($"Malformed algorithm descriptor: {algoDescription}.");
            Console.WriteLine("Either the string is empty or it contains : more than once.");
            throw new ArgumentException("Malformed algorithm descriptor.");
        }
        
        Algorithm? alg = Algorithm.AllAlgos.FirstOrDefault(x => x.AlgCodeBase.ToLower() == algoCode);
        if (alg == null)
        {
            Console.WriteLine($"Algorithm with the code {algoCode} not found.");
            throw new ArgumentException("Algorithm code from the descriptor doesn't exist");
        }

        // Algorithm is not using any parameters, can give default version that we just loaded
        if (algoParams == null)
        {
            return alg;
        }

        // treat parameters
        switch (algoCode)
        {
            case "cdrec": return CdRecFactory(algoParams);
            case "svdimp": return SvdImpFactory(algoParams);
            case "softimp": return SoftImpFactory(algoParams);
            //case "stmvl": return new StmvlAlgorithm(); //no config, so throw an exception
            case "grouse": return GROUSEFactory(algoParams);
            case "dynammo": return DynaMMoFactory(algoParams);
            case "iim": return IIMFactory(algoParams);
            case "knnimp": return KNNFactory(algoParams);

            default:
                Console.WriteLine($"ERROR: parameter configurator for the algorithm with the code {algoCode} not found.");
                Console.WriteLine("Either this algorithm cannot be be configured, or configurator uses the wrong AlgCode.");
                throw new InvalidProgramException($"Configurator has failed for a valid AlgCode: {algoCode}");
        }
    }

    private static CentroidDecompositionAlgorithm CdRecFactory(string parameters)
    {
        string[] paramList = parameters.Split('|');

        int k = 3;
        CDRankDetectionMethod detectionMethod = CDRankDetectionMethod.EntropyCentroidValues;

        foreach (string param in paramList)
        {
            if (param.StartsWith("k"))
            {
                k = Int32.Parse(param[1..]);
            } 
            else if (param.StartsWith("auto"))
            {
                detectionMethod = param[4..] switch
                {
                    "cv" => CDRankDetectionMethod.EntropyCentroidValues,
                    "sv" => CDRankDetectionMethod.EntropySingularAppx,
                    "rel" => CDRankDetectionMethod.EntropyRelevanceNorm,
                    _ => throw new ArgumentException("Unrecognized rank detection code")
                };
            }
        }
        
        return new CentroidDecompositionAlgorithm(k, detectionMethod);
    }

    private static SvdImputeAlgorithm SvdImpFactory(string parameters)
    {
        string[] paramList = parameters.Split('|');

        int k = 3;
        
        foreach (string param in paramList)
        {
            if (param.StartsWith("k"))
            {
                k = Int32.Parse(param[1..]);
            }
        }

        return new SvdImputeAlgorithm(k);
    }

    private static SoftImputeAlgorithm SoftImpFactory(string parameters)
    {
        string[] paramList = parameters.Split('|');

        int k = 3;
        
        foreach (string param in paramList)
        {
            if (param.StartsWith("k"))
            {
                k = Int32.Parse(param[1..]);
            }
        }

        return new SoftImputeAlgorithm(k);
    }

    private static GrouseAlgorithm GROUSEFactory(string parameters)
    {
        string[] paramList = parameters.Split('|');

        int k = 3;
        
        foreach (string param in paramList)
        {
            if (param.StartsWith("k"))
            {
                k = Int32.Parse(param[1..]);
            }
        }

        return new GrouseAlgorithm(k);
    }

    private static DynaMMoAlgorithm DynaMMoFactory(string parameters)
    {
        string[] paramList = parameters.Split('|');

        int k = 3;
        
        foreach (string param in paramList)
        {
            if (param.StartsWith("k"))
            {
                k = Int32.Parse(param[1..]);
            }
        }

        return new DynaMMoAlgorithm(k);
    }

    private static IIMAlgorithm IIMFactory(string parameters)
    {
        string[] paramList = parameters.Split('|');

        int n = 3;
        
        foreach (string param in paramList)
        {
            if (param.StartsWith("n"))
            {
                n = Int32.Parse(param[1..]);
            }
        }

        return new IIMAlgorithm(n);
    }

    private static Algorithm KNNFactory(string parameters)
    {
        string[] paramList = parameters.Split('|');

        int n = 3;
        
        foreach (string param in paramList)
        {
            if (param.StartsWith("n"))
            {
                n = Int32.Parse(param[1..]);
            }
        }

        return new KNNImputeAlgorithm(n);
    }
}

using System;
using System.Linq;
using System.Runtime.InteropServices;
using MathNet.Numerics.LinearAlgebra;
// ReSharper disable InconsistentNaming

namespace CleanIMP.Utilities.Interop;

/// <summary>
/// This class relies on a simple custom C library that wraps Armadillo C++ library to use its Truncated SVD functionality.
/// C# libraries don't have an implementation of SVD that is log-linear in length, so performance suffers on longer time series.
/// Armadillo needs to be installed in the system and libArmaWrap.so should be placed either in $PATH-accessible location, or just the same folder as the dotnet executable.
/// This functionality is only supported on Linux, using this class when running on Windows will result in a crash.
/// </summary>
/// <remarks>
/// Usage example and a simple sanity check are given in the two test methods.
/// </remarks>
internal static class ArmaSvdEcon
{
    [DllImport("libArmaWrap.so")]
    private static extern void proxy_arma_svd_econ(
        [In] double[] matrixNative, UInt64 dimN, UInt64 dimM,
        [In] double[] uContainer, [In] double[] sigmaContainer, [In] double[] vContainer
    );
    // [In] parameter will marshall double[] as double *
    // C will have read-write access to the CLR managed memory of the double[] array
    // but it cannot "replace" the pointer itself, so we're safe here
    
    public static (Matrix<double>, Vector<double>, Matrix<double>) SvdEconDecomposition(Matrix<double> matrix)
    {
        // convenience variables
        (int rows, int columns) = (matrix.RowCount, matrix.ColumnCount);
        int sharedDim = Math.Min(rows, columns);

        // extract raw data from matrix
        // Math.NET in theory should store the data in col-major form (i.e. columns are continuous in memory, rows are not)
        // However, as a fallback if this is not the case for some reason (AsColMajor() returns null), we can ask for a conversion at a perf. cost
        double[] matArray = matrix.AsColumnMajorArray() ?? matrix.ToColumnMajorArray();

        // declare output containers
        double[] uArray = new double[rows * sharedDim];
        double[] sArray = new double[sharedDim];
        double[] vArray = new double[columns * sharedDim];
        
        // call, C marshalling of C# [] arrays is handled in the extern declaration
        proxy_arma_svd_econ(matArray, (UInt64) rows, (UInt64) columns, uArray, sArray, vArray);

        if (uArray.Any(Double.IsNaN))
        {
            // SVD can fail, signals from armadillo are handled in the wrapper
            // to signal C# of the problem, the wrapper will fill the output with NaNs
            throw new Exception("svd_econ (armadillo) failed");
        }
        
        // construct the resulting decomposition containers, wrapper returns raw arrays
        Matrix<double> U = Matrix<double>.Build.DenseOfColumnMajor(rows, sharedDim, uArray);
        Vector<double> S = Vector<double>.Build.DenseOfArray(sArray);
        Matrix<double> V = Matrix<double>.Build.DenseOfColumnMajor(columns, sharedDim, vArray);
        
        // return
        return (U, S, V);
    }

    public static (Matrix<double>, Matrix<double>, Matrix<double>) TruncatedSvd(Matrix<double> matrix, int rank)
    {
        (Matrix<double> U, Vector<double> S, Matrix<double> V) = ArmaSvdEcon.SvdEconDecomposition(matrix);

        U = U.SubMatrix(0, matrix.RowCount, 0, rank);
        Matrix<double> W = Matrix<double>.Build.DiagonalOfDiagonalVector(S.SubVector(0, rank));
        V = V.Transpose().SubMatrix(0, rank, 0, matrix.ColumnCount);

        return (U, W, V);
    }

    public static void SvdEconTest()
    {
        var exampleMat = Matrix<double>.Build.Random(10, 5, 0x12FF1);

        var (U, S, V) = ArmaSvdEcon.SvdEconDecomposition(exampleMat);
    
        Console.WriteLine(U.ToMatrixString());
        Console.WriteLine(S.ToVectorString());
        Console.WriteLine(V.ToMatrixString());
        
        Console.WriteLine("Original:");
        Console.WriteLine(exampleMat.ToMatrixString());
        
        Console.WriteLine("Reconstruct:");
        Console.WriteLine(U * Matrix<double>.Build.DiagonalOfDiagonalVector(S) * V.Transpose());
    }

    public static void SvdEconTest2()
    {
        var exampleMat = Matrix<double>.Build.Random(5, 10, 0x12FF1);

        var (U, S, V) = ArmaSvdEcon.SvdEconDecomposition(exampleMat);
    
        Console.WriteLine(U.ToMatrixString());
        Console.WriteLine(S.ToVectorString());
        Console.WriteLine(V.ToMatrixString());
        
        Console.WriteLine("Original:");
        Console.WriteLine(exampleMat.ToMatrixString());
        
        Console.WriteLine("Reconstruct:");
        Console.WriteLine(U * Matrix<double>.Build.DiagonalOfDiagonalVector(S) * V.Transpose());
    }
}
using System;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.IntegralTransforms;
using CX64 = System.Numerics.Complex;

namespace CleanIMP.Utilities.Mathematical.ThirdPartyPorts;

// Zakhar:
// Code ported to C# from python
// Using source code of pypi's version of kshape as the starting point: https://pypi.org/project/kshape/#files
// Only SDB (shape-based distance) functionality is retained (both distance and shift), adapted to use MathNet's Complex Fourier.

public static class KShape
{
    private static int FourierSize(int size)
    {
        // next power of 2
        int fftSize = 32 - System.Numerics.BitOperations.LeadingZeroCount((UInt32) (2 * size - 1));
        fftSize = 1 << fftSize;
        return fftSize;
    }
    
    private static Vector<double> DualTransform(Vector<double> series1, Vector<double> series2)
    {
        // fft size
        int fftSize = FourierSize(series1.Count);
        
        // density
        double den = series1.L2Norm() * series2.L2Norm();

        // resize the vectors
        Vector<CX64> fft1 = Vector<CX64>.Build.Dense(fftSize);
        Vector<CX64> fft2 = Vector<CX64>.Build.Dense(fftSize);

        for (int i = 0; i < series1.Count; i++)
        {
            fft1[i] = (CX64)series1[i];
            fft2[i] = (CX64)series2[i];
        }

        //fft
        FourierOptions fo = FourierOptions.AsymmetricScaling;
        
        Fourier.Forward(fft1.AsArray(), fo);
        Fourier.Forward(fft2.AsArray(), fo);
        fft1 = fft1.PointwiseMultiply(fft2.Conjugate());
        Fourier.Inverse(fft1.AsArray(), fo); //fft1 = cc

        Vector<double> ncc = MathX.Zeros(series1.Count * 2 - 1);

        for (int i = 0; i < series1.Count; i++)
        {
            if (i != series1.Count - 1)
            {
                ncc[i] = fft1[fft1.Count - series1.Count + i + 1].Real / den;
            }

            ncc[series1.Count + i - 1] = fft1[i].Real / den;
        }

        return ncc;
    }
    
    public static double ShapeBasedDistance(Vector<double> series1, Vector<double> series2)
    {
        return (1 - ShapeBasedSimilarity(series1, series2)) / 2;
    }
    
    public static double ShapeBasedSimilarity(Vector<double> series1, Vector<double> series2)
        => FullSbd(series1, series2).Item2;

    public static int ShiftIndex(Vector<double> series1, Vector<double> series2)
        => FullSbd(series1, series2).Item1;

    public static (int, double) FullSbd(Vector<double> series1, Vector<double> series2)
    {
        Vector<double> ncc = DualTransform(series1, series2);

        int shift = ncc.MaximumIndex();
        double max = ncc[shift];

        // adjust
        shift -= (series1.Count - 1);
        //if (shift >= series1.Count)
        //    shift -= series1.Count;
        
        //if (shift >= series1.Count / 2)
        //    shift -= series1.Count;

        return (shift, max);
    }
}
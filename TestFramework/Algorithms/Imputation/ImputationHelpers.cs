using System;
using System.Collections.Generic;
using CleanIMP.Utilities.Mathematical;
using MathNet.Numerics.LinearAlgebra;


namespace CleanIMP.Algorithms.Imputation;

public static class ImputationHelpers
{
    public static void Interpolate(ref Matrix<double> matrix, List<MissingBlock> missingBlocks, bool smartInterpolate = false)
    {
        // init missing blocks
        foreach (var block in missingBlocks)
        {
            // linear interpolation
            double val1 = Double.NaN, val2 = Double.NaN;
            if (block.Start > 0)
            {
                val1 = matrix[block.Start - 1, block.Column];
            }
            if (block.Start + block.Length < matrix.RowCount)
            {
                val2 = matrix[block.Start + block.Length, block.Column];
            }

            double step;

            // fallback case - no 2nd value for interpolation
            if (Double.IsNaN(val1) && Double.IsNaN(val2) || block.Length * 4 >= matrix.RowCount && smartInterpolate)
            {
                val1 = 0.0;
                step = 0;
            }
            else if (Double.IsNaN(val1)) // start block is missing
            {
                val1 = val2;
                step = 0;
            }
            else if (Double.IsNaN(val2)) // end block is missing
            {
                step = 0;
            }
            else
            {
                step = (val2 - val1) / (block.Length + 1);
            }

            for (int i = 0; i < block.Length; ++i)
            {
                matrix[block.Start + i, block.Column] = val1 + step * (i + 1);
            }
        }
    }

    /// <summary>
    /// Adapted from python implementation from https://github.com/ATNoG/adjusted-mf
    /// Code accompanies paper "Misalignment problem in matrix decomposition with missing values"
    ///     by Sofia Fernandes, Mário Antunes, Diogo Gomes and Rui L. Aguiar
    /// </summary>
    public static void ReAlignImputation(ref Matrix<double> mat, List<MissingBlock> missingBlocks)
    {
        // we will need interpolation values of all blocks
        // while a bit wasteful, we will do this through a matrix copy
        Matrix<double> interpolated = mat.Clone();
        Interpolate(ref interpolated, missingBlocks);
    
        foreach (MissingBlock mb in missingBlocks)
        {
            Vector<double> Yr = interpolated.GetBlock(mb);
            Vector<double> Xr = mat.GetBlock(mb);
        
            // get "linear" trend
            Vector<double> Zr = MathX.Zeros(mb.Length);

            double xLeft = Xr[0];
            double xRight = Xr[mb.Length - 1];
            double deltaX = (xRight - xLeft) / (mb.Length + 1);
            for (int i = 0; i < mb.Length; i++)
            {
                Zr[i] = xLeft + (i + 1) * deltaX;
            }

            // align and put back into the matrix
            Xr -= Zr - Yr;
            TimeSeries.SetBlock(ref mat, mb, Xr);
        }
    }
}
//
// Created by zakhar on 02/11/2021.
//

#include "armawrap.h"
#include <armadillo>

extern "C"
{

void
proxy_arma_svd_econ(
    double *matrixNative, uint64_t dimN, uint64_t dimM,
    double *uContainer, double *sigmaContainer, double *vContainer
)
{
    /* Dimensional reminder:
    
    //Matrix<double>
    double[] matrix = new double[rows * columns];
    
    //Matrix<double>
    double[] uArray = new double[rows * sharedDim];
    
    //Vector<double>
    double[] sArray = new double[sharedDim];
    
    //Matrix<double>
    double[] vArray = new double[columns * sharedDim];
    
    */

    uint64_t sharedDim = std::min(dimN, dimM);
    
    // For armadillo docs see: https://arma.sourceforge.net/docs.html

    // constructor in question:
    // mat(ptr_aux_mem, n_rows, n_cols, copy_aux_mem, strict)
    // Docs URL: https://arma.sourceforge.net/docs.html#adv_constructors_mat
    arma::mat matrix(matrixNative, dimN, dimM, false, true);
    
    //   * ptr_aux_mem = matrixNative
    //       -> a C pointer to a memory block with the matrix; column-major
    //   * n_rows = dimN, n_cols = dimN
    //       -> size of the matrix, such that sizeof(ptr_aux_mem) == n_rows * n_cols
    //   * copy_aux_mem = false
    //       -> we tell arma to NOT allocate new memory and use ptr_aux_mem; mostly for performance
    //          this will be managed memory pointer from .NET's CLR
    //   * strict = true
    //       -> we tell armadillo to refuse any matrix resize operations
    //          those would reallocate and/or invalidate ptr_aux_mem;
    //          we don't ask for those here, so it's mostly an edge-case safety feature

    arma::mat U;
    arma::vec S;
    arma::mat V;

    bool code = arma::svd_econ(U, S, V, matrix, "both", "std");

    if (!code)
    {
        // svd failed - fill all the data with NaNs
        // is treated upstream in C# code as a signal for SVD failure
        std::fill_n(uContainer, dimN * sharedDim, std::numeric_limits<double>::quiet_NaN());
        std::fill_n(sigmaContainer, sharedDim, std::numeric_limits<double>::quiet_NaN());
        std::fill_n(vContainer, sharedDim * dimM, std::numeric_limits<double>::quiet_NaN());
    }
    else
    {
        // svd successful - put the result inside the provided pointers
        std::copy_n(U.memptr(), dimN * sharedDim, uContainer);
        std::copy_n(S.memptr(), sharedDim, sigmaContainer);
        std::copy_n(V.memptr(), sharedDim * dimM, vContainer);
    }
    // nothing else to do or return, the result is already filled into resp. containers
}

}
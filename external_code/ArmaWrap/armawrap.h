//
// Created by zakhar on 02/11/2021.
//

#pragma once

#include <cstdlib>
#include <cstdint>
#include <tuple>

// exposed API
extern "C"
{

void
proxy_arma_svd_econ(
    double *matrixNative, uint64_t dimN, uint64_t dimM,
    double *uContainer, double *sigmaContainer, double *vContainer
);

}
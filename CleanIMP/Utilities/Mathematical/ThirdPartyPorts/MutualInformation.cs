using System;
using System.Linq;

using Accord.Collections;
using Accord.Math.Distances;

namespace CleanIMP.Utilities.Mathematical.ThirdPartyPorts;

// Zakhar:
// Code ported to C# from python
// Using scipy's version as the starting point: https://github.com/scikit-learn/scikit-learn/blob/main/sklearn/feature_selection/_mutual_info.py
// Functionality is adapted for C# and data types required by Accord.NET

// Scikit-learn: Machine Learning in Python, Pedregosa et al., JMLR 12, pp. 2825-2830, 2011.

// Original Author: Nikolay Mayorov <n59_ru@hotmail.com>
// License: 3-clause BSD

public static class MutualInformation
{
    public static double KraskovMutualInfo(double[] vector1, double[] vector2, int k = 3)
    {
        double[][] x = vector1.Select(v => new[] {v}).ToArray();
        double[][] y = vector2.Select(v => new[] {v}).ToArray();
        double[][] xy = vector1.Select((e, i) => new[] {e, vector2[i]}).ToArray();
        
        KDTree<double> kdx = KDTree.FromData<double>(x);
        KDTree<double> kdy = KDTree.FromData<double>(y);
        KDTree<double> nn = KDTree.FromData<double>(xy);
        kdx.Distance = kdy.Distance = nn.Distance = new Chebyshev();

        double[] cntX = new double [xy.Length];
        double[] cntY = new double [xy.Length];

        for (int i = 0; i < xy.Length; i++)
        {
            // step 1 - get correct radius from each point of xy
            double radius = nn
                .Nearest(xy[i], k + 1) //since we query by point, self-node is returned, needs +1 correction
                .Select(nd => nd.Distance - 1E-12) // -eps to avoid equalities
                .Max(); // originally it's .Last(), but I don't now if the library guarantees the order

            // step 2
            cntX[i] = kdx.Nearest(x[i], radius).Count;
            cntY[i] = kdy.Nearest(y[i], radius).Count;
            // the counter includes self-node. it shouldn't, but later it's fed to the formula of psi(cnt + 1)
            //     so by doing nothing in both cases they cancel each other out
            // also, since there's always a self-node, ergo cnt >= 1, psi(cnt) is guaranteed defined on R \ Z_{<=0}
        }

        double mi = (CephesPsi.psi(xy.Length) + CephesPsi.psi(k) - cntX.Select(CephesPsi.psi).Average() - cntY.Select(CephesPsi.psi).Average());
        
        return Math.Max(0.0, mi);
    }
}

internal class Chebyshev : IMetric<double[]>
{
    public double Distance(double[] x, double[] y) => Accord.Math.Distance.Chebyshev(x, y);
}
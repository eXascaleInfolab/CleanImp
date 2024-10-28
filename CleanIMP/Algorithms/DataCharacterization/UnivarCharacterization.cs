using System;
using System.Collections.Generic;
using System.Linq;
using CleanIMP.Testing;
using CleanIMP.Utilities;
using CleanIMP.Utilities.Mathematical;
using CleanIMP.Utilities.Mathematical.ThirdPartyPorts;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

namespace CleanIMP.Algorithms.DataCharacterization;

internal static class UnivarCharacterization
{
    public static (string, double)[] ExtractDatasetFeatures(UnivarDataset dataset)
    {
        int series = dataset.Train.Count;
        int pairs = MathX.CountUniquePairs(dataset.Train.Count);
        List<(string, double)> features = new();
        double mean = 0.0, stddev = 0.0;
        
        // Reusable variables
        Vector<double> similaritiesFlat = MathX.Zeros(pairs);
        List<double> similaritiesInClass = new();
        List<double> similaritiesOutClass = new();

        // Feature: correlation
        Matrix<double> similarity = TimeSeries.SimilarityMatrix(dataset, "pearson");

        foreach ((int i, int j) in LinqX.AllPairsUnique(series))
        {
            double sim = similarity[i, j];
            similaritiesFlat[MathX.PairToFlatIndexUnique(i, j, series)] = sim;
            if (dataset.Train[i].TrueClass == dataset.Train[j].TrueClass)
            {
                similaritiesInClass.Add(sim);
            }
            else
            {
                similaritiesOutClass.Add(sim);
            }
        }

        (mean, stddev) = similaritiesFlat.MeanStandardDeviation();
        features.Add(("corr-mean", mean));
        features.Add(("corr-stddev", stddev));

        (mean, stddev) = similaritiesInClass.MeanStandardDeviation();
        features.Add(("corr-in-mean", mean));
        features.Add(("corr-in-stddev", stddev));

        (mean, stddev) = similaritiesOutClass.MeanStandardDeviation();
        features.Add(("corr-out-mean", mean));
        features.Add(("corr-out-stddev", stddev));
        
        // Feature: shift
        similaritiesFlat.Clear();
        similaritiesInClass.Clear();
        similaritiesOutClass.Clear();

        foreach ((int i, int j) in LinqX.AllPairsUnique(series))
        {
            int shiftIdx = KShape.ShiftIndex(dataset.Train[i].Vector, dataset.Train[j].Vector);
            double sim = Math.Abs(shiftIdx) / (double)dataset.Train.First().Vector.Count;
            
            similaritiesFlat[MathX.PairToFlatIndexUnique(i, j, series)] = sim;
            if (dataset.Train[i].TrueClass == dataset.Train[j].TrueClass)
            {
                similaritiesInClass.Add(sim);
            }
            else
            {
                similaritiesOutClass.Add(sim);
            }
        }

        (mean, stddev) = similaritiesFlat.MeanStandardDeviation();
        features.Add(("shift-mean", mean));
        features.Add(("shift-stddev", stddev));

        (mean, stddev) = similaritiesInClass.MeanStandardDeviation();
        features.Add(("shift-in-mean", mean));
        features.Add(("shift-in-stddev", stddev));

        (mean, stddev) = similaritiesOutClass.MeanStandardDeviation();
        features.Add(("shift-out-mean", mean));
        features.Add(("shift-out-stddev", stddev));
        
        // Feature: shape-based similarity
        similaritiesFlat.Clear();
        similaritiesInClass.Clear();
        similaritiesOutClass.Clear();

        foreach ((int i, int j) in LinqX.AllPairsUnique(series))
        {
            double sim = KShape.ShapeBasedDistance(dataset.Train[i].Vector, dataset.Train[j].Vector);
            
            similaritiesFlat[MathX.PairToFlatIndexUnique(i, j, series)] = sim;
            if (dataset.Train[i].TrueClass == dataset.Train[j].TrueClass)
            {
                similaritiesInClass.Add(sim);
            }
            else
            {
                similaritiesOutClass.Add(sim);
            }
        }

        (mean, stddev) = similaritiesFlat.MeanStandardDeviation();
        features.Add(("sbd-mean", mean));
        features.Add(("sbd-stddev", stddev));

        (mean, stddev) = similaritiesInClass.MeanStandardDeviation();
        features.Add(("sbd-in-mean", mean));
        features.Add(("sbd-in-stddev", stddev));

        (mean, stddev) = similaritiesOutClass.MeanStandardDeviation();
        features.Add(("sbd-out-mean", mean));
        features.Add(("sbd-out-stddev", stddev));

        return features.ToArray();
    }
}
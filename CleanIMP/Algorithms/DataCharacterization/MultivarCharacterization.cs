using System;
using System.Collections.Generic;
using CleanIMP.Testing;
using CleanIMP.Utilities;
using CleanIMP.Utilities.Mathematical;
using CleanIMP.Utilities.Mathematical.ThirdPartyPorts;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;

namespace CleanIMP.Algorithms.DataCharacterization;

public static class MultivarCharacterization
{
    public static (string, double)[] ExtractDatasetFeatures(ForecastDataset dataset)
    {
        int series = dataset.TsCount();
        int len = dataset.TsLen();
        int pairs = MathX.CountUniquePairs(series);
        List<(string, double)> features = new();
        double mean = 0.0, stddev = 0.0;
        
        // Reusable variables
        Vector<double> similaritiesFlat = MathX.Zeros(pairs);
        List<double> similaritiesFirst = new();

        // Feature: correlation
        Matrix<double> similarity = TimeSeries.SimilarityMatrix(dataset.Train, "pearson");

        foreach ((int i, int j) in LinqX.AllPairsUnique(series))
        {
            double sim = similarity[i, j];
            similaritiesFlat[MathX.PairToFlatIndexUnique(i, j, series)] = sim;
            if (i == 0) // TS#0
            {
                similaritiesFirst.Add(sim);
            }
        }

        (mean, stddev) = similaritiesFlat.MeanStandardDeviation();
        features.Add(("corr-mean", mean));
        features.Add(("corr-stddev", stddev));

        (mean, stddev) = similaritiesFirst.MeanStandardDeviation();
        features.Add(("corr-ts0-mean", mean));
        features.Add(("corr-ts0-stddev", stddev));

        // Feature: absolute correlation
        similaritiesFlat.Clear();
        similaritiesFirst.Clear();

        foreach ((int i, int j) in LinqX.AllPairsUnique(series))
        {
            double sim = Math.Abs(similarity[i, j]);
            similaritiesFlat[MathX.PairToFlatIndexUnique(i, j, series)] = sim;
            if (i == 0) // TS#0
            {
                similaritiesFirst.Add(sim);
            }
        }

        (mean, stddev) = similaritiesFlat.MeanStandardDeviation();
        features.Add(("abscorr-mean", mean));
        features.Add(("abscorr-stddev", stddev));

        (mean, stddev) = similaritiesFirst.MeanStandardDeviation();
        features.Add(("abscorr-ts0-mean", mean));
        features.Add(("abscorr-ts0-stddev", stddev));
        
        // Feature: shift
        similaritiesFlat.Clear();
        similaritiesFirst.Clear();

        foreach ((int i, int j) in LinqX.AllPairsUnique(series))
        {
            int shiftIdx = KShape.ShiftIndex(dataset.Train.Column(i), dataset.Train.Column(j));
            double sim = Math.Abs(shiftIdx) / (double)len;
            
            similaritiesFlat[MathX.PairToFlatIndexUnique(i, j, series)] = sim;
            if (i == 0) // TS#0
            {
                similaritiesFirst.Add(sim);
            }
        }

        (mean, stddev) = similaritiesFlat.MeanStandardDeviation();
        features.Add(("shift-mean", mean));
        features.Add(("shift-stddev", stddev));

        (mean, stddev) = similaritiesFirst.MeanStandardDeviation();
        features.Add(("shift-ts0-mean", mean));
        features.Add(("shift-ts0-stddev", stddev));
        
        // Feature: shape-based similarity
        similaritiesFlat.Clear();
        similaritiesFirst.Clear();

        foreach ((int i, int j) in LinqX.AllPairsUnique(series))
        {
            double sim = KShape.ShapeBasedDistance(dataset.Train.Column(i), dataset.Train.Column(j));
            
            similaritiesFlat[MathX.PairToFlatIndexUnique(i, j, series)] = sim;
            if (i == 0) // TS#0
            {
                similaritiesFirst.Add(sim);
            }
        }

        (mean, stddev) = similaritiesFlat.MeanStandardDeviation();
        features.Add(("sbd-mean", mean));
        features.Add(("sbd-stddev", stddev));

        (mean, stddev) = similaritiesFirst.MeanStandardDeviation();
        features.Add(("sbd-ts0-mean", mean));
        features.Add(("sbd-ts0-stddev", stddev));

        return features.ToArray();
    }
}
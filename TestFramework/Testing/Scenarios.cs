using System;
using System.Collections.Generic;
using System.Linq;

using CleanIMP.Utilities;
using CleanIMP.Utilities.Mathematical;

namespace CleanIMP.Testing;

// Enums with lists of scenarios

public enum ScenarioMv
{
    MissingPercentage,
    MissingPercentageReverse,
    MultiColumnDisjoint,
    MultiColumnReverse,
    MultiColumnReverseDNI,
    MultiColumn10,
    MultiColumn20,
    Mcar,
}

public enum ScenarioUni
{
    MissingPercentage10,
    MissingPercentage20,
    MissingPercentage30,
    MissingPercentage40,
    MissingPercentage50,
    MultiColumn10,
    MultiColumn20,
    MultiColumn30,
    MultiColumn40,
    MultiColumn50,
    Mcar,
    Mcar20,
    Mcar30,
}

// API for their usage in tests & analytics

/// <summary>
/// Base class for a scenario type.
/// Contains all necessary methods to perform arbitrary experiments with anything that qualifies as a scenario.
/// Specifics on how to use <see cref="MissingBlock"/> are handled by the task that uses those.
/// </summary>
/// <remarks>
/// Functionally it's closer to an abstract class, however implementation constraints (related to 'static abstract' keyword) force it to be an interface.
/// </remarks>
public interface IScenario<out TScen>
    where TScen : IScenario<TScen>
{
    // Static methods
    static abstract IEnumerable<TScen> AllScenarios();
    static abstract TScen CreateByName(string scenName);
    
    // Instance methods
    
    IEnumerable<int> Ticks(int rows, int cols);

    IEnumerable<MissingBlock> GetContamination(int rows, int cols, int tick, int seed);
}

/// <summary>
/// Type responsible for missing value scenarios related to multivariate time series.
/// </summary>
/// <remarks>
/// Generally applicable for cases where a single time series having missing values in impactful.
/// I.e. low number of time series per unit of imputation. 
/// </remarks>
public sealed class ScenarioMultivariate : IScenario<ScenarioMultivariate>
{
    // Fields
    private ScenarioMv _scen;

    // Static methods
    public static ScenarioMultivariate CreateByName(string scenName)
    {
        ScenarioMultivariate? newScen = AllScenarios().FirstOrDefault(sc => sc.ToString().ToLower() == scenName);
        return newScen ?? throw new ArgumentException("One or more provided scenarios in the config are not valid.");
    }

    public static implicit operator ScenarioMultivariate(ScenarioMv scen) => new() {_scen = scen};

    // Instance methods
    public static IEnumerable<ScenarioMultivariate> AllScenarios()
    {
        yield return ScenarioMv.MissingPercentage;
        yield return ScenarioMv.MissingPercentageReverse;
        yield return ScenarioMv.MultiColumnDisjoint;
        yield return ScenarioMv.MultiColumnReverse;
        yield return ScenarioMv.MultiColumnReverseDNI;
        yield return ScenarioMv.MultiColumn10;
        yield return ScenarioMv.MultiColumn20;
        yield return ScenarioMv.Mcar;
    }

    public IEnumerable<int> Ticks(int rows, int cols) => _scen switch
    {
        ScenarioMv.MissingPercentage => LinqX.ClosedSequence(10, 80, 10), //%
        ScenarioMv.MissingPercentageReverse => LinqX.ClosedSequence(10, 80, 10), //%
        ScenarioMv.MultiColumnDisjoint => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioMv.MultiColumnReverse => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioMv.MultiColumnReverseDNI => LinqX.ClosedSequence(10, 90, 10), //%
        ScenarioMv.MultiColumn10 => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioMv.MultiColumn20 => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioMv.Mcar => LinqX.ClosedSequence(10, 100, 10), //%
        
        _ => throw new Exception("Invalid or unimplemented contamination scenario")
    };

    public IEnumerable<MissingBlock> GetContamination(int rows, int cols, int tick, int seed)
    {
        Random rand = new(seed);
        double djPercent = 100.0 / cols;

        return _scen switch
        {
            ScenarioMv.MissingPercentage => new MissingBlock[] {(0, rows / 10, (rows * tick) / 100)},
            ScenarioMv.MissingPercentageReverse => new MissingBlock[] {(0, rows - (rows * tick) / 100, (rows * tick) / 100)},
            ScenarioMv.MultiColumnDisjoint => ScenarioHelpers.GetMultiColumn(rows, cols, djPercent).Take((cols * tick) / 100),
            ScenarioMv.MultiColumnReverse => ScenarioHelpers.GetReverse(rows, cols, tick),
            ScenarioMv.MultiColumnReverseDNI => ScenarioHelpers.GetReverse(rows, cols, tick),
            ScenarioMv.MultiColumn10 => ScenarioHelpers.GetMultiColumn(rows, cols, 10.0).Take((cols * tick) / 100),
            ScenarioMv.MultiColumn20 => ScenarioHelpers.GetMultiColumn(rows, cols, 20.0).Take((cols * tick) / 100),
            ScenarioMv.Mcar => ScenarioHelpers.GetMcarBlocks(rows, cols, tick, 10, 10, rand),
            
            _ => throw new Exception("Invalid or unimplemented contamination scenario")
        };
    }

    public override string ToString()
    {
        return _scen switch
        {
            ScenarioMv.MissingPercentage => "miss_perc",
            ScenarioMv.MissingPercentageReverse => "miss_perc_rev",
            ScenarioMv.MultiColumnDisjoint => "mc_dj",
            ScenarioMv.MultiColumnReverse => "mc_rev",
            ScenarioMv.MultiColumnReverseDNI => "mc_rev_dni",
            ScenarioMv.MultiColumn10 => "mc_10",
            ScenarioMv.MultiColumn20 => "mc_20",
            ScenarioMv.Mcar => "mcar",
            
            _ => throw new Exception("Invalid or unimplemented contamination scenario")
        };
    }
}

/// <summary>
/// Type responsible for missing value scenarios related to collections of univariate time series.
/// </summary>
/// <remarks>
/// Generally applicable for cases where a single time series having missing values has minimal impact.
/// I.e. a (very) large number of time series per unit of imputation.
/// </remarks>
public sealed class ScenarioUnivariate : IScenario<ScenarioUnivariate>
{
    // fields
    private ScenarioUni _scen;

    // Static methods
    public static ScenarioUnivariate CreateByName(string scenName)
    {
        ScenarioUnivariate? newScen = AllScenarios().FirstOrDefault(sc => sc.ToString().ToLower() == scenName);
        return newScen ?? throw new ArgumentException("One or more provided scenarios in the config are not valid.");
    }

    public static implicit operator ScenarioUnivariate(ScenarioUni scen) => new() {_scen = scen};

    // Instance methods
    public static IEnumerable<ScenarioUnivariate> AllScenarios()
    {
        yield return ScenarioUni.MissingPercentage10;
        yield return ScenarioUni.MissingPercentage20;
        yield return ScenarioUni.MissingPercentage30;
        yield return ScenarioUni.MissingPercentage40;
        yield return ScenarioUni.MissingPercentage50;
        yield return ScenarioUni.MultiColumn10;
        yield return ScenarioUni.MultiColumn20;
        yield return ScenarioUni.MultiColumn30;
        yield return ScenarioUni.MultiColumn40;
        yield return ScenarioUni.MultiColumn50;
        yield return ScenarioUni.Mcar;
        yield return ScenarioUni.Mcar20;
        yield return ScenarioUni.Mcar30;
    }

    public IEnumerable<int> Ticks(int rows, int cols) => _scen switch
    {
        ScenarioUni.MissingPercentage10 => LinqX.ClosedSequence(10, 80, 10), //%
        ScenarioUni.MissingPercentage20 => LinqX.ClosedSequence(10, 80, 10), //%
        ScenarioUni.MissingPercentage30 => LinqX.ClosedSequence(10, 80, 10), //%
        ScenarioUni.MissingPercentage40 => LinqX.ClosedSequence(10, 80, 10), //%
        ScenarioUni.MissingPercentage50 => LinqX.ClosedSequence(10, 80, 10), //%
            
        ScenarioUni.MultiColumn10 => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioUni.MultiColumn20 => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioUni.MultiColumn30 => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioUni.MultiColumn40 => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioUni.MultiColumn50 => LinqX.ClosedSequence(10, 100, 10), //%
        
        ScenarioUni.Mcar => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioUni.Mcar20 => LinqX.ClosedSequence(10, 100, 10), //%
        ScenarioUni.Mcar30 => LinqX.ClosedSequence(10, 100, 10), //%
            
        _ => throw new Exception("Invalid or unimplemented contamination scenario")
    };

    public IEnumerable<MissingBlock> GetContamination(int rows, int cols, int tick, int seed)
    {
        Random rand = new(seed);

        return _scen switch
        {
            // key idea - tick% size of MB (as per miss%) and after calling GetMCR/4 we have 100% of ts's with 1 MB
            // now we simply sample 10%/20%/... of those and that's it
            // maximum MV saturation @last tick - 100% * 0.8 * [NN]% = [NN*0.8]%
            ScenarioUni.MissingPercentage10 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, tick, rand).ToArray(), (int)(cols * 0.1)),
            ScenarioUni.MissingPercentage20 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, tick, rand).ToArray(), (int)(cols * 0.2)),
            ScenarioUni.MissingPercentage30 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, tick, rand).ToArray(), (int)(cols * 0.3)),
            ScenarioUni.MissingPercentage40 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, tick, rand).ToArray(), (int)(cols * 0.4)),
            ScenarioUni.MissingPercentage50 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, tick, rand).ToArray(), (int)(cols * 0.5)),
            // key idea - we 10%/20%/... size of MB, 1 per column, sample tick% of columns
            // maximum MV saturation @last tick - 100% * 1.0 * NN% = NN%
            ScenarioUni.MultiColumn10 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, 10.0, rand).ToArray(), (cols * tick) / 100),
            ScenarioUni.MultiColumn20 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, 20.0, rand).ToArray(), (cols * tick) / 100),
            ScenarioUni.MultiColumn30 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, 30.0, rand).ToArray(), (cols * tick) / 100),
            ScenarioUni.MultiColumn40 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, 40.0, rand).ToArray(), (cols * tick) / 100),
            ScenarioUni.MultiColumn50 => rand.RandomSample(ScenarioHelpers.GetMultiColumnRandom(rows, cols, 50.0, rand).ToArray(), (cols * tick) / 100),
            // key idea - generate mcar with tick% AC's; this however has active columns sequentially, we need random ones
            // so what we do is just pass this generation into the function that will use a permutation to substitute col idx
            // maximum MV saturation @last tick - hardcoded [percent]% percent
            ScenarioUni.Mcar => ScenarioHelpers.GetMcarBlocks(rows, cols, tick, 10, 10, rand).PermuteColumns(cols, tick, rand),
            ScenarioUni.Mcar20 => ScenarioHelpers.GetMcarBlocks(rows, cols, tick, 10, 20, rand).PermuteColumns(cols, tick, rand),
            ScenarioUni.Mcar30 => ScenarioHelpers.GetMcarBlocks(rows, cols, tick, 10, 30, rand).PermuteColumns(cols, tick, rand),
            
            _ => throw new Exception("Invalid or unimplemented contamination scenario")
        };
    }

    public override string ToString()
    {
        return _scen switch
        {
            ScenarioUni.MissingPercentage10 => "miss_perc10",
            ScenarioUni.MissingPercentage20 => "miss_perc20",
            ScenarioUni.MissingPercentage30 => "miss_perc30",
            ScenarioUni.MissingPercentage40 => "miss_perc40",
            ScenarioUni.MissingPercentage50 => "miss_perc50",
            ScenarioUni.MultiColumn10 => "mc_10",
            ScenarioUni.MultiColumn20 => "mc_20",
            ScenarioUni.MultiColumn30 => "mc_30",
            ScenarioUni.MultiColumn40 => "mc_40",
            ScenarioUni.MultiColumn50 => "mc_50",
            ScenarioUni.Mcar => "mcar",
            ScenarioUni.Mcar20 => "mcar20",
            ScenarioUni.Mcar30 => "mcar30",

            _ => throw new Exception("Invalid or unimplemented contamination scenario")
        };
    }
}

/// <summary>
/// Helper class with methods to construct missing value patterns, almost regardless of scenario and scenario type.
/// </summary>
public static class ScenarioHelpers
{
    internal static IEnumerable<MissingBlock> GetMultiColumn(int rows, int cols, double percentage)
    {
        if (percentage is <= 0.0 or > 100.0)
        {
            throw new ArgumentException(
                "Invalid multi-column scenario specification - percentage value is outside of ]0,100] range.");
        }
        int blockLen = Math.Max((int)((rows * percentage) / 100), 1);
        int loop = (int)(100 / percentage);
        
        return Enumerable.Range(0, cols)
            .Select(col => new MissingBlock(col, (col % loop) * blockLen, blockLen));
    }

    internal static IEnumerable<MissingBlock> GetReverse(int rows, int cols, int tick)
    {
        int blockLen = rows  / 10; // 10% const
        int affectedCols = (cols * tick) / 100; // tick%
        int blockPosSeed = rows / cols;

        for (int j = 0; j < affectedCols; j++)
        {
            int endPos = rows - j * blockPosSeed;
            int startPos = Math.Max(endPos - blockLen, 0);
            if (startPos + blockLen > rows) throw new Exception("Scenario generation failed.");
            yield return new MissingBlock(j, startPos, blockLen);
        }
    }

    internal static IEnumerable<MissingBlock> GetMultiColumnRandom(int rows, int cols, double percentage, Random rand)
    {
        if (percentage is <= 0.0 or > 100.0)
        {
            throw new ArgumentException(
                "Invalid multi-column scenario specification - percentage value is outside of ]0,100] range.");
        }
        int blockLen = Math.Max((int)((rows * percentage) / 100), 1);
        
        return Enumerable.Range(0, cols)
            .Select(col => new MissingBlock(col, rand.Next(0, rows - blockLen), blockLen));
    }

    internal static IEnumerable<MissingBlock> GetMcarBlocks(int rows, int cols, int tick, int block, int percent, Random rand)
    {
        if (percent is <= 0 or >= 80)
        {
            throw new ArgumentException(
                "Invalid mcar scenario specification - percentage value is outside of ]0,80[ range.");
        }
        int activeColumns = (cols * tick) / 100; // tick%
        int rmBlocks = Math.Max((rows * activeColumns * percent) / (100 * block), activeColumns);
        // ^ we aim at percent% saturation of missing values in active columns
        // so we remove (on average) p% of rows from each column, which is = rows * active_col * percent/100
        // then we divide this by block to know the number of blocks we need to remove
        // for short matrices (sub-100) we have a fallback - at least (on average) 1 block per column

        List<MissingBlock> blocks = new();
        Dictionary<int, List<int>> columnIdx = Enumerable.Range(0, activeColumns)
            .ToDictionary(idx => idx, _ => Enumerable.Range(0, rows / block).ToList());
        
        for (int i = 0; i < rmBlocks; i++)
        {
            int col = columnIdx.Keys.ElementAt(rand.Next(columnIdx.Count));
            int row = columnIdx[col][rand.Next(columnIdx[col].Count)];
            blocks.Add(new MissingBlock(col, row * block, block));
            
            columnIdx[col].Remove(row);
            // make sure we don't remove more than 80% of column's values, hardcoded restriction
            if (columnIdx[col].Count <= Math.Max(1, (rows / block) / 5)) columnIdx.Remove(col);
        }

        return TimeSeries.MergeConsecutive(blocks);
    }

    internal static IEnumerable<MissingBlock> PermuteColumns(this IEnumerable<MissingBlock> blocks, int cols, int tick, Random rand)
    {
        int activeColumns = (cols * tick) / 100; // tick%

        int[] perm = rand.NextPermutation(cols, activeColumns);

        foreach (MissingBlock block in blocks)
        {
            yield return new MissingBlock(perm[block.Column], block.Start, block.Length);
        }
    }
}


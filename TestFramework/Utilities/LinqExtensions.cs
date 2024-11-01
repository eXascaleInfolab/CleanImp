using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace CleanIMP.Utilities;

/// <summary>
/// A class for custom utility extension methods facilitating the use of LINQ.
/// </summary>
public static class LinqExtensions
{
    /// <summary>
    /// Performs an <paramref name="action"/> on every member of the collection and its index.
    /// </summary>
    /// <param name="collection">A collection to perform an <paramref name="action"/> on</param>
    /// <param name="action">Action to perform on the collection elements and their index</param>
    public static void ForEach<T>(this IEnumerable<T> collection, Action<T, int> action)
    {
        int counter = 0;
        foreach (T item in collection)
        {
            action(item, counter);
            counter++;
        }
    }

    /// <summary>
    /// Finds an index of an element in the array.
    /// </summary>
    /// <param name="array">Array to the search in.</param>
    /// <param name="elem">Element to find in the <paramref name="array"/>.</param>
    /// <returns>An index of an element or -1 if it's not found.</returns>
    public static int IndexOf<T>(this T[] array, T elem)
    where T : IEquatable<T>
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].Equals(elem))
                return i;
        }

        return -1;
    }

    // ReSharper disable once InconsistentNaming
    public static IEnumerable<T> WhereNOT<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        => collection.Where(e => !predicate(e));

    /// <summary>
    /// Takes a <paramref name="collection"/> of collections and returns a collection where each member is a member of a sub-collection.
    /// </summary>
    /// <param name="collection">A collection of collections.</param>
    public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> collection)
    {
        return collection.SelectMany(x => x);
    }

    public static (T, IEnumerable<T>) HeadTail<T>(this IEnumerable<T> collection)
    {
        using IEnumerator<T> enumerator = collection.GetEnumerator();
        enumerator.MoveNext();
        T head = enumerator.Current;
        return (head, TailGetter(enumerator));

        IEnumerable<T> TailGetter(IEnumerator<T> internalEnumerator)
        {
            while (internalEnumerator.MoveNext())
                yield return internalEnumerator.Current;
        }
    }

    public static T[][] ArrayTranspose<T>(this T[][] array)
    {
        T[][] newArray = new T[array[0].Length][];
        
        for (int j = 0; j < array[0].Length; j++)
        {
            newArray[j] = new T[array.Length];
        }
        
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].Length != array[0].Length)
            {
                throw new ArgumentException("Unsupported input: nested arrays have different length, the array can not be transposed.");
            }
            for (int j = 0; j < array[i].Length; j++)
            {
                newArray[j][i] = array[i][j];
            }
        }

        return newArray;
    }

    /// <summary>
    /// Takes a <paramref name="stringCollection"/> and joins them into a single string.
    /// All elements are separated by <paramref name="joiner"/>.
    /// </summary>
    /// <param name="stringCollection">A collection to contract into a single string.</param>
    /// <param name="joiner">A string that interpolates elements of <paramref name="stringCollection"/>. Default value is a blank space.</param>
    public static string StringJoin(this IEnumerable<string> stringCollection, string joiner = " ")
    {
        IEnumerable<string> enumerated = stringCollection as string[] ??
                                         stringCollection as IList<string> ?? stringCollection.ToArray();

        int count = enumerated.Count();
        var result = new StringBuilder();

        for (int i = 0; i < count - 1; i++)
        {
            result.Append(enumerated.ElementAt(i));
            result.Append(joiner);
        }

        result.Append(enumerated.ElementAt(count - 1));

        return result.ToString();
    }
    
    /// <summary>
    /// Takes a <paramref name="collection"/>, converts its elements to strings and joins them into a single string.
    /// All elements are separated by <paramref name="joiner"/>.
    /// </summary>
    /// <param name="collection">A collection to convert to string and contract into a single string.</param>
    /// <param name="joiner">A string that interpolates elements of <paramref name="collection"/>. Default value is a blank space.</param>
    public static string StringJoin<T>(this IEnumerable<T> collection, string joiner = " ")
        => collection.Select(x => $"{x}").StringJoin(joiner);

    public static Dictionary<TKey, TValue> ToDictionaryX<TKey, TValue>(this IEnumerable<(TKey, TValue)> collection)
        where TKey : struct
    {
        return collection.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
    }

    public static TValue Consume<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
    {
        if (!dictionary.ContainsKey(key))
        {
            throw new ArgumentException("The dictionary doesn't contain the key to consume");
        }

        TValue val = dictionary[key];
        dictionary.Remove(key);
        return val;
    }
}

public static class LinqX
{
    // /////////// //
    // Generators //
    // ///////// //

    /// <summary>
    /// Generates a set X = { x_i | x_i = <paramref name="start"/> + i * <paramref name="step"/>, i = 0 ... inf, x_i &lt;= <paramref name="end"/> }.
    /// </summary>
    /// <returns>The sequence.</returns>
    /// <param name="start">Start of the sequence.</param>
    /// <param name="end">End of the sequence.</param>
    /// <param name="step">Step size (default value is 1).</param>
    public static IEnumerable<int> ClosedSequence(int start, int end, int step = 1)
    {
        for (int i = start; i <= end; i += step)
        {
            yield return i;
        }
    }

    public static IEnumerable<(int, int)> AllPairsDistinct(int elements)
    {
        for (int i = 0; i < elements; i++)
        {
            for (int j = 0; j < elements; j++)
            {
                if (i != j) yield return (i, j);
            }
        }
    }

    public static IEnumerable<(int, int)> AllPairsUnique(int elements)
    {
        for (int i = 0; i < elements; i++)
        {
            for (int j = i + 1; j < elements; j++)
            {
                yield return (i, j);
            }
        }
    }
}
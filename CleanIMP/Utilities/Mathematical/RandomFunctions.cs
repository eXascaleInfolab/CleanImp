using System;
using System.Collections.Generic;
using System.Linq;

namespace CleanIMP.Utilities.Mathematical;

public static class RandomExtensions
{
    public static int[] NextPermutation(this Random rand, int n, int k, int startingIndex = 0)
    {
        int[] result = Enumerable.Range(0, n).ToArray();

        for (int i = startingIndex; i < k; i++)
        {
            int next = rand.Next(i, n);

            if (next != i) //guaranteed true for n==k, but required for n!=k
            {
                (result[i], result[next]) = (result[next], result[i]);
            }
        }

        if (n == k)
        {
            return result;
        }
        else
        {
            int[] res = new int[k];
            Array.Copy(result, startingIndex, res, 0, k);
            return res;
        }
    }
    
    public static T[] RandomSample<T>(this Random rand, ICollection<T> collection, int size)
    {
        int[] perm = rand.NextPermutation(collection.Count, size);
        T[] sample = new T[size];

        for (int i = 0; i < size; i++)
        {
            sample[i] = collection.ElementAt(perm[i]);
        }

        return sample;
    }
}
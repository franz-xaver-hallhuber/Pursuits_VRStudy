using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Correlation
{
    class Spearman
    {
        public static double calculateSpearman(List<double> a, List<double> b)
        {
            List<KeyValuePair<int, double>> _tA = convertAndSortByValue(a);
            List<KeyValuePair<int, double>> _tB = convertAndSortByValue(b);

            rankKeyValuePairs(ref _tA);
            rankKeyValuePairs(ref _tB);

            _tA.Sort(delegate (KeyValuePair<int, double> x, KeyValuePair<int, double> y) { return x.Key.CompareTo(y.Key); });
            _tB.Sort(delegate (KeyValuePair<int, double> x, KeyValuePair<int, double> y) { return x.Key.CompareTo(y.Key); });

            return (1 - (6 * sumOfDiffPow2(_tA, _tB) / (b.Count * (Math.Pow(b.Count, 2) - 1))));
        }

        static List<KeyValuePair<int, double>> convertAndSortByValue(List<double> sortedList)
        {
            List<KeyValuePair<int, double>> ret = new List<KeyValuePair<int, double>>();
            for (int i = 0; i < sortedList.Count; i++)
            {
                ret.Add(new KeyValuePair<int, double>(i, sortedList[i]));
            }

            ret.Sort(delegate (KeyValuePair<int, double> x, KeyValuePair<int, double> y)
            {
                return x.Value.CompareTo(y.Value);
            });

            return ret;
        }

        static void rankKeyValuePairs(ref List<KeyValuePair<int, double>> sortedList)
        {
            int count = 1, val = 0;
            for (int i = 1; i <= sortedList.Count; i++)
            {
                val += i;
                if (i < sortedList.Count && sortedList[i - 1].Value == sortedList[i].Value)
                {
                    count++;
                }
                else
                {
                    float pear = val / (float)count;
                    while (count >= 1)
                    {
                        sortedList[i - count] = new KeyValuePair<int, double>(sortedList[i - count].Key, pear);
                        count--;
                    }
                    count = 1;
                    val = 0;
                }
            }
        }

        static double sumOfDiffPow2(List<KeyValuePair<int, double>> rankedListA, List<KeyValuePair<int, double>> rankedListB)
        {
            double ret = 0;
            for (int i = 0; i < Math.Min(rankedListA.Count, rankedListB.Count); i++) ret += Math.Pow((rankedListA[i].Value - rankedListB[i].Value), 2);
            return ret;
        }
    }

    class Pearson
    {
        public static double calculatePearson(List<double> a, List<double> b)
        {
            double zaehler = 0, nenner = 0, nenner1 = 0, nenner2 = 0;

            // Debug.Log("Pearson List Difference: " + (a.Count - b.Count));

            for (int i = 0; i < Math.Min(a.Count, b.Count); i++)
            {
                zaehler += (a[i] - a.Average()) * (b[i] - b.Average()); // (_tempXPObj[i] - _tempXPObj.Average() is 0 when object is only moving along x-axis
                nenner1 += Math.Pow((a[i] - a.Average()), 2);
                nenner2 += Math.Pow((b[i] - b.Average()), 2);
            }

            nenner = nenner1 * nenner2;
            nenner = Math.Sqrt(nenner);

            return (zaehler / nenner);
        }
    }
}

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNet.Extensions
{
    // https://blogs.msdn.microsoft.com/pfxteam/2012/03/05/implementing-a-simple-foreachasync-part-2/
    public static class EnumerableExtensions
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> source, int dop, Func<T, Task> body) =>
            Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                    {
                        while (partition.MoveNext())
                        {
                            await body(partition.Current);
                        }
                    }
                }));

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (action is null) return;
            foreach (var item in source) action(item);
        }

        public static Dictionary<string, string> AsReverseMap(
            this Dictionary<string, string> dictionary) =>
            dictionary.Concat(dictionary.ToDictionary(kvp => kvp.Value, kvp => kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? equalityComparer = default)
        {
            HashSet<TKey> keys = new(equalityComparer ?? EqualityComparer<TKey>.Default);
            foreach (var element in source)
            {
                if (keys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
    }
}

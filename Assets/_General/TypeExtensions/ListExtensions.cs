using System;
using System.Collections.Generic;
using System.Threading;

namespace _General.TypeExtensions
{
    // From: https://stackoverflow.com/questions/273313/randomize-a-listt
    public static class ThreadSafeRandom
    {
        [ThreadStatic] private static Random Local;

        public static Random ThisThreadsRandom { get => Local ??= new Random(Guid.NewGuid().GetHashCode()); }
    }
    
    public static class ListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        public static T Random<T>(this IList<T> list)
        {
            if (list.Count == 0) return default;
            
            int index = ThreadSafeRandom.ThisThreadsRandom.Next(list.Count);
            return list[index];
        }
    }
}

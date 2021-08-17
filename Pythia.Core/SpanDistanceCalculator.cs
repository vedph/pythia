using System;

namespace Pythia.Core
{
    /// <summary>
    /// Spans distance calculator. The functions in this helper class are usually
    /// implemented as UDF in SQL. You might want to use this class as the
    /// source for a CLR UDF, or just as a reference for writing pure SQL UDFs.
    /// </summary>
    /// <remarks>Calculating the distance between spans is at the core of
    /// search operators like NEAR, BEFORE, AFTER, INSIDE, OVERLAPS, LALIGN,
    /// RALIGN.</remarks>
    public static class SpanDistanceCalculator
    {
        /// <summary>
        /// Gets count of items of span A overlapping with items of span B.
        /// </summary>
        /// <param name="a1">The A span start.</param>
        /// <param name="a2">The A span end.</param>
        /// <param name="b1">The B span start.</param>
        /// <param name="b2">The B span end.</param>
        /// <returns>The overlap count.</returns>
        public static int GetOverlapCount(int a1, int a2, int b1, int b2)
        {
            int min = Math.Min(a1, b1);
            int max = Math.Max(a2, b2);
            int n = 0;
            for (int i = min; i <= max; i++)
            {
                if (i >= a1 && i <= a2 && i >= b1 && i <= b2) n++;
            }
            return n;
        }

        /// <summary>
        /// Determines whether the specified A and B spans overlap.
        /// </summary>
        /// <param name="a1">The A span start.</param>
        /// <param name="a2">The A span end.</param>
        /// <param name="b1">The B span start.</param>
        /// <param name="b2">The B span end.</param>
        /// <returns><c>true</c> on overlap; otherwise, <c>false</c>.</returns>
        public static bool IsOverlap(int a1, int a2, int b1, int b2)
        {
            int min = Math.Min(a1, b1);
            int max = Math.Max(a2, b2);
            for (int i = min; i <= max; i++)
            {
                if (i >= a1 && i <= a2 && i >= b1 && i <= b2) return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether spans A and B overlap for the specified amounts.
        /// </summary>
        /// <param name="a1">The A span start.</param>
        /// <param name="a2">The A span end.</param>
        /// <param name="b1">The B span start.</param>
        /// <param name="b2">The B span end.</param>
        /// <param name="n">The min required overlap count (1-N).</param>
        /// <param name="m">The max required overlap count (1-N).</param>
        /// <returns><c>true</c> if the specified the required overlap is matched;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsOverlapWithin(int a1, int a2, int b1, int b2,
            int n, int m)
        {
            // if a before b or a after b, no overlap
            if (a2 < b1 || a1 > b2) return false;

            int d = GetOverlapCount(a1, a2, b1, b2);
            return d >= n && d <= m;
        }

        /// <summary>
        /// Determines whether span A is fully inside span B, within the
        /// specified distances.
        /// </summary>
        /// <param name="a1">The A span start.</param>
        /// <param name="a2">The A span end.</param>
        /// <param name="b1">The B span start.</param>
        /// <param name="b2">The B span end.</param>
        /// <param name="ns">The min required distance of A from B start.</param>
        /// <param name="ms">The max required distance of A from B start.</param>
        /// <param name="ne">The min required distance of A from B end.</param>
        /// <param name="me">The max required distance of A from B end.</param>
        /// <returns>
        ///   <c>true</c> if A is inside B within the specified distances;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public static bool IsInsideWithin(int a1, int a2, int b1, int b2,
            int ns, int ms, int ne, int me)
        {
            if (a1 < b1 || a2 > b2) return false;

            int ds = a1 - b1;
            int de = b2 - a2;
            return ds >= ns && ds <= ms && de >= ne && de <= me;
        }

        /// <summary>
        /// Determines whether span A is before span B within the specified
        /// distance.
        /// </summary>
        /// <param name="a1">The A span start.</param>
        /// <param name="a2">The A span end.</param>
        /// <param name="b1">The B span start.</param>
        /// <param name="b2">The B span end.</param>
        /// <param name="n">The min required distance (0-N).</param>
        /// <param name="m">The max required distance (0-N).</param>
        /// <returns><c>true</c> if A is before B within the specified distance;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsBeforeWithin(int a1, int a2, int b1, int b2,
            int n, int m)
        {
            // a is before b when a2 < b1
            if (a2 >= b1 || IsOverlap(a1, a2, b1, b2)) return false;

            int d = b1 - a2 - 1;
            return d >= n && d <= m;
        }

        /// <summary>
        /// Determines whether span A is after span B within the specified
        /// distance.
        /// </summary>
        /// <param name="a1">The A span start.</param>
        /// <param name="a2">The A span end.</param>
        /// <param name="b1">The B span start.</param>
        /// <param name="b2">The B span end.</param>
        /// <param name="n">The min required distance (0-N).</param>
        /// <param name="m">The max required distance (0-N).</param>
        /// <returns><c>true</c> if A is after B within the specified distance;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsAfterWithin(int a1, int a2, int b1, int b2,
            int n, int m)
        {
            // a is after b when a1 > b2
            if (a1 <= b2 || IsOverlap(a1, a2, b1, b2)) return false;

            int d = a1 - b2 - 1;
            return d >= n && d <= m;
        }

        /// <summary>
        /// Determines whether span A is near to span B within the specified
        /// distance.
        /// </summary>
        /// <param name="a1">The A span start.</param>
        /// <param name="a2">The A span end.</param>
        /// <param name="b1">The B span start.</param>
        /// <param name="b2">The B span end.</param>
        /// <param name="n">The min required distance (0-N).</param>
        /// <param name="m">The max required distance (0-N).</param>
        /// <returns><c>true</c> if A is near B within the specified distance;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsNearWithin(int a1, int a2, int b1, int b2,
            int n, int m)
        {
            return IsBeforeWithin(a1, a2, b1, b2, n, m)
                   || IsAfterWithin(a1, a2, b1, b2, n, m);
        }

        /// <summary>
        /// Determines whether span A is left aligned to span B within the
        /// specified distance. A must start with or after B, but not before.
        /// </summary>
        /// <param name="a1">The A span start.</param>
        /// <param name="b1">The B span start.</param>
        /// <param name="n">The min required distance (0-N).</param>
        /// <param name="m">The max required distance (0-N).</param>
        /// <returns><c>true</c> if A is left aligned with B within the
        /// specified distance; otherwise, <c>false</c>.</returns>
        public static bool IsLeftAligned(int a1, int b1, int n, int m)
        {
            return a1 - b1 >= n && a1 - b1 <= m;
        }

        /// <summary>
        /// Determines whether span A is right aligned to span B within the
        /// specified distance. A must end with or before B, but not after.
        /// </summary>
        /// <param name="a2">The A span end.</param>
        /// <param name="b2">The B span end.</param>
        /// <param name="n">The min required distance (0-N).</param>
        /// <param name="m">The max required distance (0-N).</param>
        /// <returns><c>true</c> if A is right aligned with B within the
        /// specified distance; otherwise, <c>false</c>.</returns>
        public static bool IsRightAligned(int a2, int b2, int n, int m)
        {
            return b2 - a2 >= n && b2 - a2 <= m;
        }
    }
}

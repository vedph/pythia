using Xunit;

namespace Pythia.Core.Test
{
    public sealed class SpanDistanceCalculatorTest
    {
        [Theory]
        [InlineData(1, int.MaxValue)]
        [InlineData(3, 5)]
        public void IsOverlap_ABeforeB_False(int n, int m)
        {
            Assert.False(SpanDistanceCalculator.IsOverlapWithin(1, 3, 5, 7, n, m));
        }

        [Theory]
        [InlineData(1, int.MaxValue)]
        [InlineData(3, 5)]
        public void IsOverlap_AAfterB_False(int n, int m)
        {
            Assert.False(SpanDistanceCalculator.IsOverlapWithin(5, 7, 1, 3, n, m));
        }

        [Theory]
        // overlap by 1
        [InlineData(1, 3, 3, 5, 1, int.MaxValue, true)]
        // overlap by 2
        [InlineData(1, 3, 2, 5, 1, int.MaxValue, true)]
        // overlap by 1, but min=2
        [InlineData(1, 3, 3, 5, 2, int.MaxValue, false)]
        // overlap by 2, but max=1
        [InlineData(1, 3, 2, 5, 1, 1, false)]
        public void IsOverlap_AOverlappingBStart(int a1, int a2, int b1, int b2,
            int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsOverlapWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // overlap by 1
        [InlineData(3, 5, 1, 3, 1, int.MaxValue, true)]
        // overlap by 2
        [InlineData(2, 5, 1, 3, 1, int.MaxValue, true)]
        // overlap by 1, but min=2
        [InlineData(3, 5, 1, 3, 2, int.MaxValue, false)]
        // overlap by 2, but max=1
        [InlineData(2, 5, 1, 3, 1, 1, false)]
        public void IsOverlap_AOverlappingBEnd(int a1, int a2, int b1, int b2,
            int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsOverlapWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // overlap by 2
        [InlineData(2, 3, 1, 5, 1, int.MaxValue, true)]
        // overlap by 2, but min=3
        [InlineData(2, 3, 1, 5, 3, int.MaxValue, false)]
        // overlap by 2, but max=1
        [InlineData(2, 3, 1, 5, 1, 1, false)]
        public void IsOverlap_AInsideB(int a1, int a2, int b1, int b2,
            int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsOverlapWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // overlap by 2
        [InlineData(1, 5, 2, 3, 1, int.MaxValue, true)]
        // overlap by 2, but min=3
        [InlineData(1, 5, 2, 3, 3, int.MaxValue, false)]
        // overlap by 2, but max=1
        [InlineData(1, 5, 2, 3, 1, 1, false)]
        public void IsOverlap_BInsideA(int a1, int a2, int b1, int b2,
            int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsOverlapWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // overlap by 2
        [InlineData(2, 3, 2, 3, 1, int.MaxValue, true)]
        // overlap by 2, but min=3
        [InlineData(2, 3, 2, 3, 3, int.MaxValue, false)]
        // overlap by 2, but max=1
        [InlineData(2, 3, 2, 3, 1, 1, false)]
        public void IsOverlap_AEqualsB(int a1, int a2, int b1, int b2,
            int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsOverlapWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // distance 1
        [InlineData(1, 3, 5, 8, 0, int.MaxValue, 0, int.MaxValue)]
        // distance 0
        [InlineData(1, 4, 5, 8, 0, int.MaxValue, 0, int.MaxValue)]
        // with distances (ignored)
        [InlineData(1, 3, 5, 8, 1, int.MaxValue, 0, int.MaxValue)]
        [InlineData(1, 3, 5, 8, 0, 1, 0, int.MaxValue)]
        [InlineData(1, 3, 5, 8, 0, int.MaxValue, 1, int.MaxValue)]
        [InlineData(1, 3, 5, 8, 0, int.MaxValue, 0, 1)]
        public void IsInsideWithin_Before_False(int a1, int a2, int b1, int b2,
            int ns, int ms, int ne, int me)
        {
            Assert.False(SpanDistanceCalculator.IsInsideWithin
                (a1, a2, b1, b2, ns, ms, ne, me));
        }

        [Theory]
        [InlineData(1, 5, 5, 8, 0, int.MaxValue, 0, int.MaxValue)]
        [InlineData(1, 6, 5, 8, 0, int.MaxValue, 0, int.MaxValue)]
        // with distances (ignored)
        [InlineData(1, 5, 5, 8, 1, int.MaxValue, 0, int.MaxValue)]
        [InlineData(1, 5, 5, 8, 0, 1, 0, int.MaxValue)]
        [InlineData(1, 5, 5, 8, 0, int.MaxValue, 1, int.MaxValue)]
        [InlineData(1, 5, 5, 8, 0, int.MaxValue, 0, 1)]
        public void IsInsideWithin_LeftOverlap_False(int a1, int a2, int b1, int b2,
            int ns, int ms, int ne, int me)
        {
            Assert.False(SpanDistanceCalculator.IsInsideWithin
                (a1, a2, b1, b2, ns, ms, ne, me));
        }

        [Theory]
        [InlineData(5, 7, 1, 9, 0, int.MaxValue, 0, int.MaxValue, true)]
        [InlineData(5, 7, 5, 7, 0, int.MaxValue, 0, int.MaxValue, true)]
        // NS
        [InlineData(2, 3, 1, 9, 1, int.MaxValue, 0, int.MaxValue, true)]
        [InlineData(2, 3, 1, 9, 2, int.MaxValue, 0, int.MaxValue, false)]
        // MS
        [InlineData(2, 3, 1, 9, 0, 1, 0, int.MaxValue, true)]
        [InlineData(2, 3, 1, 9, 0, 0, 0, int.MaxValue, false)]
        // NE
        [InlineData(6, 8, 1, 9, 0, int.MaxValue, 1, int.MaxValue, true)]
        [InlineData(6, 8, 1, 9, 0, int.MaxValue, 2, int.MaxValue, false)]
        // ME
        [InlineData(6, 8, 1, 9, 0, int.MaxValue, 0, 1, true)]
        [InlineData(6, 8, 1, 9, 0, int.MaxValue, 0, 0, false)]
        public void IsInsideWithin_Inside_Ok(int a1, int a2, int b1, int b2,
            int ns, int ms, int ne, int me, bool expected)
        {
            Assert.Equal(expected, SpanDistanceCalculator.IsInsideWithin
                (a1, a2, b1, b2, ns, ms, ne, me));
        }

        [Theory]
        [InlineData(3, 7, 1, 5, 0, int.MaxValue, 0, int.MaxValue)]
        [InlineData(5, 6, 1, 5, 0, int.MaxValue, 0, int.MaxValue)]
        // with distances (ignored)
        [InlineData(3, 7, 1, 5, 1, int.MaxValue, 0, int.MaxValue)]
        [InlineData(3, 7, 1, 5, 0, 1, 0, int.MaxValue)]
        [InlineData(3, 7, 1, 5, 0, int.MaxValue, 1, int.MaxValue)]
        [InlineData(3, 7, 1, 5, 0, int.MaxValue, 0, 1)]
        public void IsInsideWithin_RightOverlap_False(int a1, int a2, int b1, int b2,
            int ns, int ms, int ne, int me)
        {
            Assert.False(SpanDistanceCalculator.IsInsideWithin
                (a1, a2, b1, b2, ns, ms, ne, me));
        }

        [Theory]
        [InlineData(1, 4, 6, 7, 0, int.MaxValue, 0, int.MaxValue)]
        [InlineData(1, 4, 5, 6, 0, int.MaxValue, 0, int.MaxValue)]
        // with distances (ignored)
        [InlineData(1, 4, 6, 7, 1, int.MaxValue, 0, int.MaxValue)]
        [InlineData(1, 4, 6, 7, 0, 1, 0, int.MaxValue)]
        [InlineData(1, 4, 6, 7, 0, int.MaxValue, 1, int.MaxValue)]
        [InlineData(1, 4, 6, 7, 0, int.MaxValue, 0, 1)]
        public void IsInsideWithin_After_False(int a1, int a2, int b1, int b2,
            int ns, int ms, int ne, int me)
        {
            Assert.False(SpanDistanceCalculator.IsInsideWithin
                (a1, a2, b1, b2, ns, ms, ne, me));
        }

        [Theory]
        [InlineData(1, 9, 5, 7, 0, int.MaxValue, 0, int.MaxValue)]
        // with distances (ignored)
        [InlineData(1, 9, 5, 7, 1, int.MaxValue, 0, int.MaxValue)]
        [InlineData(1, 9, 5, 7, 0, 1, 0, int.MaxValue)]
        [InlineData(1, 9, 5, 7, 0, int.MaxValue, 1, int.MaxValue)]
        [InlineData(1, 9, 5, 7, 0, int.MaxValue, 0, 1)]
        public void IsInsideWithin_BInsideA_False(int a1, int a2, int b1, int b2,
            int ns, int ms, int ne, int me)
        {
            Assert.False(SpanDistanceCalculator.IsInsideWithin
                (a1, a2, b1, b2, ns, ms, ne, me));
        }

        [Theory]
        // overlap by 1
        [InlineData(2, 3, 3, 5, 0, int.MaxValue)]
        // overlap by 2
        [InlineData(2, 3, 1, 5, 0, int.MaxValue)]
        public void IsBeforeWithin_Overlap_False(int a1, int a2, int b1, int b2,
            int n, int m)
        {
            Assert.False(SpanDistanceCalculator.IsBeforeWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // distance 0
        [InlineData(4, 5, 2, 3, 0, int.MaxValue)]
        // distance 1
        [InlineData(4, 5, 2, 2, 0, int.MaxValue)]
        // distance 2
        [InlineData(4, 5, 1, 1, 0, int.MaxValue)]
        public void IsBeforeWithin_After_False(int a1, int a2, int b1, int b2,
            int n, int m)
        {
            Assert.False(SpanDistanceCalculator.IsBeforeWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // distance 0
        [InlineData(2, 3, 4, 5, 0, int.MaxValue, true)]
        // distance 1
        [InlineData(2, 3, 5, 6, 0, int.MaxValue, true)]
        // distance 1 but min=2
        [InlineData(2, 3, 5, 6, 2, int.MaxValue, false)]
        // distance 2 but max=1
        [InlineData(2, 3, 6, 6, 0, 1, false)]
        public void IsBeforeWithin_Before(int a1, int a2, int b1, int b2,
            int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsBeforeWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // overlap by 1
        [InlineData(2, 3, 3, 5, 0, int.MaxValue)]
        // overlap by 2
        [InlineData(2, 3, 1, 5, 0, int.MaxValue)]
        public void IsAfterWithin_Overlap_False(int a1, int a2, int b1, int b2,
            int n, int m)
        {
            Assert.False(SpanDistanceCalculator.IsAfterWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // distance 0
        [InlineData(2, 3, 4, 5, 0, int.MaxValue)]
        // distance 1
        [InlineData(2, 2, 4, 5, 0, int.MaxValue)]
        // distance 2
        [InlineData(1, 1, 4, 5, 0, int.MaxValue)]
        public void IsAfterWithin_Before_False(int a1, int a2, int b1, int b2,
            int n, int m)
        {
            Assert.False(SpanDistanceCalculator.IsAfterWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // distance 0
        [InlineData(4, 5, 2, 3, 0, int.MaxValue, true)]
        // distance 1
        [InlineData(5, 6, 2, 3, 0, int.MaxValue, true)]
        // distance 1 but min=2
        [InlineData(4, 5, 2, 3, 2, int.MaxValue, false)]
        // distance 2 but max=1
        [InlineData(6, 6, 2, 3, 0, 1, false)]
        public void IsAfterWithin_After(int a1, int a2, int b1, int b2,
            int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsAfterWithin(a1, a2, b1, b2, n, m));
        }

        [Theory]
        // distance 0
        [InlineData(3, 3, 0, int.MaxValue, true)]
        // distance +1
        [InlineData(4, 3, 0, int.MaxValue, true)]
        // distance -1
        [InlineData(3, 4, 0, int.MaxValue, false)]
        // distance 0 min 1
        [InlineData(3, 3, 1, int.MaxValue, false)]
        // distance 0 max 1
        [InlineData(3, 3, 0, 1, true)]
        // distance +1 min 1
        [InlineData(4, 3, 1, int.MaxValue, true)]
        // distance +2 min 1
        [InlineData(4, 3, 2, int.MaxValue, false)]
        // distance +1 max 1
        [InlineData(4, 3, 0, 1, true)]
        // distance +1 max 0
        [InlineData(4, 3, 0, 0, false)]
        public void IsLeftAligned_Ok(int a1, int b1, int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsLeftAligned(a1, b1, n, m));
        }

        [Theory]
        // distance 0
        [InlineData(3, 3, 0, int.MaxValue, true)]
        // distance +1
        [InlineData(4, 3, 0, int.MaxValue, false)]
        // distance -1
        [InlineData(3, 4, 0, int.MaxValue, true)]
        // distance 0 min 1
        [InlineData(3, 3, 1, int.MaxValue, false)]
        // distance 0 max 1
        [InlineData(3, 3, 0, 1, true)]
        // distance -1 min 1
        [InlineData(3, 4, 1, int.MaxValue, true)]
        // distance -1 min 2
        [InlineData(3, 4, 2, int.MaxValue, false)]
        // distance -1 max 1
        [InlineData(3, 4, 0, 1, true)]
        // distance -1 max 0
        [InlineData(3, 4, 0, 0, false)]
        public void IsRightAligned_Ok(int a2, int b2, int n, int m, bool expected)
        {
            Assert.Equal(expected,
                SpanDistanceCalculator.IsRightAligned(a2, b2, n, m));
        }
    }
}

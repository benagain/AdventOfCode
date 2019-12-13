using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace AdventOfCode
{
    public class Day3CrossedWires
    {
        private readonly CrossedWires sut = new CrossedWires();

        [Theory]
        [InlineData("R5", 5, 0)]
        [InlineData("L6", -6, 0)]
        [InlineData("U1", 0, 1)]
        [InlineData("D9", 0, -9)]
        public void FindLengthOfSegment(string segment, int x, int y)
            => sut.LengthOf(segment).Should().Be((x, y));

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("A1")]
        [InlineData("R")]
        public void FindLengthOfInvalidSegment(string segment)
            => sut.LengthOf(segment).Should().BeNull();

        public static IEnumerable<object[]> PathParts => new[]
        {
            new object[] { "R5", new[] { (5,0)} },
            new object[] { "R5,U2", new[] { (5,0), (0, 2)} },
            new object[] { "R5,U2,L3,D1", new[] { (5,0), (0, 2), (-3, 0), (0, -1) } },
        };

        [Theory]
        [MemberData(nameof(PathParts))]
        public void ParsePath(string path, (int x, int y)[] parts)
            => sut.ParsePath(path).Should().BeEquivalentTo(parts);

        public static IEnumerable<object[]> PathGraph => new[]
        {
            new object[] { "R5", new[] { (0,0), (1,0), (2,0) , (3,0) , (4,0), (5,0) } },
            new object[] { "R5,U2", new[] { (0,0), (1,0), (2,0), (3,0), (4,0), (5,0), (5,1), (5,2) } },
            new object[] { "R5,U2,L3,D1", new[] { (0,0), (1,0), (2,0), (3,0), (4,0), (5,0), (5,1), (5,2), (4,2), (3,2), (2,2), (2,1) } },
            new object[] { "R1,U1", new[] { (0,0), (1,0), (1, 1) } },
        };

        [Theory]
        [MemberData(nameof(PathGraph))]
        public void ParsePathToGraph(string path, (int, int)[] pavingSlags)
            => sut.ParsePathToGraph(path).Should().BeEquivalentTo(pavingSlags);

        [Theory]
        [InlineData(0, 5, "0,1,2,3,4,5")]
        [InlineData(5, 0, "5")]
        [InlineData(0, -5, "-5,-4,-3,-2,-1,0")]
        [InlineData(-5, 0, "-5")]
        [InlineData(2, 3, "2,3,4,5")]
        [InlineData(4, -2, "2,3,4")]
        [InlineData(-1, 3, "-1,0,1,2")]
        [InlineData(-3, -2, "-5,-4,-3")]
        public void MovementRange(int initial, int move, string range)
            => string.Join(",", sut.MovementRange(initial, move)).Should().Be(range);

        public static IEnumerable<object[]> Crosses => new[]
        {
            new object[] { "R1,U1", "U1,R1", new[] { (1,1) } },
            new object[] { "R2,U2", "U2,R2", new[] { (2,2) } },
            new object[] { "R5,U5,L5", "U6,R2,D6", new[] { (0,5), (2,5), (2,0) } },
        };


        [Theory]
        [MemberData(nameof(Crosses))]
        public void FindCrosses(string path1, string path2, (int, int)[] crosses)
            => sut.FindCrosses(path1, path2).Should().BeEquivalentTo(crosses);
        //[Theory]
        //[InlineData("R1,U1", "U1,R1", 2)]
        //[InlineData("R2,U1", "U2,R1", 3)]
        //public void Calculate(string path1, string path2, int closestCross)
        //    => sut.Calculate(path1, path2).Should().Be(closestCross);
    }

    public class CrossedWires
    {
        internal (int x, int y)? LengthOf(string segment)
        {
            var x = new Regex(@"([LRUD])(\d+)").Match(segment ?? "");
            if (!x.Success) return default;

            var length = int.Parse(x.Groups[2].Value);
            return x.Groups[1].Value switch
            {
                "L" => (-length, 0),
                "R" => (length, 0),
                "U" => (0, length),
                "D" => (0, -length),
                _ => default
            };
        }

        internal (int x, int y)[] ParsePath(string path)
        {
            var segments = path.Split(",", StringSplitOptions.RemoveEmptyEntries);
            return segments.Select(LengthOf).OfType<(int x, int y)>().ToArray();
        }

        internal (int x, int y)[] ParsePathToGraph(string path)
        {
            var segments = ParsePath(path);
            return segments.Aggregate(
                (x: 0, y: 0, steppedOn: new List<(int x, int y)>()), 
                (acc, xy) =>
            {
                acc.steppedOn.AddRange(
                    MovementRange(acc.x, xy.x, x => (x, acc.y)));
                acc.steppedOn.AddRange(
                    MovementRange(acc.y, xy.y, y => (acc.x, y)));
                return (acc.x + xy.x, acc.y + xy.y, acc.steppedOn);
            }, x => x.steppedOn.Distinct().ToArray());
        }

        private IEnumerable<(int x, int accy)> MovementRange(
            int accx, int xyx, Func<int, (int, int)> selector) 
            => MovementRange(accx, xyx).Select(selector);

        internal int[] MovementRange(int a, int b)
        {
            var (start, count) = b < 0
                ? (a + b, (b * -1) + 1)
                : (a, b + 1);

            return Enumerable.Range(start, count).ToArray();
        }

        internal (int x, int y)[] FindCrosses(string path1, string path2)
        {
            var paves1 = ParsePathToGraph(path1);
            var paves2 = ParsePathToGraph(path2);
            var intersection = paves1.Intersect(paves2).ToList();
            intersection.Remove((0, 0));
            return intersection.ToArray(); ;
        }
    }
}
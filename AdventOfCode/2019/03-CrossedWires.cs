using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using static System.Math;

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
        [InlineData(5, -10, "-5,-4,-3,-2,-1,0,1,2,3,4,5")]
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

        [Theory]
        [InlineData("R1,U1", "U1,R1", 2)]
        [InlineData("R5,U3", "U2,R7", 7)]
        [InlineData("U7,R6,D4,L4", "R8,U5,L5,D3", 6)]
        [InlineData("U5,R2,D10", "D2,R4", 4)]
        [InlineData("R75,D30,R83,U83,L12,D49,R71,U7,L72", "U62,R66,U55,R34,D71,R55,D58,R83", 159)]
        [InlineData("R98,U47,R26,D63,R33,U87,L62,D20,R33,U53,R51", "U98,R91,D20,R16,D67,R40,U7,R15,U6,R7", 135)]
        public void Calculate(string path1, string path2, int closestCross)
            => sut.Calculate(path1, path2).Should().Be(closestCross);
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
                (x: 0, y: 0, touched: new List<(int x, int y)>()),
                AddMovements,
                acc => acc.touched.Distinct().ToArray());

            (int, int, List<(int x, int)>) AddMovements(
                (int x, int y, List<(int x, int y)> touched) acc,
                (int x, int y) i)
                => (acc.x + i.x, acc.y + i.y
                   , acc.touched
                        .Union(AddMovement(acc.x, acc.y, i.x, i.y))
                        .ToList());

            IEnumerable<(int x, int accy)> AddMovement(
                int curX, int curY, int moveX, int moveY)
                => MovementRange(curX, moveX, x => (x, curY)).Union(
                   MovementRange(curY, moveY, y => (curX, y)));
        }

        private IEnumerable<(int x, int accy)> MovementRange(
            int accx, int xyx, Func<int, (int, int)> selector)
            => MovementRange(accx, xyx).Select(selector);

        internal int[] MovementRange(int start, int count)
        {
            var isReverseMovement = count < 0;

            var (start_, count_) = isReverseMovement
                ? (start + count, (count * -1))
                : (start, count);

            return Enumerable.Range(start_, count_ + 1).ToArray();
        }

        internal (int x, int y)[] FindCrosses(string path1, string path2)
        {
            var paves1 = ParsePathToGraph(path1);
            var paves2 = ParsePathToGraph(path2);
            var intersection = paves1.Intersect(paves2).ToList();
            intersection.Remove((0, 0));
            return intersection.ToArray();
        }

        internal int Calculate(string path1, string path2)
            => FindCrosses(path1, path2)
                .Select(cross => Abs(cross.x) + Abs(cross.y))
                .DefaultIfEmpty(0)
                .Min();
    }
}
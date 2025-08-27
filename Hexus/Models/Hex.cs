using System;
using System.Collections.Generic;

namespace Hexus.Models
{
    public readonly record struct Hex(int Q, int R)
    {
        public int S => -Q - R;

        public static Hex operator +(Hex a, Hex b) => new(a.Q + b.Q, a.R + b.R);
        public static Hex operator -(Hex a, Hex b) => new(a.Q - b.Q, a.R - b.R);

        private static readonly Hex[] AxialDirections =
        {
            new(1, 0), new(0, 1), new(-1, 1),
            new(-1, 0), new(0, -1), new(1, -1)
        };

        public static int Distance(Hex a, Hex b)
        {
            var vec = a - b;
            return (Math.Abs(vec.Q) + Math.Abs(vec.R) + Math.Abs(vec.S)) / 2;
        }

        public IEnumerable<Hex> GetNeighbors()
        {
            foreach (var dir in AxialDirections)
            {
                yield return this + dir;
            }
        }
    }
}

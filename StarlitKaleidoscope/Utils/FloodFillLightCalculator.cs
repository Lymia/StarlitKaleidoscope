using System;
using System.Collections.Generic;
using XRL.World;

namespace StarlitKaleidoscope.Utils {
    public static class FloodFillLightCalculator {
        [Serializable]
        public readonly struct Loc : IEquatable<Loc> {
            public Loc(int x, int y) {
                this.x = x;
                this.y = y;
            }
            public readonly int x, y;

            public bool Equals(Loc other) => x == other.x && y == other.y;
            public override bool Equals(object obj) => obj is Loc other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(x, y);
            
            public Loc Add(int x, int y) => new Loc(this.x + x, this.y + y);
        }

        static bool validLoc(Zone zone, Loc loc) {
            return loc.x >= 0 && loc.x < zone.Width && (loc.y >= 0 && loc.y < zone.Height);
        }

        const float FSqrt2 = 1.41421356237f;
        static void iter(Zone zone, ref List<Loc> illuminated, Loc source, float radius) {
            illuminated.Clear();
            
            var visited = new HashSet<Loc>();
            var toVisit = new PriorityQueue<Loc, float>();
            toVisit.Enqueue(source, -radius);

            while (toVisit.TryDequeue(out var element, out var priority)) {
                if (priority >= 0) break;
                if (visited.Add(element)) {
                    if (!validLoc(zone, element)) continue;
                    var cell = zone.Map[element.x][element.y];
                    illuminated.Add(element);

                    if (!cell.IsSolidForProjectile() || cell.IsPassable()) {
                        // cardinal
                        toVisit.Enqueue(element.Add(1, 0), priority + 1);
                        toVisit.Enqueue(element.Add(-1, 0), priority + 1);
                        toVisit.Enqueue(element.Add(0, 1), priority + 1);
                        toVisit.Enqueue(element.Add(0, -1), priority + 1);
                        // diagonal
                        toVisit.Enqueue(element.Add(1, 1), priority + FSqrt2);
                        toVisit.Enqueue(element.Add(-1, 1), priority + FSqrt2);
                        toVisit.Enqueue(element.Add(1, -1), priority + FSqrt2);
                        toVisit.Enqueue(element.Add(-1, -1), priority + FSqrt2);
                    }
                }
            }
        }

        public static void CalculateLitLocations(Zone zone, int x, int y, float radius, ref List<Loc> illuminated) {
            iter(zone, ref illuminated, new Loc(x, y), radius);
        }
    }
}
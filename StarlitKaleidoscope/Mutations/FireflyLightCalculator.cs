using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using StarlitKaleidoscope.ThirdParty;
using XRL.World;
using XRL.World.Effects;

namespace StarlitKaleidoscope.Mutations {
    public static class FireflyLightCalculator {
        [Serializable]
        public readonly struct Loc : IEquatable<Loc> {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Loc(int x, int y) {
                this.x = x;
                this.y = y;
            }
            
            public readonly int x, y;

            public bool Equals(Loc other) => x == other.x && y == other.y;
            public override bool Equals(object obj) => obj is Loc other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(x, y);
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Loc Add(int x, int y) => new Loc(this.x + x, this.y + y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool validLoc(Zone zone, Loc loc) {
            return loc.x >= 0 && loc.x < zone.Width && (loc.y >= 0 && loc.y < zone.Height);
        }

        const float CoordScale = 8.0f;
        const float TimeScale = 2.0f;
        const float BaseNoiseMagnitude = 2.5f;
        const float RadiusNoiseMagnitudeFactor = 0.15f;
        const float FSqrt2 = 1.41421356237f;

        public class CacheContext {
            internal PriorityQueue<Loc, float> toVisit = new();
            internal readonly GameObject genericFlyingEntity = GameObject.CreateSample("Creature");
            string lastSeenZoneId = null;
            internal readonly FastNoiseLite noise = new FastNoiseLite();
            
            public CacheContext() {
                genericFlyingEntity.ApplyEffect(new Flying());
                noise.SetFractalOctaves(5);
                noise.SetFractalGain(0.75f);
            }

            public void SetupNoise(Zone zone, int seed) {
                if (lastSeenZoneId != zone.ZoneID) {
                    noise.SetSeed(seed ^ zone.ZoneID.GetHashCode());
                    lastSeenZoneId = zone.ZoneID;
                }
            }
        }
        
        static void iter(
            CacheContext ctx, Zone zone, ref List<Loc> illuminated, Loc source, float radius, int updateCount
        ) {
            illuminated.Clear();
            
            var visited = new HashSet<Loc>();
            var toVisit = ctx.toVisit;
            var genericFlyingEntity = ctx.genericFlyingEntity;
            var noise = ctx.noise;
            toVisit.Clear();
            toVisit.Enqueue(source, -radius);

            var timeCoord = TimeScale * updateCount;
            var finalNoiseMagnitude = BaseNoiseMagnitude + radius * RadiusNoiseMagnitudeFactor;
            while (toVisit.TryDequeue(out var element, out var priority)) {
                if (priority >= 0) break;
                if (visited.Add(element)) {
                    var noiseValue = noise.GetNoise(CoordScale * element.x, CoordScale * element.y, timeCoord);
                    if (priority >= noiseValue * finalNoiseMagnitude) continue;
                    if (!validLoc(zone, element)) continue;
                    
                    var cell = zone.Map[element.x][element.y];
                    illuminated.Add(element);

                    if (!cell.IsSolidForProjectile() || cell.IsPassable(genericFlyingEntity)) {
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

        public static void CalculateLitLocations(
            CacheContext ctx, Zone zone, int x, int y, float radius, ref List<Loc> illuminated, int updateCount
        ) {
            iter(ctx, zone, ref illuminated, new Loc(x, y), radius, updateCount);
        }
    }
}
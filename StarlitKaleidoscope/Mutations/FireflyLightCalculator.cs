using System;
using System.Collections.Generic;
using StarlitKaleidoscope.Common;
using StarlitKaleidoscope.ThirdParty;
using XRL.World;
using XRL.World.Effects;

namespace StarlitKaleidoscope.Mutations {
    public static class FireflyLightCalculator {
        const float CoordScale = 8.0f;
        const float TimeScale = 2.0f;
        const float BaseNoiseMagnitude = 2.5f;
        const float RadiusNoiseMagnitudeFactor = 0.15f;
        const float FSqrt2 = 1.41421356237f;

        public class CacheContext {
            internal PriorityQueue<Loc, float> toVisit = new();
            internal readonly GameObject genericFlyingEntity = GameObject.CreateSample("Creature");
            string lastSeenZoneId;
            internal readonly FastNoiseLite noise = new FastNoiseLite();
            
            public CacheContext() {
                genericFlyingEntity.ApplyEffect(new Flying());
                noise.SetFractalOctaves(5);
                noise.SetFractalGain(0.75f);
            }

            public void SetupNoise(Zone zone, int seed) {
                if (lastSeenZoneId != zone.ZoneID) {
                    noise.SetSeed(HashCode.Combine(seed, zone.ZoneID.GetHashCode()));
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
            while (toVisit.TryDequeue(out var elem, out var priority)) {
                if (priority >= 0) break;
                if (visited.Add(elem)) {
                    var noiseValue = noise.GetNoise(CoordScale * elem.x, CoordScale * elem.y, timeCoord);
                    if (priority >= noiseValue * finalNoiseMagnitude) continue;
                    if (!zone.IsValidLoc(elem.x, elem.y)) continue;
                    
                    var cell = zone.Map[elem.x][elem.y];
                    illuminated.Add(elem);

                    if (!cell.IsSolidForProjectile() || cell.IsPassable(genericFlyingEntity)) {
                        // cardinal
                        toVisit.Enqueue(elem.Add(1, 0), priority + 1);
                        toVisit.Enqueue(elem.Add(-1, 0), priority + 1);
                        toVisit.Enqueue(elem.Add(0, 1), priority + 1);
                        toVisit.Enqueue(elem.Add(0, -1), priority + 1);
                        // diagonal
                        toVisit.Enqueue(elem.Add(1, 1), priority + FSqrt2);
                        toVisit.Enqueue(elem.Add(-1, 1), priority + FSqrt2);
                        toVisit.Enqueue(elem.Add(1, -1), priority + FSqrt2);
                        toVisit.Enqueue(elem.Add(-1, -1), priority + FSqrt2);
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
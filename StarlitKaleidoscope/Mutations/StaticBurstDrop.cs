using System;
using System.Collections.Generic;
using System.Text;
using XRL.Rules;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Tinkering;

namespace StarlitKaleidoscope.Mutations {
    public static class StaticBurstDrop {
        readonly static int[] BitTextures = { 0, 1, 2, 3, 4, 6, 7 };

        public static List<GameObject> CreateDrops(GameObject source) {
            var list = new List<GameObject>();
            var tier = source.GetTier();

            var dross = createDross(source, tier);
            dross.Count = source.HasPart<GivesRep>() ? Stat.Random(1, tier) : 1;
            list.Add(dross);

            return list;
        }

        static GameObject createDross(GameObject source, int tier) {
            var obj = GameObject.Create("StarlitKaleidoscope_AmalgamatedDross");
            var builder = new StringBuilder();
            obj.Blueprint += $":{source.Blueprint}:{tier}:v1";
            var seededRandom = Stat.GetSeededRandomGenerator(obj.Blueprint);

            // randomize bits
            if (!TinkerItem.BitCostMap.ContainsKey(obj.Blueprint)) {
                var bitsRandom = Stat.GetSeededRandomGenerator($"bits for {obj.Blueprint}");
                for (int i = 0; i < 1 + tier / 3 + Stat.Random(0, 1 + tier / 3); i++)
                    builder.Append('0');
                var maxTier = Math.Min(tier, 8);
                for (int i = 0; i <= maxTier; i++) {
                    var presenceChance = Math.Min(0.2f + (tier - i) * 0.075f, 1f / 3f);
                    if (maxTier == i) presenceChance += 0.3f;
                    if (bitsRandom.NextDouble() < presenceChance) {
                        builder.Append(i);
                        if (i < maxTier && bitsRandom.NextDouble() < presenceChance)
                            builder.Append(i);
                    }
                }
                var realBits = BitType.ToRealBits(builder.ToString(), obj.Blueprint);
                TinkerItem.BitCostMap.Add(obj.Blueprint, realBits);
            }

            // randomize rendering
            if (obj.TryGetPart<Render>(out var render)) {
                var color = Crayons.AllColors[seededRandom.Next(Crayons.AllColors.Length)][0];
                var detail = Crayons.AllColors[seededRandom.Next(Crayons.AllColors.Length)][0];
                while (detail == color)
                    detail = Crayons.AllColors[seededRandom.Next(Crayons.AllColors.Length)][0];
                render.ColorString = $"&{color}";
                render.DetailColor = detail.ToString();
                render.Tile = $"SLK_StaticBurstItems/bit{BitTextures[seededRandom.Next(0, BitTextures.Length)]}.png";
            }

            // randomize cost
            if (obj.TryGetPart<Commerce>(out var commerce)) {
                var cappedTier = Math.Min(tier, 8);
                var baseCost = Math.Max(cappedTier * cappedTier, 5 * Math.Max(1, cappedTier));
                var costFactor = (seededRandom.NextDouble() * 2 - 1) * 0.2;
                var cost = (int) (baseCost * costFactor);
                commerce.Value = Math.Max(1, cost);
            }

            return obj;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using StarlitKaleidoscope.Mutations;
using UnityEngine.Serialization;
using XRL.Messages;

namespace XRL.World.Parts.Mutation {
    [Serializable]
    public class StarlitKaleidoscope_BioluminescentSymbionts : BaseMutation {
        public StarlitKaleidoscope_BioluminescentSymbionts() {
            DisplayName = "Bioluminescent Symbionts";
            Type = "Physical";
        }

        public int TimeStep;
        public int NoiseSeed;
        public string LitLocationZoneId;
        public List<FireflyLightCalculator.Loc> LitLocations = new();

        [NonSerialized]
        FireflyLightCalculator.CacheContext ctx;

        public override string GetDescription() =>
            "You are bound to countless bioluminescent insects and see through their eyes.";

        public float LightRange(int Level) => Level <= 5 ? 5.0f + Level : 10.0f + (Level - 5) / 2.0f;

        public override string GetLevelText(int Level) {
            return "You light up and see into all tiles up to {{rules|" + LightRange(Level) + "}} tiles away. " +
                   "This vision can bend across corners.\n" +
                   "+400 reputation with {{w|insects}}\n";
        }

        public override IPart DeepCopy(GameObject Parent, Func<GameObject, GameObject> MapInv) {
            var newObj = base.DeepCopy(Parent, MapInv) as StarlitKaleidoscope_BioluminescentSymbionts;
            newObj.reseed();
            newObj.TimeStep = 0;
            newObj.updateLightCells(Parent.CurrentCell);
            return newObj;
        }

        void reseed() {
            NoiseSeed = new Random().Next();
        }

        void updateLightCells(Cell basisCell) {
            if (NoiseSeed == 0) reseed();
            
            if (ctx == null) ctx = new();
            ctx.SetupNoise(basisCell.ParentZone, NoiseSeed);

            var zone = basisCell.ParentZone;
            FireflyLightCalculator.CalculateLitLocations(
                ctx, zone, basisCell.X, basisCell.Y, LightRange(Level), ref LitLocations, TimeStep++
            );
            LitLocationZoneId = zone.ZoneID;
        }

        void updateExplored() {
            if (IsPlayer()) {
                var zone = TheGame.ZoneManager.GetZone(LitLocationZoneId);
                foreach (var loc in LitLocations) zone.SetExplored(loc.x, loc.y, true);
            }
        }

        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) ||
                   ID == BeforeRenderEvent.ID ||
                   ID == SingletonEvent<EndTurnEvent>.ID ||
                   ID == PooledEvent<GetItemElementsEvent>.ID ||
                   ID == EnteringZoneEvent.ID;
        }

        public override bool HandleEvent(GetItemElementsEvent E) {
            if (E.IsRelevantCreature(this.ParentObject))
                E.Add("jewels", 1);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeRenderEvent E) {
            var zone = ParentObject.CurrentZone;
            var isPlayer = IsPlayer();
            if (zone.ZoneID == LitLocationZoneId)
                foreach (var loc in LitLocations) {
                    zone.AddLight(loc.x, loc.y, 0, LightLevel.Light);
                    if (isPlayer)
                        zone.SetVisibility(loc.x, loc.y, true);
                }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EnteringZoneEvent E) {
            updateLightCells(E.Cell);
            updateExplored();
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EndTurnEvent E) {
            updateLightCells(ParentObject.CurrentCell);
            updateExplored();
            return base.HandleEvent(E);
        }
    }
}
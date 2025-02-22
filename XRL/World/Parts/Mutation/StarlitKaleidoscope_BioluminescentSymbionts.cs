using System.Collections.Generic;
using StarlitKaleidoscope.Utils;

namespace XRL.World.Parts.Mutation {
    public class StarlitKaleidoscope_BioluminescentSymbionts : BaseMutation {
        public StarlitKaleidoscope_BioluminescentSymbionts() {
            DisplayName = "Bioluminescent Symbionts";
            Type = "Physical";
        }

        string locListZoneId;
        List<FloodFillLightCalculator.Loc> locList = new();

        public override string GetDescription() => "You are bound to countless bioluminescent insects and see through their eyes.";

        public float LightRange(int Level) => Level switch {
            1 => 4.0f,
            2 => 5.0f,
            3 => 6.0f,
            4 => 7.0f,
            5 => 8.0f,
            _ => 8.0f + (Level - 5) / 2.0f,
        };
        
        public override string GetLevelText(int Level) {
            return "You light up and see into all tiles up to {{rules|" + LightRange(Level) + "}} tiles away. " +
                   "This vision can bend across corners.\n" +
                   "+400 reputation with {{w|insects}}\n";
        }

        void updateLightCells() {
            var basisCell = GetBasisCell();
            var zone = basisCell.ParentZone;
            locListZoneId = zone.ZoneID;
            FloodFillLightCalculator.CalculateLitLocations(zone, basisCell.X, basisCell.Y, LightRange(Level), ref locList);
        }
        
        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade) ||
                   ID == BeforeRenderEvent.ID || 
                   ID == SingletonEvent<EndTurnEvent>.ID ||
                   ID == PooledEvent<GetItemElementsEvent>.ID;
        }

        public override bool HandleEvent(GetItemElementsEvent E)
        {
            if (E.IsRelevantCreature(this.ParentObject))
                E.Add("jewels", 1);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeRenderEvent E) {
            var zone = GetBasisCell().ParentZone;
            var isPlayer = IsPlayer();
            if (zone.ZoneID == locListZoneId)
                foreach (var loc in locList) {
                    zone.AddLight(loc.x, loc.y, 0, LightLevel.Light);
                    if (isPlayer)
                        zone.SetVisibility(loc.x, loc.y, true);
                }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(EndTurnEvent E)
        {
            updateLightCells();
            if (IsPlayer()) {
                var zone = GetBasisCell().ParentZone;
                foreach (var loc in locList)
                    zone.SetExplored(loc.x, loc.y, true);
            }
            return base.HandleEvent(E);
        }
    }
}
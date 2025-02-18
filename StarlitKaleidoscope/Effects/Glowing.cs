using System;
using XRL.Rules;
using XRL.World;

namespace Mods.StarlitKaleidoscope.Effects {
    [Serializable]
    public class Glowing : Effect, ITierInitialized {
        public int GlowRadius;
        public int DvPenalty;

        public Glowing() {
            DisplayName = "{{Y|illuminated}}";
            GlowRadius = 2;
            DvPenalty = 1;
            Duration = 10;
        }

        public Glowing(int glowRadius, int dvPenalty, int duration) {
            GlowRadius = glowRadius;
            DvPenalty = dvPenalty;
            Duration = duration;
        }

        public void Initialize(int Tier) {
            GlowRadius = 2 + Tier / 3;
            DvPenalty = 1 + Tier / 2;
            Duration = Stat.Random(10, 30);
        } 

        public override bool UseStandardDurationCountdown() => true;
        public override string GetDetails() => "-" + DvPenalty + " DV";
        public override string GetDescription() => "{{Y|illuminated ({{C|-" + DvPenalty + " DV}})}}";
        public override string GetStateDescription() => "{{Y|illuminated}}";
        
        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) || ID == BeforeRenderEvent.ID;
        }

        public override bool HandleEvent(BeforeRenderEvent E) {
            AddLight(GlowRadius);
            return base.HandleEvent(E);
        }

        public override bool Apply(GameObject obj) {
            var existingEffect = obj.GetEffect<Glowing>();
            if (existingEffect != null) {
                if (existingEffect.DvPenalty < DvPenalty) existingEffect.DvPenalty = DvPenalty;
                if (existingEffect.GlowRadius < GlowRadius) existingEffect.GlowRadius = GlowRadius;
                if (existingEffect.Duration < Duration) existingEffect.Duration = Duration;
                return false;
            }
            
            ApplyStats();
            return true;
        }

        public override void Remove(GameObject obj) => UnapplyStats();

        void ApplyStats() => this.StatShifter.SetStatShift("DV", -this.DvPenalty);

        void UnapplyStats() => this.StatShifter.RemoveStatShifts();
    }
}

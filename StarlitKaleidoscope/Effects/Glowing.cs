using System;
using UnityEngine.Serialization;
using XRL.Rules;
using XRL.World;

namespace StarlitKaleidoscope.Effects {
    [Serializable]
    public class Glowing : Effect, ITierInitialized {
        public int GlowRadius;
        [FormerlySerializedAs("DvPenalty")]
        public int DVPenalty;

        public Glowing() {
            DisplayName = "{{Y|illuminated}}";
            GlowRadius = 2;
            DVPenalty = 1;
            Duration = 10;
        }

        public Glowing(int glowRadius, int dvPenalty, int duration) : this() {
            GlowRadius = glowRadius;
            DVPenalty = dvPenalty;
            Duration = duration;
        }

        public void Initialize(int Tier) {
            GlowRadius = 2 + Tier / 3;
            DVPenalty = 1 + Tier / 2;
            Duration = Stat.Random(10, 30);
        }

        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        public override bool UseStandardDurationCountdown() => true;
        public override string GetDetails() => "-" + DVPenalty + " DV";
        public override string GetDescription() => "{{Y|illuminated ({{C|-" + DVPenalty + " DV}})}}";
        public override string GetStateDescription() => "{{Y|illuminated}}";
        
        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) || ID == BeforeRenderEvent.ID;
        }

        public override bool HandleEvent(BeforeRenderEvent E) {
            AddLight(GlowRadius);
            return base.HandleEvent(E);
        }

        public override bool Apply(GameObject obj) {
            if (obj.TryGetEffect<Glowing>(out var existingEffect)) {
                if (existingEffect.DVPenalty < DVPenalty) existingEffect.DVPenalty = DVPenalty;
                if (existingEffect.GlowRadius < GlowRadius) existingEffect.GlowRadius = GlowRadius;
                if (existingEffect.Duration < Duration) existingEffect.Duration = Duration;
                ApplyStats();
                return false;
            }
            
            ApplyStats();
            return true;
        }

        public override void Remove(GameObject obj) => UnapplyStats();

        void ApplyStats() => this.StatShifter.SetStatShift("DV", -DVPenalty);

        void UnapplyStats() => this.StatShifter.RemoveStatShifts();
    }
}

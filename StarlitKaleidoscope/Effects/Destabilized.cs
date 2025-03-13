using System;
using ConsoleLib.Console;
using XRL.Rules;
using XRL.World;

namespace StarlitKaleidoscope.Effects {
    [Serializable]
    public class Destabilized : Effect, ITierInitialized {
        public Destabilized() {
            DisplayName = "{{Y|destabilized}}";
            Duration = 10;
        }

        public Destabilized(int duration) : this() {
            Duration = duration;
        }

        public void Initialize(int Tier) {
            Duration = Stat.Random(10, 30);
        }
        
        
        public override string GetDetails() => "Target will drop additional items on death.";

        // not marked as TYPE_NEGATIVE so it isn't removed. also cus it doesn't really debuff
        public override int GetEffectType() => TYPE_GENERAL;

        public override bool UseStandardDurationCountdown() => true;

        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) || ID == BeforeRenderEvent.ID;
        }

        public override bool FinalRender(RenderEvent E, bool bAlt) {
            E.WantsToPaint = true;
            return true;
        }

        // :D
        public override void OnPaint(ScreenBuffer buffer) {
            // TODO Implement VFX for destabilized (duration scaling color glitch field)
            base.OnPaint(buffer);
        }

        public override bool Apply(GameObject obj) {
            if (obj.TryGetEffect<Destabilized>(out var existingEffect)) {
                existingEffect.Duration += Duration;
                return false;
            }
            return true;
        }
    }
}
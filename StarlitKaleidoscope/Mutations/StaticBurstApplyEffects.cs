using System;
using System.Linq;
using XRL.Rules;
using XRL.World;
using XRL.World.Effects;

namespace StarlitKaleidoscope.Mutations {
    public class StaticBurstApplyEffects {
        readonly struct EffectKind {
            public readonly Type effectClass;
            public readonly int weight;

            public EffectKind(Type effectClass, int weight) {
                if (!typeof(Effect).IsAssignableFrom(effectClass))
                    throw new ArgumentException($"effectClass ({effectClass}) must derive from Effect");
                if (!typeof(ITierInitialized).IsAssignableFrom(effectClass))
                    throw new ArgumentException($"effectClass ({effectClass}) must derive from ITierInitialized");
                
                this.effectClass = effectClass;
                this.weight = weight;
            }
        }

        readonly static EffectKind[] effects = new[] {
            // Damage over time effects
            new EffectKind(typeof(Bleeding), 20),
            new EffectKind(typeof(AshPoison), 10),
            new EffectKind(typeof(SporeCloudPoison), 10),
            new EffectKind(typeof(PhasePoisoned), 1),
            new EffectKind(typeof(Poisoned), 20),
            new EffectKind(typeof(PoisonGasPoison), 10),
            
            // Generic numeric debuffs 
            new EffectKind(typeof(ShatterArmor), 20),
            new EffectKind(typeof(ShatterMentalArmor), 20),
            new EffectKind(typeof(Dazed), 5),
            new EffectKind(typeof(Disoriented), 5),
            new EffectKind(typeof(Hobbled), 3),
            new EffectKind(typeof(Lovesick), 5),
            new EffectKind(typeof(Shaken), 5),
            new EffectKind(typeof(Shamed), 5),
            new EffectKind(typeof(AxonsDeflated), 3),
            new EffectKind(typeof(BasiliskPoison), 5),
            
            // Speciality debuffs
            new EffectKind(typeof(CoatedInPlasma), 3),
            new EffectKind(typeof(Confused), 3),
            new EffectKind(typeof(Exhausted), 3),
            new EffectKind(typeof(Ill), 3),
            new EffectKind(typeof(Paralyzed), 3),
            new EffectKind(typeof(Prone), 3),
            new EffectKind(typeof(Stuck), 3),
            new EffectKind(typeof(Stun), 3),
        };

        readonly static Tuple<EffectKind, int>[] computedEffects;

        static StaticBurstApplyEffects() {
            computedEffects = new Tuple<EffectKind, int>[effects.Select(x => x.weight).Sum()];
            var i = 0;
            foreach (var effect in effects.Select((x, i) => new Tuple<EffectKind, int>(x, i)))
                for (var j = 0; j < effect.Item1.weight; j++)
                    computedEffects[i++] = effect;
        }

        public static void ApplyEffects(GameObject source, GameObject target, int count, int tier) {
            var effectGenerated = new bool[effects.Length]; 
            if (count > 10) count = 10;
            if (count <= 0) return;
            while (count > 0) {
                var effect = computedEffects[Stat.Rnd.Next(0, computedEffects.Length)];
                if (!effectGenerated[effect.Item2]) {
                    var effectObj = (Effect) Activator.CreateInstance(effect.Item1.effectClass);
                    ((ITierInitialized) effectObj).Initialize(tier);
                    target.ApplyEffect(effectObj, source);
                    effectGenerated[effect.Item2] = true;
                    count--;
                }
            }
        }
    }
}
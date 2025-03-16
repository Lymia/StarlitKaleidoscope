using System;
using StarlitKaleidoscope.Common;
using StarlitKaleidoscope.Parts.Effects;
using StarlitKaleidoscope.Parts.Mutations;
using XRL.Rules;
using XRL.World;
using XRL.World.Effects;

namespace StarlitKaleidoscope.Mutations {
    public static class StaticBurstApplyEffects {
        interface IEffectFactory : IWeight {
            Effect createEffect(int tier);
        }

        class EffectType<T> : IEffectFactory where T : Effect, ITierInitialized, new() {
            readonly Action<T, int> callback;
            public int Weight { get; }

            public EffectType(int weight, Action<T, int> callback = null) {
                Weight = weight;
                this.callback = callback;
            }

            public Effect createEffect(int tier) {
                var effectObject = new T();
                effectObject.Initialize(tier);
                callback?.Invoke(effectObject, tier);
                return effectObject;
            }
        }

        class CallbackEffect : IEffectFactory {
            readonly Func<int, Effect> callback;
            public int Weight { get; }

            public CallbackEffect(Func<int, Effect> callback, int weight) {
                this.callback = callback;
                Weight = weight;
            }

            public Effect createEffect(int tier) {
                return callback(tier);
            }
        }

        static string strDamage(int tier) {
            return $"{tier}d2";
        }

        static int intDamage(int tier) {
            return tier * 3 / 2 + Stat.Random(0, 1);
        }

        static int avPvPenalty(int tier) {
            return Stat.Random(0, 2) + tier;
        }

        static int durationCap(int tier) {
            tier -= 1;
            return Stat.Random(15 + tier * 5, 25 + tier * 10);
        }

        readonly static WeighedSelection<IEffectFactory> effects = new(new IEffectFactory[] {
            // Damage over time effects
            new EffectType<Bleeding>(20, (eff, tier) => eff.Damage = strDamage(tier)),
            new EffectType<AshPoison>(10, (eff, tier) => eff.Damage = intDamage(tier)),
            new EffectType<SporeCloudPoison>(10, (eff, tier) => eff.Damage = intDamage(tier)),
            new EffectType<PhasePoisoned>(1, (eff, tier) => eff.DamageIncrement = strDamage(tier)),
            new EffectType<Poisoned>(20, (eff, tier) => eff.DamageIncrement = strDamage(tier)),
            new EffectType<PoisonGasPoison>(10, (eff, tier) => eff.Damage = intDamage(tier)),

            // Generic numeric debuffs 
            new EffectType<ShatterArmor>(20, (eff, tier) => eff.AVPenalty = avPvPenalty(tier)),
            new EffectType<ShatterMentalArmor>(20, (eff, tier) => eff.MAPenalty = avPvPenalty(tier)),
            new EffectType<Glowing>(20, (eff, tier) => eff.DVPenalty = avPvPenalty(tier)),
            new EffectType<Dazed>(5),
            new EffectType<Disoriented>(5),
            new EffectType<Hobbled>(3),
            new EffectType<Lovesick>(5),
            new EffectType<Shaken>(5),
            new EffectType<Shamed>(5),
            new EffectType<AxonsDeflated>(3),
            new EffectType<BasiliskPoison>(5),
            new CallbackEffect(_ => new Interdicted(typeof(StaticBurst).FullName, 10), 5),

            // Speciality debuffs
            new EffectType<CoatedInPlasma>(3),
            new EffectType<Confused>(3),
            new EffectType<Exhausted>(3),
            new EffectType<Ill>(3),
            new EffectType<Paralyzed>(3),
            new EffectType<Prone>(3),
            new EffectType<Stuck>(3),
            new EffectType<Stun>(3),
        });

        public static void ApplyEffects(GameObject source, GameObject target, int count, int tier) {
            var genEffects = effects.select(count);
            foreach (var effect in genEffects) {
                var effectObject = effect.createEffect(tier);
                var newDuration = durationCap(tier);
                if (newDuration < effectObject.Duration)
                    effectObject.Duration = newDuration;
                target.ApplyEffect(effectObject, source);
            }
        }
    }
}
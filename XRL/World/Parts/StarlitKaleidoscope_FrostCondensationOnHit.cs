using System;
using UnityEngine.Serialization;

namespace XRL.World.Parts {
    [Serializable]
    public class StarlitKaleidoscope_FrostCondensationOnHit : IPart {
        [FormerlySerializedAs("Amount")]
        public string TemperatureChange = null;

        public string IceDamage = null;

        public StarlitKaleidoscope_FrostCondensationOnHit() { }

        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) || ID == PooledEvent<BeforeMeleeAttackEvent>.ID;
        }

        public override bool HandleEvent(BeforeMeleeAttackEvent E) {
            if (E.Weapon == this.ParentObject) {
                PlayWorldSound("Sounds/Enhancements/sfx_enhancement_cold", Combat: true);
            }
            return base.HandleEvent(E);
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar) {
            Registrar.Register("ProjectileHit");
            Registrar.Register("WeaponDealDamage");
            Registrar.Register("WeaponHit");
            base.Register(Object, Registrar);
        }

        int GetFinalTemperature(Physics physics, int temperature, int amount) {
            if (temperature > physics.FreezeTemperature) {
                var relTemp = temperature - physics.FreezeTemperature;
                
                // linear above the freeze temperature
                if (relTemp >= amount) return temperature - amount;
                
                // otherwise, recursively call GetFinalTemperature at freezing point
                return GetFinalTemperature(physics, physics.FreezeTemperature, amount - relTemp);
            } else if (temperature <= physics.BrittleTemperature) {
                // we don't mess with temperature that are alrady frozen
                return temperature;
            } else {
                // we apply diminishing returns to attempts 
                var freezeDifference = physics.FreezeTemperature - physics.BrittleTemperature;
                var frozenRatio = (float) (temperature - physics.BrittleTemperature) / freezeDifference;
                var finalFrozenProgress = 1.0f / frozenRatio + (float) amount / freezeDifference;
                var finalFrozenRatio = 1.0f / finalFrozenProgress;
                return (int) Math.Round(finalFrozenRatio * freezeDifference + physics.BrittleTemperature);
            }
        }

        public override bool FireEvent(Event E) {
            if (TemperatureChange != null && (E.ID == "WeaponHit" || E.ID == "WeaponDealDamage" || E.ID == "ProjectileHit")) {
                var source = E.GetGameObjectParameter("Attacker");
                var target = E.GetGameObjectParameter("Defender");
                if (target != null) {
                    // apply temperature change
                    var rawAmount = TemperatureChange.RollCached();
                    if (rawAmount >= 0) return base.FireEvent(E);
                    var currentTemperature = target.Physics.Temperature;
                    var amount = GetFinalTemperature(target.Physics, currentTemperature, -rawAmount) - currentTemperature;
                    target.TemperatureChange(amount, E.GetGameObjectParameter("Attacker"), Phase: ParentObject.GetPhase());
                    
                    // apply bonus frost damage
                    if (IceDamage != null && target.PhaseMatches(ParentObject))
                        target.TakeDamage(IceDamage.RollCached(), "from %t frost!", "Cold", Attacker: source);
                }
            }
            return base.FireEvent(E);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using XRL.World;
using XRL.World.Parts;

namespace StarlitKaleidoscope.Patches.MutationSystems {
    [HarmonyPatch(typeof(MagazineAmmoLoader))]
    public static class OverrideWeaponProjectileHandler {
        static IEnumerable<MethodBase> TargetMethods() {
            var result = new List<MethodBase>();
            result.Add(AccessTools.Method(typeof(MagazineAmmoLoader), "HandleEvent", new[] { typeof(LoadAmmoEvent) }));
            result.Add(AccessTools.Method(typeof(MagazineAmmoLoader), "HandleEvent", new[] { typeof(GetDisplayNameEvent) }));
            return result;
        }

        [ThreadStatic]
        static bool overrideAmmo;

        static void Prefix(MagazineAmmoLoader __instance) {
            overrideAmmo = __instance.Ammo != null && __instance.Ammo.HasPart<StarlitKaleidoscope_OverrideWeaponProjectile>();
        }

        public static string GetProjectileObject(MagazineAmmoLoader loader) {
            if (overrideAmmo) return null;
            return loader.ProjectileObject;
        }

        public static GameObject GetProjectileFor(GameObject Ammo, GameObject Launcher) {
            GameObject target = GetProjectileObjectEvent.GetFor(Ammo, Launcher);
            var overrideProjectile = Ammo.GetPart<StarlitKaleidoscope_OverrideWeaponProjectile>();
            if (overrideProjectile != null) {
                var targetProjectile = target.GetPart<Projectile>();
                if (targetProjectile != null) {
                    if (overrideProjectile.OverrideStats) {
                        targetProjectile.BasePenetration = overrideProjectile.BasePenetration;
                        targetProjectile.BaseDamage = overrideProjectile.BaseDamage;
                    }
                    if (!overrideProjectile.Attributes.IsNullOrEmpty()) {
                        targetProjectile.Attributes = targetProjectile.Attributes.IsNullOrEmpty()
                            ? overrideProjectile.Attributes
                            : targetProjectile.Attributes + "," + overrideProjectile.Attributes;
                    }
                }

                var targetFrostCondensation = target.GetPart<StarlitKaleidoscope_FrostCondensationOnHit>();
                if (targetFrostCondensation != null)
                    if (overrideProjectile.FrostCondensationTemperatureChange != null)
                        targetFrostCondensation.Amount = overrideProjectile.FrostCondensationTemperatureChange;
            }
            return target;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            // Override ProjectileObject for weapons with OverrideWeaponProjectile.
            var codeMatcher = new CodeMatcher(instructions, generator);
            codeMatcher
                .Start()
                .MatchStartForward(
                    new CodeMatch(
                        OpCodes.Ldfld,
                        AccessTools.Field(typeof(MagazineAmmoLoader), "ProjectileObject")
                    )
                )
                .ThrowIfInvalid("Could not find accesses to MagazineAmmoLoader.ProjectileObject")
                .Repeat(cm => {
                    cm.RemoveInstruction();
                    cm.InsertAndAdvance(CodeInstruction.Call(() => GetProjectileObject(default)));
                });
            instructions = codeMatcher.Instructions();
            
            // Replace the GetProjectileObjectEvent.GetFor calls to allow for editing the stats
            var meth_from = AccessTools.Method(typeof(GetProjectileObjectEvent), "GetFor");
            var meth_to = AccessTools.Method(typeof(OverrideWeaponProjectileHandler), "GetProjectileFor");
            instructions = Transpilers.MethodReplacer(instructions, meth_from, meth_to);

            return instructions;
        }
    }
}
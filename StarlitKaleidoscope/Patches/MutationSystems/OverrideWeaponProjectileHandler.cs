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
                        var originalPen = targetProjectile.BasePenetration;
                        targetProjectile.BasePenetration = overrideProjectile.BasePenetration;
                        var originalDmg = targetProjectile.BaseDamage;
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
            var codeMatcher = new CodeMatcher(instructions, generator);

            // Override ProjectileObject for weapons with OverrideWeaponProjectile.
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
            
            // Replace the GetProjectileObjectEvent.GetFor calls to allow for editing the stats
            codeMatcher
                .Start()
                .MatchStartForward(
                    new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(GetProjectileObjectEvent), "GetFor"))
                )
                .ThrowIfInvalid("Could not find accesses to GetProjectileObjectEvent.GetFor")
                .Repeat(cm => {
                    cm.RemoveInstruction();
                    cm.InsertAndAdvance(CodeInstruction.Call(() => GetProjectileFor(default, default)));
                });

            return codeMatcher.Instructions();
        }
    }
}
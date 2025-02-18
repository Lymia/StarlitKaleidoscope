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

        public static string GetProjectileObject(MagazineAmmoLoader loader) {
            if (loader.Ammo != null && loader.Ammo.HasPart<StarlitKaleidoscope_OverrideWeaponProjectile>()) return null;
            return loader.ProjectileObject;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
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

            return codeMatcher.Instructions();
        }
    }
}
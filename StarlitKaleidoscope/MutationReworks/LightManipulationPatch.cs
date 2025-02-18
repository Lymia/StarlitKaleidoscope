using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Mods.StarlitKaleidoscope.Effects;
using XRL;
using XRL.World;
using XRL.World.Parts.Mutation;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
namespace Mods.StarlitKaleidoscope.MutationReworks {
    public static class LightManipulationPatch {
        public static int GetMaxLightRadius(int Level) =>
            4 + Level / 2 + GetMinLightRadius(Level);

        public static int GetMinLightRadius(int Level) =>
            2 + (Level - 1) / 4;

        public static int GetDvPenalty(int Level) =>
            1 + (Level + 1) / 4;

        public static int GetGlowDuration(int Level) =>
            9 + Level;

        public static int MaxUsedCharges(LightManipulation self) =>
            GetMaxLightRadius(self.Level) - GetMinLightRadius(self.Level);

        public static void ApplyGlowingEffect(LightManipulation self, GameObject target) {
            target.ApplyEffect(new Glowing(
                GetMinLightRadius(self.Level), GetDvPenalty(self.Level), GetGlowDuration(self.Level)
            ));
        }

        public static String NewDescription(LightManipulation self, int Level) {
            return "You produce ambient light within a radius of {{rules|" + self.GetMaxLightRadius(Level) + "}}.\n" +
                   "You may focus the light into a laser beam, temporarily reducing the radius of your ambient light by 1, " +
                   "to a minimum of {{rules|" + GetMinLightRadius(Level) + "}}.\n" +
                   "Targets you hit glow for a short time, causing them to suffer a DV penalty.\n" +
                   "\n" +
                   "Laser damage increment: {{rules|" + self.GetDamage(Level) + "}}\n" +
                   "Laser penetration: {{rules|" + (self.GetLasePenetrationBonus(Level) + 4) + "}}\n" +
                   "Illuminated light radius: {{rules|" + (GetMinLightRadius(Level) + 4) + "}}\n" +
                   "Illuminated DV penalty: {{rules|" + (GetDvPenalty(Level) + 4) + "}}\n" +
                   "Illuminated duration: {{rules|" + (GetGlowDuration(Level) + 4) + "}}\n" +
                   "\n" +
                   "Ambient light recharges at a rate of 1 unit every " + self.GetRadiusRegrowthTurns() + 
                   " rounds until it reaches its maximum value.\n" + 
                   "{{rules|" + self.GetReflectChance(Level).ToString() + "%}} chance to reflect light-based damage";
        }
    }

    // low level functions
    public static class LightManipulationPatchLL {
        [ThreadStatic]
        static GameObject laseTarget;

        public static GameObject WithLaseTarget(GameObject target) {
            laseTarget = target;
            return target;
        }

        public static void ApplyGlowingEffect(LightManipulation self) {
            LightManipulationPatch.ApplyGlowingEffect(self, laseTarget);
        }
        
        internal static void FinalizeCall() {
            laseTarget = null;
        }
    }
    
    // Patch detailed description
    [HarmonyPatch(typeof(LightManipulation), nameof(LightManipulation.GetLevelText))]
    internal static class LightManipulationPatchDescription {
        static bool Prefix(out String __result, LightManipulation __instance, int Level) {
            __result = LightManipulationPatch.NewDescription(__instance, Level);
            return false;
        }
    }
    
    // Patch max radius
    [HarmonyPatch(typeof(LightManipulation), nameof(LightManipulation.GetMaxLightRadius))]
    internal static class LightManipulationPatchRadius {
        static bool Prefix(out int __result, int Level) {
            __result = LightManipulationPatch.GetMaxLightRadius(Level);
            return false;
        }
    }
    
    // Patch minimum radius
    [HarmonyPatch(typeof(LightManipulation), nameof(LightManipulation.HandleEvent), typeof(CommandEvent))]
    internal static class LightManipulationPatchHandleEventCommandEvent {
        internal static void PatchLightRadius(CodeMatcher codeMatcher) {
            // replace the call to get_MaxLightRadius with a call to maxUsedCharges
            codeMatcher
                .MatchStartForward(
                    new CodeMatch(
                        OpCodes.Call,
                        AccessTools.DeclaredPropertyGetter(typeof(LightManipulation), "MaxLightRadius")
                    )
                )
                .ThrowIfInvalid("Could not find call to LightManipulation.get_MaxLightRadius")
                .Repeat(
                    matchAction: cm => {
                        cm.RemoveInstruction();
                        cm.InsertAndAdvance(
                            CodeInstruction.Call(() => LightManipulationPatch.MaxUsedCharges(default))
                        );
                    }
                );
        }
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);
            PatchLightRadius(codeMatcher);
            return codeMatcher.Instructions();
        }
    }
    
    // Patch max charges display
    [HarmonyPatch(typeof(LightManipulation), nameof(LightManipulation.SyncAbilityName))]
    internal static class LightManipulationPatchSyncAbilityName {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);
            LightManipulationPatchHandleEventCommandEvent.PatchLightRadius(codeMatcher);
            return codeMatcher.Instructions();
        }
    }
    
    // Patch lase glowing effect
    [HarmonyPatch(typeof(LightManipulation), nameof(LightManipulation.Lase))]
    internal static class LightManipulationPatchLase {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);
            
            // store the lase target
            codeMatcher
                .MatchStartForward(
                    new CodeMatch(
                        OpCodes.Callvirt, 
                        AccessTools.Method(typeof(Cell), nameof(Cell.GetCombatTarget))
                    )
                )
                .ThrowIfInvalid("Could not find call to Cell.GetCombatTarget")
                .Advance(1)
                .InsertAndAdvance(CodeInstruction.Call(() => LightManipulationPatchLL.WithLaseTarget(default)));
            
            // inject the glowing effect call
            codeMatcher
                .MatchStartForward(
                    new CodeMatch(() => default(GameObject).TakeDamage(default, default, default))
                )
                .ThrowIfInvalid("Could not find call to GameObject.TakeDamage")
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0)) // this
                .InsertAndAdvance(
                    CodeInstruction.Call(() => LightManipulationPatchLL.ApplyGlowingEffect(default))
                );
        
            return codeMatcher.Instructions();
        }

        static void Finalizer() {
            LightManipulationPatchLL.FinalizeCall();
        }
    }
    
    // Fix lase text on level up
    [HarmonyPatch(typeof(LightManipulation), nameof(LightManipulation.ChangeLevel))]
    internal static class LightManipulationPatchChangeLevel {
        static void Postfix(LightManipulation __instance) {
            __instance.SyncAbilityName();
        }
    }
    
    // Expose additional fields to the ability text
    [HarmonyPatch(typeof(LightManipulation), nameof(LightManipulation.CollectStats))]
    internal static class LightManipulationPatchCollectStats {
        static void Postfix(LightManipulation __instance, Templates.StatCollector stats, int Level) {
            stats.Set("MinRadius", LightManipulationPatch.GetMinLightRadius(Level), !stats.mode.Contains("ability"));
            stats.Set("DvPenalty", LightManipulationPatch.GetDvPenalty(Level), !stats.mode.Contains("ability"));
            stats.Set("GlowDuration", LightManipulationPatch.GetGlowDuration(Level), !stats.mode.Contains("ability"));
            stats.Set("GlowRadius", LightManipulationPatch.GetMinLightRadius(Level), !stats.mode.Contains("ability"));
        }
    }
}
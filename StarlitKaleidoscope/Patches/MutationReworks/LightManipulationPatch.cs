using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StarlitKaleidoscope.Parts.Effects;
using XRL;
using XRL.World;
using XRL.World.Parts.Mutation;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
namespace StarlitKaleidoscope.Patches.MutationReworks {
    public static class LightManipulationPatch {
        public static int GetMaxLightRadius(int Level) =>
            4 + Level / 2 + GetMinLightRadius(Level);

        public static int GetMinLightRadius(int Level) =>
            2 + (Level - 1) / 4;

        public static int GetDvPenalty(int Level) =>
            1 + (Level + 1) / 4;

        public static int GetGlowLightRadius(int Level) =>
            GetMinLightRadius(Level);
        
        public static int GetGlowDuration(int Level) =>
            9 + Level;

        public static int MaxLaseCharges(LightManipulation self) =>
            GetMaxLightRadius(self.Level) - GetMinLightRadius(self.Level);

        public static void ApplyGlowingEffect(LightManipulation self, GameObject target) {
            target.ApplyEffect(new Glowing(
                GetGlowLightRadius(self.Level), GetDvPenalty(self.Level), GetGlowDuration(self.Level)
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
                   "Illuminated light radius: {{rules|" + (GetGlowLightRadius(Level) + 4) + "}}\n" +
                   "Illuminated DV penalty: {{rules|" + (GetDvPenalty(Level) + 4) + "}}\n" +
                   "Illuminated duration: {{rules|" + (GetGlowDuration(Level) + 4) + "}}\n" +
                   "\n" +
                   "Ambient light recharges at a rate of 1 unit every " + self.GetRadiusRegrowthTurns() + 
                   " rounds until it reaches its maximum value.\n" + 
                   "{{rules|" + self.GetReflectChance(Level) + "%}} chance to reflect light-based damage";
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
    
    // Patch minimum light radius
    [HarmonyPatch(typeof(LightManipulation))]
    internal static class LightManipulationPatchMinLightRadius {
        static IEnumerable<MethodBase> TargetMethods() {
            var result = new List<MethodBase>();
            result.Add(AccessTools.Method(typeof(LightManipulation), "HandleEvent", new[] { typeof(CommandEvent) }));
            result.Add(AccessTools.Method(typeof(LightManipulation), "SyncAbilityName"));
            return result;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            // replace the call to get_MaxLightRadius with a call to MaxLaseCharges
            var func_getMaxLightRadius = AccessTools.DeclaredPropertyGetter(typeof(LightManipulation), "MaxLightRadius");
            var func_maxLaseCharges =
                AccessTools.Method(typeof(LightManipulationPatch), nameof(LightManipulationPatch.MaxLaseCharges));
            return instructions.MethodReplacer(func_getMaxLightRadius, func_maxLaseCharges);
        }
    }
    
    // Patch lase glowing effect
    [HarmonyPatch(typeof(LightManipulation), nameof(LightManipulation.Lase))]
    internal static class LightManipulationPatchLase {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);
            
            // store the lase target
            var storeLocation = codeMatcher
                .Start()
                .MatchStartForward(
                    new CodeMatch(
                        OpCodes.Callvirt,
                        AccessTools.Method(typeof(Cell), nameof(Cell.GetCombatTarget))
                    ),
                    new CodeMatch(instr => instr.IsStloc())
                )
                .ThrowIfInvalid("Could not find call to Cell.GetCombatTarget + store sequence")
                .Advance(1) // advance to the stloc instruction
                .Instruction;
            var loadInstruction = PatchUtils.StlocToLdloc(storeLocation);
            
            // inject the glowing effect call
            codeMatcher
                .Start()
                .MatchEndForward(
                    new CodeMatch(instr =>
                        // It's too annoying to get the right version of TakeDamage
                        instr.opcode == OpCodes.Callvirt && instr.operand is MethodInfo info && 
                        info.DeclaringType == typeof(GameObject) && info.Name == "TakeDamage"
                    )
                )
                .ThrowIfInvalid("Could not find call to GameObject.TakeDamage")
                .Advance(1)
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0), // this
                    loadInstruction,
                    CodeInstruction.Call(() => LightManipulationPatch.ApplyGlowingEffect(default, default))
                );
        
            return codeMatcher.Instructions();
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
            stats.Set("GlowRadius", LightManipulationPatch.GetGlowLightRadius(Level), !stats.mode.Contains("ability"));
        }
    }
}
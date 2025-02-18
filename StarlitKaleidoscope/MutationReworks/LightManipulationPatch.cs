using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Mods.StarlitKaleidoscope.Effects;
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
}
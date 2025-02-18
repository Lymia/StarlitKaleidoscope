using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using XRL.World;
using XRL.World.Parts;

namespace StarlitKaleidoscope.Patches.MutationSystems {
    [HarmonyPatch(typeof(MagazineAmmoLoader), nameof(MagazineAmmoLoader.HandleEvent), typeof(InventoryActionEvent))]
    public static class TransientAmmoHandler {
        public static bool CheckIsTransientAmmo(MagazineAmmoLoader loader, InventoryActionEvent E) {
            if (loader.Ammo != null && loader.Ammo.HasPart<StarlitKaleidoscope_TransientAmmo>()) {
                if (E.Actor.IsPlayer())
                    IComponent<GameObject>.EmitMessage(
                        E.Actor, 
                        "The " + loader.Ammo.Does("melt") + " away as you unload " + 
                        loader.Ammo.GetPronounProvider().Objective + "."
                    );
                loader.SetAmmo(null);
                return true;
            }
            return false;
        }
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var codeMatcher = new CodeMatcher(instructions, generator);
            
            // Insert a "return true" branch at the end of the code.
            var returnTrueLabel = codeMatcher.End().Pos;
            codeMatcher
                .End()
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1)) // true
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ret));
            
            // Insert our check after the code to play sounds.
            codeMatcher
                .MatchStartForward(
                    new CodeMatch(() => default(GameObject).PlayWorldSoundTag(default, default, default, default)
                ))
                .ThrowIfInvalid("Could not find call to GameObject.PlayWorldSoundTag")
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0)) // this
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1)) // E
                .InsertAndAdvance(
                    CodeInstruction.Call(() => CheckIsTransientAmmo(default, default))
                )
                .InsertBranchAndAdvance(OpCodes.Brtrue, returnTrueLabel);
        
            return codeMatcher.Instructions();
        }
    }
}
using System.Collections.Generic;
using System.Linq;
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
                        loader.Ammo.Does("melt") + " away as you unload " + loader.Ammo.GetPronounProvider().Objective + "."
                    );
                loader.SetAmmo(null);
                return true;
            }
            return false;
        }
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var codeMatcher = new CodeMatcher(instructions, generator);
            
            var label = new Label();
            codeMatcher
                .Start()
                .MatchStartForward(
                    new CodeMatch(
                        OpCodes.Callvirt,
                        AccessTools.Method(typeof(GameObject), "PlayWorldSoundTag")
                    )
                )
                .ThrowIfInvalid("Could not find call to GameObject.PlayWorldSoundTag")
                .Advance(1)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0)) // this
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_1)) // E
                .InsertAndAdvance(
                    CodeInstruction.Call(() => CheckIsTransientAmmo(default, default))
                )
                .InsertAndAdvance(new CodeInstruction(OpCodes.Brfalse, label))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_1))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ret))
                .AddLabels(new[] { label });
        
            return codeMatcher.Instructions();
        }
    }
}
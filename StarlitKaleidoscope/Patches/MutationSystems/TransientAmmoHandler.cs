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
            if (loader.Ammo != null && loader.Ammo.GetIntProperty("SLK:TransientAmmo") == 1) {
                if (E.Actor.IsPlayer())
                    E.Actor.EmitMessage(
                        loader.Ammo.Does("melt") + " away as you unload " + loader.Ammo.them + "."
                    );
                loader.SetAmmo(null);
                return true;
            }
            return false;
        }
        
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var codeMatcher = new CodeMatcher(instructions, generator);
            
            var label = generator.DefineLabel();
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
                .InsertAndAdvance(
                    new CodeInstruction(OpCodes.Ldarg_0), // this
                    new CodeInstruction(OpCodes.Ldarg_1), // E
                    CodeInstruction.Call(() => CheckIsTransientAmmo(default, default)),
                    new CodeInstruction(OpCodes.Brfalse, label),
                    new CodeInstruction(OpCodes.Ldc_I4_1),
                    new CodeInstruction(OpCodes.Ret)
                )
                .AddLabels(new[] { label });
        
            return codeMatcher.Instructions();
        }
    }
}
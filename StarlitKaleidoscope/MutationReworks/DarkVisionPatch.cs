using System.Reflection;
using HarmonyLib;
using XRL.World.Parts.Mutation;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
namespace StarlitKaleidoscope.MutationReworks {
    [HarmonyPatch(typeof(DarkVision))]
    internal static class DarkVisionPatchRadius {
        static MethodBase TargetMethod() {
            return AccessTools.Constructor(typeof(DarkVision));
        }
        static void Postfix(DarkVision __instance) {
            __instance.Radius = 8;
        }
    }
}

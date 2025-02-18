using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mods.StarlitKaleidoscope.Effects;
using XRL;
using XRL.World;
using XRL.World.Parts.Mutation;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local
namespace Mods.StarlitKaleidoscope.MutationReworks {
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

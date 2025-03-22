using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using XRL;

namespace StarlitKaleidoscope.Patches {
    public static class XRLRedirect {
        public static Type GetTypeOverride(String name, bool throwOnError, bool ignoreCase) {
            Type type = typeof(ModManager).Assembly.GetType(name, throwOnError, ignoreCase);
            if (type == null)
                type = Type.GetType(name, throwOnError, ignoreCase);
            return type;
        }
    }

    [HarmonyPatch(typeof(ModManager), "ResolveType", typeof(string), typeof(string), typeof(bool), typeof(bool),
        typeof(bool))]
    internal static class XRLRedirectResolve {
        static bool Prefix(ref string Namespace, ref string TypeID) {
            var fullType = (Namespace == null ? TypeID : $"{Namespace}.{TypeID}");
            var splitName = fullType.Split('.');

            var ns = String.Join('.', splitName.Take(splitName.Length - 1));
            var type = splitName.Last();

            if (!type.StartsWith("SLKS:")) return true;
            var realType = type.Split(":").Last();

            if (ns == "XRL.World.Parts.Mutation") {
                Namespace = null;
                TypeID = $"StarlitKaleidoscope.Parts.Mutations.{realType}";
            } else if (ns == "XRL.World.Parts") {
                Namespace = null;
                TypeID = $"StarlitKaleidoscope.Parts.Generic.{realType}";
            } else {
                throw new Exception($"Cannot redirect namespace: {ns}");
            }

            return true;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
            var func_GetType =
                AccessTools.Method(typeof(Type), nameof(Type.GetType), new[] { typeof(string), typeof(bool), typeof(bool) });
            var func_GetTypeOverride =
                AccessTools.Method(typeof(XRLRedirect), nameof(XRLRedirect.GetTypeOverride));
            return instructions.MethodReplacer(func_GetType, func_GetTypeOverride);
        }
    }

    [HarmonyPatch(typeof(ModManager), "ResolveTypeName")]
    internal static class XRLRedirectName {
        static bool Prefix(Type T, ref string __result) {
            if (T.Assembly == typeof(XRLRedirectName).Assembly) {
                if (T.Namespace!.StartsWith("StarlitKaleidoscope.Parts.")) {
                    __result = $"SLKS:{T.Name}";
                    return false;
                }
            }
            return true;
        }
    }
}
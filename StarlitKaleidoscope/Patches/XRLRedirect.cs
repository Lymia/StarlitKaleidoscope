using System;
using System.Linq;
using HarmonyLib;
using XRL;

namespace StarlitKaleidoscope.Patches {
    [HarmonyPatch(typeof(ModManager), "ResolveType", typeof(string), typeof(string), typeof(bool), typeof(bool),
        typeof(bool))]
    public class XRLRedirectResolve {
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
    }

    [HarmonyPatch(typeof(ModManager), "ResolveTypeName")]
    public class XRLRedirectName {
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
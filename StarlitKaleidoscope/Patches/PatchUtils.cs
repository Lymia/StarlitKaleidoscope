using System;
using System.Reflection.Emit;
using HarmonyLib;

namespace StarlitKaleidoscope.Patches {
    public static class PatchUtils {
        public static CodeInstruction StlocToLdloc(CodeInstruction instr) {
            if (instr.opcode == OpCodes.Stloc_0) return new CodeInstruction(OpCodes.Ldloc_0);
            if (instr.opcode == OpCodes.Stloc_1) return new CodeInstruction(OpCodes.Ldloc_1);
            if (instr.opcode == OpCodes.Stloc_2) return new CodeInstruction(OpCodes.Ldloc_2);
            if (instr.opcode == OpCodes.Stloc_3) return new CodeInstruction(OpCodes.Ldloc_3);
            if (instr.opcode == OpCodes.Stloc) return new CodeInstruction(OpCodes.Ldloc, instr.operand);
            if (instr.opcode == OpCodes.Stloc_S) return new CodeInstruction(OpCodes.Ldloc_S, instr.operand);
            throw new Exception("Input instruction is not a stloc instruction.");
        }
    }
}
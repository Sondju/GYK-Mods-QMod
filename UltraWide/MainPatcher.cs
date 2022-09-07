using HarmonyLib;
using Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using FlowCanvas.Nodes;
using UnityEngine;

namespace UltraWide;

public class MainPatcher
{
    private static float _newValue;
    private static float _otherNewValue;
    public static void Patch()
    {
        try
        {
            _newValue = Screen.width > 3440 ? 72f : 48f;
            _otherNewValue = Screen.width > 3440 ? 12f : 8f;
            
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.UltraWide");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            // _newValue = Screen.width > 3440 ? 72f : 48f;
            // _otherNewValue = Screen.width > 3440 ? 12f : 8f;
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("UltraWide", $"{message}", error);
    }

    [HarmonyBefore("com.p1xel8ted.graveyardkeeper.LargerScale")]
    [HarmonyPatch(typeof(ResolutionConfig), nameof(ResolutionConfig.GetResolutionConfigOrNull))]
    public static class ResolutionConfigGetResolutionConfigOrNullPatch
    {
        [HarmonyPostfix]
        public static void Postfix(int width, int height, ref ResolutionConfig __result)
        {
            __result ??= new ResolutionConfig(width, height);
        }
    }
    
 
    [HarmonyPatch(typeof(FogObject), nameof(FogObject.Update))]
    public static class FogObjectUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref Vector3 ___TILES_X_VECTOR)
        {
            ___TILES_X_VECTOR = new Vector2(_newValue, 0f);
        }
        
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            var index = -1;
            for (var i = 0; i < code.Count; i++)
            {

                if (code[i].opcode == OpCodes.Ldarg_0 &&
                    code[i+1].opcode == OpCodes.Ldflda &&
                    code[i+2].opcode == OpCodes.Ldfld &&
                    code[i+3].opcode == OpCodes.Ldc_R4 && code[i+3].OperandIs(6) &&
                    code[i+4].opcode == OpCodes.Ldsfld &&
                    code[i+5].opcode == OpCodes.Sub)
                {
                    index = i + 3;
                }
            }

            if (index != -1)
            {
                code[index].operand = _otherNewValue;
                Log($"Patched 6 to {_otherNewValue} in FogUpdate.");
            }
            else
            {
                Log($"Could not patch 6 to {_otherNewValue} in FogUpdate."); 
            }

            return code.AsEnumerable();
        }
    }
    
    [HarmonyPatch(typeof(FogObject), nameof(FogObject.InitFog))]
    public static class InitFogPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref Vector3 ___TILES_X_VECTOR)
        {
            ___TILES_X_VECTOR = new Vector2(_newValue, 0f);
        }
        
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
           
            var index = -1;
            for (var i = 0; i < code.Count; i++)
            {

                if (code[i].opcode == OpCodes.Stloc_1 &&
                    code[i+1].opcode == OpCodes.Ldloc_1 &&
                    code[i+2].opcode == OpCodes.Ldc_I4_6 &&
                    code[i+3].opcode == OpCodes.Blt_S &&
                    code[i+4].opcode == OpCodes.Ret)
                {
                    index = i + 2;
                }
            }

            if (index != -1)
            {
                code[index] = new CodeInstruction(OpCodes.Ldc_I4_S, Convert.ToInt32(_otherNewValue));
                Log($"Patched 6 to {_otherNewValue} in InitFog.");
            }
            else
            {
                Log($"Could not patch 6 to {_otherNewValue} in InitFog."); 
            }

            return code.AsEnumerable();

        }
    }
}
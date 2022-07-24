using FlowCanvas.Nodes;
using HarmonyLib;
using JetBrains.Annotations;
using ParadoxNotion.Serialization.FullSerializer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Helper
{
    // [HarmonyPatchAll]
    internal class MiscFixes
    {
        //stops unnecessary log spam due to spawning clone objects that don't have matching sprites
        [HarmonyPatch(typeof(EasySpritesCollection), nameof(EasySpritesCollection.GetSprite))]
        public static class EasySpritesCollectionGetSpritePatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref bool not_found_is_valid)
            {
                not_found_is_valid = true;
            }
        }

        //stops unnecessary log spam due to wheres ma storage inventory changes
        [HarmonyPatch(typeof(MultiInventory), "IsEmpty")]
        public static class MultiInventoryIsEmptyPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref bool print_log)
            {
                print_log = false;
            }
        }

        //stops unnecessary duplicate objects spam from spawning clone vendors
        //[HarmonyDebug]
        //[HarmonyPatch(typeof(fsJsonParser))]
        //public static class fsJsonParserPatch
        //{
        //    internal static IEnumerable<MethodBase> TargetMethods()
        //    {
        //        var methods = typeof(fsJsonParser).GetMethods(AccessTools.all);

        //        foreach (var method in methods.Where(a => a.Name.Equals("Parse") && a.GetParameters().Length == 2))
        //        {
        //            yield return method;
        //        }
        //    }

        //    [HarmonyTranspiler]
        //    [CanBeNull]
        //    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //    {
               
        //        var otherObj = AccessTools.Field(typeof(fsResult), "Success");

        //        var instructionsList = new List<CodeInstruction>(instructions);
        //        for (var i = 0; i < instructionsList.Count; i++)
        //        {
        //            if (instructionsList[i].opcode == OpCodes.Ldstr && instructionsList[i].operand.ToString().Contains("no") && instructionsList[i + 1].opcode == OpCodes.Call)
        //            {
        //                instructionsList.RemoveRange(i, 3);
        //                instructionsList.InsertRange(i, new List<CodeInstruction>()
        //                {
        //                    [0] = new(OpCodes.Nop),
        //                    [1] = new(OpCodes.Call, otherObj),
        //                    [2] = new(OpCodes.Ret),
        //                });
        //            }
        //        }

        //        return instructionsList.AsEnumerable();
        //    }
        //}

        //stops unnecessary path finding failed log spam
        [HarmonyPatch(typeof(MovementComponent), "OnPathFailed")]
        public static class MovementComponentOnPathFailedPatch
        {
            [HarmonyTranspiler]
            [CanBeNull]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = new List<CodeInstruction>(instructions);
                instructionsList.RemoveRange(0, 9);
                return instructionsList.AsEnumerable();
            }
        }

        //stops unnecessary duplicate objects spam from spawning clone vendors
        [HarmonyPatch(typeof(WorldMap), "GetWorldGameObjectByComparator")]
        public static class WorldMapGetWorldGameObjectByComparatorPatch
        {
            [HarmonyTranspiler]
            [CanBeNull]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = new List<CodeInstruction>(instructions);
                instructionsList.RemoveRange(102, 8);
                return instructionsList.AsEnumerable();
            }
        }

        //stops unnecessary carrying item spr null errors
        [HarmonyPatch(typeof(BaseCharacterComponent), nameof(BaseCharacterComponent.SetCarryingItem), typeof(Item))]
        public static class BaseCharacterComponentSetCarryingItemPatch
        {
            [HarmonyTranspiler]
            [CanBeNull]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = new List<CodeInstruction>(instructions);
                instructionsList.RemoveRange(28, 2);
                return instructionsList.AsEnumerable();
            }
        }

        //stops unnecessary no animator spam
        [HarmonyPatch(typeof(CustomNetworkAnimatorSync), "Init", typeof(GameObject))]
        public static class CustomNetworkAnimatorSyncInitPatch
        {
            [HarmonyTranspiler]
            [CanBeNull]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = new List<CodeInstruction>(instructions);
                instructionsList.RemoveRange(19, 6);
                return instructionsList.AsEnumerable();
            }
        }

        //stops unnecessary duplicate objects spam from spawning clone vendors
        [HarmonyDebug]
        [HarmonyPatch(typeof(Flow_TryFreeIdlePoint))]
        public static class FlowTryFreeIdlePointRegisterPortsPatch
        {
            internal static IEnumerable<MethodBase> TargetMethods()
            {
                var inner = typeof(Flow_TryFreeIdlePoint).GetNestedType("<>c__DisplayClass0_0", AccessTools.all)
                            ?? throw new Exception("Inner Not Found");

                foreach (var method in inner.GetMethods(AccessTools.all))
                {
                    if (method.Name.Contains("<RegisterPorts>") && method.GetParameters().Length == 1)
                    {
                        yield return method;
                    }
                }
            }

            [HarmonyTranspiler]
            [CanBeNull]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instructionsList = new List<CodeInstruction>(instructions);
                for (var i = 0; i < instructionsList.Count; i++)
                {
                    if (instructionsList[i].opcode == OpCodes.Ldstr && instructionsList[i].operand.ToString().Contains("can not") && instructionsList[i + 1].opcode == OpCodes.Call)
                    {
                        instructionsList.RemoveRange(i, 2);
                    }
                }

                return instructionsList.AsEnumerable();
            }
        }
    }
}
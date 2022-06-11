using System;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TheSeedEqualizer
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.TheSeedEqualizer");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
        }

        [HarmonyPatch(typeof(CraftComponent))]
        [HarmonyPatch(nameof(CraftComponent.FillCraftsList))]
        public static class CraftComponentFillCraftsListPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                try
                {
                    GameBalance.me.craft_data.ForEach(craft =>
                    {
                        //if (craft.id.Contains("refugee")) return;
                        if (!(craft.id.Contains("grow_desk_planting") |
                              craft.id.Contains("grow_vineyard_planting"))) return;
                        if (craft.needs == null || craft.needs.Count == 0) return;

                        craft.needs.ForEach(need =>
                        {
                            if (!need.id.Contains("seeds") && !need.id.Contains("seed")) return;
                            foreach (var t in craft.output.Where(t =>
                                         t.id.Contains("seed") || t.id.Contains("seeds")))
                            {
                                t.value = (int) Math.Ceiling(need.value * 1.05);
                                t.min_value = SmartExpression.ParseExpression(t.value.ToString());
                                t.max_value = SmartExpression.ParseExpression(((int)Math.Ceiling(need.value * 1.10)).ToString());
                            }
                        });
                    });
                }
                catch (Exception ex)
                {
                    Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
                    Debug.LogError($"[TheSeedEqualizer] {ex.Message}, {ex.Source}, {ex.StackTrace}");
                    Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
                }
            }
        }
    }
}
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using Helper;
using UnityEngine;

namespace TheSeedEqualizer;

public class MainPatcher
{
    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.TheSeedEqualizer");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("TheSeedEqualizer", $"{message}", error);
    }

    [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.FillCraftsList))]
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
                            t.value = (int)Math.Ceiling(need.value * 1.05);
                            t.min_value = SmartExpression.ParseExpression(t.value.ToString());
                            t.max_value =
                                SmartExpression.ParseExpression(((int)Math.Ceiling(need.value * 1.10)).ToString());
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}",true);
            }
        }
    }
}
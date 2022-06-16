using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace UltraWide;

public class MainPatcher
{
    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.UltraWide");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UltraWide]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
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
}
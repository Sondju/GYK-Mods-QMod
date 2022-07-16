using HarmonyLib;
using System;
using System.Reflection;
using Helper;

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
}
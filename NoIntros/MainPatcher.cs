using HarmonyLib;
using Helper;
using LazyBearGames.Preloader;
using System;
using System.Reflection;

namespace NoIntros;

public class MainPatcher
{
    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.NoIntros");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("NoIntros", $"{message}", error);
    }

    [HarmonyPatch(typeof(LBPreloader), "StartAnimations")]
    public static class LbPreloaderStartAnimationsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(LBPreloader __instance)
        {
            __instance.logos.Clear();
        }
    }
}
using HarmonyLib;
using LazyBearGames.Preloader;
using System.Reflection;
using UnityEngine;

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
        catch (System.Exception ex)
        {
            Debug.LogError($"[NoIntros]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
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
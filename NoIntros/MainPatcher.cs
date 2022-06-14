using System.Reflection;
using HarmonyLib;
using LazyBearGames.Preloader;

namespace NoIntros
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.NoIntros");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(LBPreloader), "StartAnimations")]
        public static class LBPreloaderStartAnimationsPatch
        {
            [HarmonyPrefix]
            public static void Prefix(LBPreloader __instance)
            {
                __instance.logos.Clear();
            }
        }
    }
}

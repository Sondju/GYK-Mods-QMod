using Harmony;
using System.Reflection;
using LazyBearGames.Preloader;

namespace NoIntros
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.NoIntros");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }
        
        [HarmonyPatch(typeof(LBPreloader), "StartAnimations")]
        public static class NoIntroPatch
        {
            [HarmonyPrefix]
            public static void Prefix(LBPreloader __instance)
            {
                __instance.logos.Clear();
            }
        }
    }
}
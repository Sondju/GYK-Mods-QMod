using System.Reflection;
using HarmonyLib;

namespace UltraWide
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.UltraWide");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        [HarmonyBefore("com.p1xel8ted.graveyardkeeper.LargerScale")]
        [HarmonyPatch(typeof(ResolutionConfig), nameof(ResolutionConfig.GetResolutionConfigOrNull))]
        public static class ResPatcher
        {
            [HarmonyPostfix]
            public static void Postfix(int width, int height, ref ResolutionConfig __result)
            {
                __result ??= new ResolutionConfig(width, height);
            }
        }
    }
}
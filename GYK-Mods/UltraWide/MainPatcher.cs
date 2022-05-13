using Harmony;
using System.Reflection;

namespace UltraWide
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.UltraWide");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(ResolutionConfig))]
        [HarmonyPatch(nameof(ResolutionConfig.GetResolutionConfigOrNull))]
        public static class ResPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref ResolutionConfig __result)
            {
                var resolutionConfig = new ResolutionConfig(3440, 1440);
                __result = resolutionConfig;
            }
        }
    }
}
using System.Reflection;
using HarmonyLib;

namespace LargerScale
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        public static void Patch()
        {
            _cfg = Config.GetOptions();

            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.LargerScale");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyAfter("com.p1xel8ted.graveyardkeeper.UltraWide")]
        [HarmonyPatch(typeof(ResolutionConfig), nameof(ResolutionConfig.GetResolutionConfigOrNull))]
        public static class ResolutionConfigPatcher
        {
            [HarmonyPostfix]
            public static void Postfix(int width, int height, ref ResolutionConfig __result)
            {
                __result ??= new ResolutionConfig(width, height)
                {
                    pixel_size = _cfg.GameScale,
                };
            }
        }
    }
}

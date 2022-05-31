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
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        [HarmonyPatch(typeof(ResolutionConfig), nameof(ResolutionConfig.GetResolutionConfigOrNull))]
        public static class Patcher
        {
            [HarmonyPostfix]
            public static void Postfix(ref ResolutionConfig __result)
            {
                var config = new ResolutionConfig(_cfg.Width, _cfg.Height)
                {
                    pixel_size = _cfg.GameScale,
                };
                __result = config;
            }
        }
    }
}
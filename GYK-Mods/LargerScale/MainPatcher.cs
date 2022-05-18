using System.Reflection;
using Harmony;

namespace LargerScale
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        public static void Patch()
        {
            _cfg = Config.GetOptions();
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.LargerScale");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(ResolutionConfig), nameof(ResolutionConfig.GetResolutionConfigOrNull))]
        public static class Patcher
        {
            [HarmonyPostfix]
            static void Postfix(ref ResolutionConfig __result)
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
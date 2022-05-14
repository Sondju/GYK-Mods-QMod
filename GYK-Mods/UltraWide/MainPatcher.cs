using System.Collections.Generic;
using System.Linq;
using Harmony;
using System.Reflection;

namespace UltraWide
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        public static void Patch()
        {
            _cfg = Config.GetOptions();
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.UltraWide");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(ResolutionConfig), "GetResolutionConfigOrNull")]
        public static class ResPatcher
        {
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                codes[19].operand = _cfg.Height;
                codes[22].operand = _cfg.Width;
                return codes.AsEnumerable();
            }
        }
    }
}
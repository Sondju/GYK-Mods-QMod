using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace UltraWide
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        public static void Patch()
        {
            _cfg = Config.GetOptions();

            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.UltraWide");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
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
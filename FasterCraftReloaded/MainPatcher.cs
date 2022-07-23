using HarmonyLib;
using Helper;
using System;
using System.Reflection;

namespace FasterCraftReloaded
{
    public class MainPatcher
    {
        private static Config.Options _cfg;
        public static void Patch()
        {
            try
            {
                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.FasterCraftReloaded");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _cfg = Config.GetOptions();
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        private static void Log(string message, bool error = false)
        {
            Tools.Log("FasterCraftReloaded", $"{message}", error);
        }

        [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.DoAction))]
        public static class Patch1
        {
            [HarmonyPrefix]
            public static void Prefix(ref float delta_time)
            {
                delta_time *= _cfg.CraftSpeedMultiplier;
            }

        }
    }
}
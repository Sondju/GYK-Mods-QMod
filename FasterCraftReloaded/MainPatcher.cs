using HarmonyLib;
using Helper;
using System;
using System.Linq;
using System.Reflection;

namespace FasterCraftReloaded
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        private static readonly string[] Exclude = {
            "zombie","refugee","bee","tree","berry","bush","pump"
        };

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
        public static class CraftComponentDoActionPatch
        {
            [HarmonyPrefix]
            public static void Prefix(CraftComponent __instance, ref float delta_time)
            {
                if (Exclude.Any(__instance.wgo.obj_id.ToLowerInvariant().Contains)) return;
                Log($"[CraftComponent.DoAction]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                delta_time *= _cfg.CraftSpeedMultiplier;
            }
        }
    }
}
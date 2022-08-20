using FasterCraftReloaded.lang;
using HarmonyLib;
using Helper;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace FasterCraftReloaded
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        private static readonly string[] Exclude = {
            "zombie","refugee","bee","tree","berry","bush","pump", "compost", "peat", "slime", "candelabrum", "incense", "garden","planting"
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
            if (_cfg.Debug || error)
            {
                Tools.Log("FasterCraftReloaded", $"{message}", error);
            }
        }

        private static string GetLocalizedString(string content)
        {
            Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
            return content;
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        public static class TimeOfDayUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (Input.GetKeyUp(KeyCode.F5))
                {
                    _cfg = Config.GetOptions();

                    if (!CrossModFields.ConfigReloadShown)
                    {
                        Tools.ShowMessage(GetLocalizedString(strings.ConfigMessage), Vector3.zero);
                        CrossModFields.ConfigReloadShown = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.DoAction))]
        public static class CraftComponentDoActionPatch
        {
            [HarmonyPrefix]
            public static void Prefix(CraftComponent __instance, ref float delta_time, ref WorldGameObject ___other_obj)
            {
                // if (__instance?.current_craft == null) return;
                if (___other_obj == null) return;
                if (!___other_obj.is_player) return;

                if (Exclude.Any(__instance.wgo.obj_id.ToLowerInvariant().Contains))
                {
                    // Log($"[ModifyCraftSpeed - REJECTED]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }

                Log(
                    $"[CC.DoAction]: WGO: {__instance.wgo.obj_id}, WgoIsPlayer: {__instance.wgo.is_player}, Craft: {__instance.current_craft.id}, OtherObj: {___other_obj.obj_id}, OtherWgoIsPlayer: {___other_obj.is_player}");

                delta_time *= _cfg.CraftSpeedMultiplier;
            }
        }

        [HarmonyPatch(typeof(CraftComponent), "ReallyUpdateComponent")]
        public static class CraftComponentReallyUpdateComponentPatch
        {
            [HarmonyPrefix]
            public static void Prefix(CraftComponent __instance, ref float delta_time)
            {
                if (__instance?.current_craft == null) return;

                //   Log(
                //  $"[CraftComponent.ReallyUpdateComponent]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");

                if (_cfg.ModifyCompostSpeed && Tools.CompostCraft(__instance.wgo))
                {
                    delta_time *= _cfg.CompostSpeedMultiplier;
                   //  Log($"[ModifyCompostSpeed]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }

                if (_cfg.ModifyZombieMinesSpeed && Tools.ZombieMineCraft(__instance.wgo))
                {
                    delta_time *= _cfg.ZombieMinesSpeedMultiplier;
                  //   Log($"[ModifyZombieMinesSpeed]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }

                if (_cfg.ModifyZombieSawmillSpeed && Tools.ZombieSawmillCraft(__instance.wgo))
                {
                    delta_time *= _cfg.ZombieSawmillSpeedMultiplier;
                   // Log($"[ZombieSawmillSpeedMultiplier]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }

                if (_cfg.ModifyPlayerGardenSpeed && Tools.PlayerGardenCraft(__instance.wgo))
                {
                    delta_time *= _cfg.PlayerGardenSpeedMultiplier;
                    // Log($"[ModifyPlayerGardenSpeed]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }

                if (_cfg.ModifyRefugeeGardenSpeed && Tools.RefugeeGardenCraft(__instance.wgo))
                {
                    delta_time *= _cfg.RefugeeGardenSpeedMultiplier;
                  //   Log($"[ModifyRefugeeGardenSpeed]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }

                if (_cfg.ModifyZombieGardenSpeed && Tools.ZombieGardenCraft(__instance.wgo))
                {
                    delta_time *= _cfg.ZombieGardenSpeedMultiplier;
                    // Log($"[ModifyZombieGardenSpeed]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }

                if (_cfg.ModifyZombieVineyardSpeed && Tools.ZombieVineyardCraft(__instance.wgo))
                {
                    delta_time *= _cfg.ZombieVineyardSpeedMultiplier;
                   //  Log($"[ModifyZombieVineyardSpeed]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }

                if (Exclude.Any(__instance.wgo.obj_id.ToLowerInvariant().Contains))
                {
                   // Log($"[ModifyCraftSpeed - REJECTED]: WGO: {__instance.wgo.obj_id}, Craft: {__instance.current_craft.id}");
                    return;
                }
                Log(
                    $"[CC.ReallyUpdateComponent]: WGO: {__instance.wgo.obj_id}, WgoIsPlayer: {__instance.wgo.is_player}, Craft: {__instance.current_craft.id}");
                delta_time *= _cfg.CraftSpeedMultiplier;
            }
        }
    }
}
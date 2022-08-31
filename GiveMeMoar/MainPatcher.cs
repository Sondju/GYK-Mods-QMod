using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;
using Helper;
using UnityEngine;
using System.Threading;
using GiveMeMoar.lang;

namespace GiveMeMoar
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        private static readonly List<string> DropList = new()
        {
            "fruit:berry", "fruit:apple_green_crop", "fruit:apple_red_crop", "honey", "beeswax", "ash", "shr_agaric",
            "shr_boletus", "bat_wing", "jelly_slug",
            "jelly_slug_blue", "jelly_slug_orange", "jelly_slug_black", "bee", "slime", "spider_web", "1h_ore_metal",
            "nails_bloody", "nugget_silver", "nugget_gold",
            "graphite", "sand_river", "stick", "stone_plate_1", "sulfur", "clay", "coal", "lifestone", "butterfly",
            "maggot",
            "moth", "flw_chamomile", "flw_dandelion", "flw_poppy", "wheat_seed", "cabbage_seed", "carrot_seed",
            "beet_seed", "onion_seed:1", "onion_seed:2",
            "onion_seed:3", "lentils_seed:1", "lentils_seed:2", "lentils_seed:3", "pumpkin_seed:1", "pumpkin_seed:2",
            "pumpkin_seed:3", "hop_seed:1", "hop_seed:2", "hop_seed:3",
            "hamp_seed:1", "hamp_seed:2", "hamp_seed:3", "grapes_seed:1", "grapes_seed:2", "grapes_seed:3"
        };

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.GiveMeMoar");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _cfg = Config.GetOptions();
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
                if (!MainGame.game_started) return;

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

        [HarmonyPatch(typeof(PrayLogics), "SpreadFaithIncome")]
        public static class DropMultiPatches2
        {
            [HarmonyPrefix]
            private static void Prefix(ref int faith)
            {
                faith *= _cfg.faithMultiplier;
            }
        }

        [HarmonyPatch(typeof(SoulsHelper), "CalculatePointsAfterSoulRelease")]
        public static class DropMultiPatches3
        {
            [HarmonyPostfix]
            private static void Postfix(ref float __result)
            {
                if (_cfg.gratitudeMultiplier > 1f)
                {
                    __result *= _cfg.gratitudeMultiplier;
                }
            }
        }


        [HarmonyPatch(typeof(DropResGameObject), "Drop", typeof(Vector3), typeof(Item), typeof(Transform),
            typeof(Direction), typeof(float), typeof(int), typeof(bool), typeof(bool))]
        public static class DropMultiPatches5
        {
            [HarmonyPrefix]
            private static void Prefix(ref Item item)
            {
                if (DropList.Contains(item.id) && _cfg.resourceMultiplier > 1f)
                {
                    item.value *= _cfg.resourceMultiplier;
                    return;
                }

                if (item.id == "sin_shard" && _cfg.sinShardMultiplier > 1f)
                {
                    item.value *= _cfg.sinShardMultiplier;
                }
            }
        }

        [HarmonyPatch(typeof(PrayLogics), "SpreadMoneyIncome")]
        public static class DropMultiPatches4
        {
            [HarmonyPrefix]
            private static void Prefix(ref float money)
            {
                money *= _cfg.donationMultiplier;
            }
        }

        [HarmonyPatch(typeof(TechPointsDrop), "Drop", typeof(Vector3), typeof(int), typeof(int), typeof(int))]
        public static class DropMultiPatches1
        {
            [HarmonyPrefix]
            private static void Drop(ref int r, ref int g, ref int b)
            {
                r *= _cfg.redTechPointMultiplier;
                g *= _cfg.greenTechPointMultiplier;
                b *= _cfg.blueTechPointMultiplier;
            }
        }
    }
}
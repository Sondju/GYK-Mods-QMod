using System;
using HarmonyLib;
using Rewired;
using System.Globalization;
using System.Reflection;
using System.Threading;
using BeamMeUpGerry.lang;
using UnityEngine;

namespace BeamMeUpGerry
{
    public class MainPatcher
    {
        private static Config.Options _cfg;
        private static bool _showCooldownReadyAlert;
        private static string Lang { get; set; }

        public static void Patch()
        {
            try
            {
                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.BeamMeUpGerry");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _cfg = Config.GetOptions();
                _showCooldownReadyAlert = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BeamMeUpGerry]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
            }
        }

        private static Item GetHearthstone()
        {
            return MainGame.me.player.data.GetItemWithID("hearthstone");
        }

        [HarmonyPatch(typeof(MultiAnswerGUI), nameof(MultiAnswerGUI.OnChosen))]
        public static class MultiAnswerGuiOnChosenPatch
        {
            [HarmonyPostfix]
            public static void Postfix(string answer)
            {
                _showCooldownReadyAlert = !string.Equals("cancel", answer);

            }
        }

        [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.ApplyLanguageChange))]
        public static class GameSettingsApplyLanguageChange
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
            }
        }

        private static void Beam()
        {
            var item = GetHearthstone();
            if (item != null)
            {
                if (item.GetGrayedCooldownPercent() > 0)
                {
                    ShowMessage(strings.NotReady);
                }
                else
                {
                    MainGame.me.player.UseItemFromInventory(item);
                }
            }
            else
            {
                ShowMessage(strings.WhereIsIt);
            }
        }

        private static void ShowMessage(string msg)
        {
            if (_cfg.DisableAlerts) return;
            if (_cfg.DisableGerryVoice)
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
                MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                    SmartSpeechEngine.VoiceID.None, true);
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
                MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.InfoBox,
                    SmartSpeechEngine.VoiceID.Skull);
            }
        }


        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        public static class TimeOfDayUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (!MainGame.game_started || MainGame.me.player.is_dead || MainGame.me.player.IsDisabled() ||
                    MainGame.paused || !BaseGUI.all_guis_closed) return;

                if (LazyInput.gamepad_active && ReInput.players.GetPlayer(0).GetButtonDown(7))
                {
                    Beam();
                }

                if (Input.GetKeyUp(KeyCode.Z))
                {
                    Beam();
                }
            }


            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!MainGame.game_started || MainGame.me.player.is_dead || MainGame.me.player.IsDisabled() ||
                    MainGame.paused || !BaseGUI.all_guis_closed) return;
                var item = GetHearthstone();
                if (item == null) return;
                if (item.GetGrayedCooldownPercent() <= 0 && _showCooldownReadyAlert)
                {
                    _showCooldownReadyAlert = false;
                    ShowMessage(strings.Ready);
                }
            }
        }

        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch(nameof(Item.GetGrayedCooldownPercent))]
        public static class ItemGetGrayedCooldownPercentPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref Item __instance, ref int __result)
            {
                if (__instance is not { id: "hearthstone" }) return;
                if (_cfg.DisableCooldown)
                {
                    __result = 0;
                    return;
                }
                if (_cfg.HalfCooldown)
                {
                    __result /= 2;
                }
            }
        }
    }
}
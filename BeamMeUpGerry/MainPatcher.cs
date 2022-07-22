using BeamMeUpGerry.lang;
using HarmonyLib;
using Helper;
using Rewired;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using NodeCanvas.Tasks.Actions;
using UnityEngine;

namespace BeamMeUpGerry
{
    public class MainPatcher
    {
        private static Config.Options _cfg;
        private static bool _showCooldownReadyAlert;
        private static string Lang { get; set; }
        private static bool _menuShown;
        private static bool _usingStone;

        private static readonly Dictionary<string, Vector3> LocationByVector = new()
        {
            { "zone_witch_hut", new Vector3(-4964.0f, -1772.0f, -370.2f) },
            { "zone_cellar", new Vector3(10841.9f, -9241.7f, -1923.1f) },
            { "zone_alchemy", new Vector3(8249.0f, -10180.7f, -2119.3f) },
            { "zone_morgue", new Vector3(9744.0f, -11327.5f, -2357.9f) },
            { "zone_beegarden", new Vector3(3234.0f, 1815.0f, 378.81f) },
            { "zone_hill", new Vector3(8292.7f, 1396.6f, 292.71f) },
            { "zone_sacrifice", new Vector3(9529.1f, -8427.1f, -1753.71f) },
            { "zone_souls", new Vector3(11050.1f, -10807.1f, -2249.21f) },
            { "zone_graveyard", new Vector3(1635.7f, -1506.9f, -313.61f) },
            { "zone_euric_room", new Vector3(20108.0f, -11599.6f, -2412.41f) },
            { "zone_church", new Vector3(190.6f, -8715.7f, -1815.7f) },
            { "zone_zombie_sawmill", new Vector3(2204.3f, 3409.7f, 710.8f) },
            { "cancel", Vector3.zero }
        };

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
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        private static void Log(string message, bool error = false)
        {
            Tools.Log("BeamMeUpGerry", $"{message}", error);
        }

        private static Item GetHearthstone()
        {
            return MainGame.me.player.data.GetItemWithID("hearthstone");
        }

        [HarmonyPatch(typeof(MultiAnswerGUI), "ShowAnswers", new[] { typeof(List<AnswerVisualData>), typeof(bool) })]
        public static class MultiAnswerGuiShowAnswersPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref MultiAnswerGUI __instance, ref List<AnswerVisualData> answers)
            {
                if (__instance == null) return;
                if (_menuShown || !_usingStone) return;
                answers.Insert(answers.Count - 1, new AnswerVisualData()
                {
                    id = "..."
                });
            }
        }

        [HarmonyPatch(typeof(MultiAnswerGUI), nameof(MultiAnswerGUI.OnChosen))]
        public static class MultiAnswerGuiOnChosenPatch
        {
            private static MultiAnswerGUI instance;

            [HarmonyPrefix]
            public static void Prefix(ref MultiAnswerGUI __instance, ref string answer)
            {
                instance = __instance;
                if (answer != "...") return;
                var answers = LocationByVector.Select(location => new AnswerVisualData() { id = location.Key }).ToList();
                answer = "cancel";
                _menuShown = true;
                MainGame.me.player.ShowMultianswer(answers, BeamGerryOnChosen);
            }

            private static void BeamGerryOnChosen(string chosen)
            {
                if (string.Equals("cancel", chosen))
                {
                    _showCooldownReadyAlert = false;
                    _menuShown = false;
                    _usingStone = false;
                    MainGame.me.player.components.character.control_enabled = true;
                    return;
                }
                
                var vectorFound = LocationByVector.TryGetValue(chosen, out var vector);
                if (vectorFound)
                {
                    if (_cfg.FadeForCustomLocations)
                    {
                        CameraFader.current.FadeOut(0.15f);
                        GJTimer.AddTimer(0.15f, delegate
                        {
                            MainGame.me.player.PlaceAtPos(vector);
                            MainGame.me.player.components.character.control_enabled = true;
                            GJTimer.AddTimer(1.5f, delegate { CameraFader.current.FadeIn(0.15f); });
                        });
                    }
                    else
                    {
                        MainGame.me.player.PlaceAtPos(vector);
                        MainGame.me.player.components.character.control_enabled = true;
                    }
                }
                else
                {
                    MainGame.me.player.Say("I don't know where that is!");
                }
                _menuShown = false;
            }

            [HarmonyPostfix]
            public static void Postfix(string answer)
            {
                MainGame.me.player.components.character.control_enabled = !_menuShown;
                // _showCooldownReadyAlert = !
                if (string.Equals("cancel", answer))
                {
                    _showCooldownReadyAlert = false;
                }
                else
                {
                    _showCooldownReadyAlert = true;
                }
                _usingStone = false;
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
                    _usingStone = true;
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
                    Log($"[ZONE]: {GJL.L("zone_" + MainGame.me.player.GetMyWorldZoneId())}, ID: {"zone_" + MainGame.me.player.GetMyWorldZoneId()}, Vector: {MainGame.me.player_pos}");
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
                if (item.GetGrayedCooldownPercent() <= 0 && _showCooldownReadyAlert && !_cfg.DisableCooldown)
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
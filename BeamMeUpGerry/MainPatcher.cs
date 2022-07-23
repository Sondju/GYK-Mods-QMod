using BeamMeUpGerry.lang;
using HarmonyLib;
using Helper;
using NodeCanvas.Tasks.Actions;
using Rewired;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace BeamMeUpGerry
{
    public class MainPatcher
    {
        private static Config.Options _cfg;
        private static bool _showCooldownReadyAlert;
        private static string Lang { get; set; }
        private static bool _dotSelection;
        private static bool _usingStone;

        private static readonly Dictionary<string, Vector3> LocationByVectorPartOne = new()
        {
            { "zone_witch_hut", new Vector3(-4964.0f, -1772.0f, -370.2f) },
            { "zone_cellar", new Vector3(10841.9f, -9241.7f, -1923.1f) },
            { "zone_alchemy", new Vector3(8249.0f, -10180.7f, -2119.3f) },
            { "zone_morgue", new Vector3(9744.0f, -11327.5f, -2357.9f) },
            { "zone_beegarden", new Vector3(3234.0f, 1815.0f, 378.81f) },
            { "zone_hill", new Vector3(8292.7f, 1396.6f, 292.71f) },
            { "zone_sacrifice", new Vector3(9529.1f, -8427.1f, -1753.71f) },
            { "....", Vector3.zero },
            { "cancel", Vector3.zero }
        };

        private static readonly Dictionary<string, Vector3> LocationByVectorPartTwo = new()
        {
            { "zone_souls", new Vector3(11050.1f, -10807.1f, -2249.21f) },
            { "zone_graveyard", new Vector3(1635.7f, -1506.9f, -313.61f) },
            { "zone_euric_room", new Vector3(20108.0f, -11599.6f, -2412.41f) },
            { "zone_church", new Vector3(190.6f, -8715.7f, -1815.7f) },
            { "zone_zombie_sawmill", new Vector3(2204.3f, 3409.7f, 710.8f) },
            { strings.Clay, new Vector3(595.4f, -3185.8f, -663.6f) },
            { strings.Sand, new Vector3(334.3f, 875.9f, 182.5f) },
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

        private static bool RemoveZone(AnswerVisualData answer)
        {
            if (answer.id.Contains("Clay") || answer.id.Contains("Sand") || answer.id.Contains("...") || answer.id.Contains("....") || answer.id.Contains("cancel")) return false;
            var zone = answer.id.Replace("zone_", "");
            if(MainGame.me.save.known_world_zones.Exists(a => string.Equals(a, zone)))
            {
                Log($"[RemoveZone]: Player knows {zone}. NOT removing.");
                return false;
            }
            Log($"[RemoveZone]: Player does not know {zone}. Removing.");
            return true;
        }
        
        private static List<AnswerVisualData> ValidateAnswerList(IEnumerable<AnswerVisualData> answers)
        {
            return answers.Where(answer => !RemoveZone(answer)).ToList();
        }

        private static MultiAnswerGUI _maGui;

        [HarmonyPatch(typeof(MultiAnswerGUI), "ShowAnswers", typeof(List<AnswerVisualData>), typeof(bool))]
        public static class MultiAnswerGuiShowAnswersPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref MultiAnswerGUI __instance, ref List<AnswerVisualData> answers)
            {
               
                if (__instance == null) return;

                _maGui = __instance;

                if (_dotSelection) return;
                if (!_usingStone) return;

                answers.Insert(answers.Count - 1, new AnswerVisualData()
                {
                    id = "..."
                });
            }
        }

        [HarmonyPatch(typeof(MultiAnswerGUI), nameof(MultiAnswerGUI.OnChosen))]
        public static class MultiAnswerGuiOnChosenPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref string answer)
            {
                if (_isNpc) return;
                List<AnswerVisualData> answers;

                _dotSelection = false;

                if (answer == "...")
                {
                    answers = LocationByVectorPartOne.Select(location => new AnswerVisualData() { id = location.Key }).ToList();
                    Show(out answer);
                }

                if (answer == "....")
                {
                    answers = LocationByVectorPartTwo.Select(location => new AnswerVisualData() { id = location.Key }).ToList();
                    Show(out answer);
                }

                void Show(out string answer)
                {
                    _isNpc = false;
                    var cleanedAnswers = ValidateAnswerList(answers);
                    answer = "cancel";
                    _dotSelection = true;
                    _usingStone = true;
                    MainGame.me.player.components.character.control_enabled = false;
                    MainGame.me.player.ShowMultianswer(cleanedAnswers, BeamGerryOnChosen);
                }
            }

            private static void BeamGerryOnChosen(string chosen)
            {
                if (_isNpc) return;
                if (string.Equals("cancel", chosen))
                {
                    return;
                }

                var partOne = LocationByVectorPartOne.TryGetValue(chosen, out var vectorOne);
                var partTwo = LocationByVectorPartTwo.TryGetValue(chosen, out var vectorTwo);

                Vector3 vector;
                if (partOne)
                {
                    vector = vectorOne;
                }
                else if (partTwo)
                {
                    vector = vectorTwo;
                }
                else
                {
                    vector = MainGame.me.player_pos;
                }

                var found = partOne || partTwo;
                if (found)
                {
                    if (_cfg.FadeForCustomLocations)
                    {
                        CameraFader.current.FadeOut(0.15f);
                        GJTimer.AddTimer(0.15f, delegate
                        {
                            MainGame.me.player.PlaceAtPos(vector);
                            MainGame.me.player.components.character.control_enabled = true;
                            GJTimer.AddTimer(1.25f, delegate { CameraFader.current.FadeIn(0.15f); });
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
                    MainGame.me.player.components.character.control_enabled = true;
                    _maGui.DestroyBubble();
                }
            }

            [HarmonyPostfix]
            public static void Postfix(string answer)
            {
               
                if (string.Equals("cancel", answer) && !_dotSelection)
                {
                    //real cancel
                    _usingStone = false;
                    _dotSelection = false;
                    MainGame.me.player.components.character.control_enabled = true;
                    return;
                }
                if (string.Equals("cancel", answer) && _dotSelection)
                {
                    //fake cancel to close the old menu and open a new one
                    _usingStone = true;
                    _dotSelection = true;
                    MainGame.me.player.components.character.control_enabled = false;
                    return;
                }
                if (_isNpc) return;
                _usingStone = false;
                _dotSelection = false;
                MainGame.me.player.components.character.control_enabled = true;
            }
        }

        private static bool _isNpc;
    
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

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref WorldGameObject __instance, ref WorldGameObject other_obj)
            {
                _isNpc = __instance.obj_def.IsNPC();
                //Log($"[WorldGameObject.Interact]: Instance: {__instance.obj_id}, InstanceIsPlayer: {__instance.is_player},  Other: {other_obj.obj_id}, OtherIsPlayer: {other_obj.is_player}");
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
                    _isNpc = false;
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
                    Log($"[ZONE]: {GJL.L("zone_" + MainGame.me.player.GetMyWorldZoneId())}, ID: {"zone_" + MainGame.me.player.GetMyWorldZoneId()}, Vector: {MainGame.me.player_pos}");
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
using BeamMeUpGerry.lang;
using FlowCanvas.Nodes;
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
        private const float Fee = 5f;
        // private const float Fee = 100000f; //testing

        private static readonly Dictionary<string, Vector3> LocationByVectorPartOne = new()
        {
            { "zone_witch_hut", new Vector3(-4964.0f, -1772.0f, -370.2f) }, //gd_witch_27
            { "zone_cellar", new Vector3(10841.9f, -9241.7f, -1923.1f) },
            { "zone_alchemy", new Vector3(8249.0f, -10180.7f, -2119.3f) },
            { "zone_morgue", new Vector3(9744.0f, -11327.5f, -2357.9f) },
            { "zone_beegarden", new Vector3(3234.0f, 1815.0f, 378.81f) },
            { "zone_hill", new Vector3(8292.7f, 1396.6f, 292.71f) },
            { "zone_sacrifice", new Vector3(9529.1f, -8427.1f, -1753.71f) },
            { "zone_beatch", new Vector3(22507.9f, 314.9f, 70.3f) },
            { "zone_vineyard", new Vector3(6712.3f, 42.1f, 10.2f) },
            { "zone_camp", new Vector3(20690.7f, 2818.7f, 591.5f) },
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
            { strings.Mill, new Vector3(11805.2f, -768.9f, -157.7f) }, //mill_to_crossroads
            { strings.Farmer, new Vector3(11800.7f, -3251.7f, -675.0f) }, //none suitable
            { "cancel", Vector3.zero }
        };

        private static Config.Options _cfg;
        private static bool _dotSelection;
        private static bool _isNpc;
        private static MultiAnswerGUI _maGui;
        private static bool _usingStone;
        private static string Lang { get; set; }

        public static void Patch()
        {
            try
            {
                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.BeamMeUpGerry");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _cfg = Config.GetOptions();
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        private static void Beam()
        {
            if (_usingStone || _dotSelection || _isNpc) return;

            var item = GetHearthstone();
            if (item != null)
            {
                _usingStone = true;
                _isNpc = false;
                MainGame.me.player.UseItemFromInventory(item);
            }
            else
            {
                SpawnGerry(strings.WhereIsIt, Vector3.zero);
            }
        }

        private static Item GetHearthstone()
        {
            return MainGame.me.player.data.GetItemWithID("hearthstone");
        }

        private static void Log(string message, bool error = false)
        {
            Tools.Log("BeamMeUpGerry", $"{message}", error);
        }

        private static bool RemoveZone(AnswerVisualData answer)
        {
            var wheatExists = MainGame.me.save.known_world_zones.Exists(a => string.Equals(a, "zone_wheat_land"));
            if (answer.id.Contains(strings.Farmer))
            {
                return wheatExists && MainGame.me.save.known_npcs.npcs.Exists(a => a.npc_id.Contains("farmer"));
            }

            if (answer.id.Contains(strings.Mill))
            {
                return wheatExists && MainGame.me.save.known_npcs.npcs.Exists(a => a.npc_id.Contains("miller"));
            }

            if (answer.id.Contains(strings.Clay) || answer.id.Contains(strings.Sand) || answer.id.Contains("...") || answer.id.Contains("....") || answer.id.Contains("cancel")) return false;
            var zone = answer.id.Replace("zone_", "");
            if (MainGame.me.save.known_world_zones.Exists(a => string.Equals(a, zone)))
            {
                Log($"[RemoveZone]: Player knows {zone}. NOT removing.");
                return false;
            }
            Log($"[RemoveZone]: Player does not know {zone}. Removing.");
            return true;
        }

        private static void SpawnGerry(string message, Vector3 customPosition)
        {
            var location = MainGame.me.player_pos;
            location.x += 125f;
            location.y += 125f;
            if (customPosition != Vector3.zero)
            {
                location = customPosition;
            }
            var gerry = WorldMap.SpawnWGO(MainGame.me.world_root.transform, "talking_skull", location);
            gerry.ReplaceWithObject("talking_skull", true);

            GJTimer.AddTimer(0.5f, delegate
            {
                gerry.Say(message, delegate
                {
                    GJTimer.AddTimer(0.25f, delegate
                    {
                        gerry.ReplaceWithObject("talking_skull", true);
                        gerry.DestroyMe();
                    });
                }, null, SpeechBubbleGUI.SpeechBubbleType.Talk, SmartSpeechEngine.VoiceID.Skull);
            });
        }

        private static List<AnswerVisualData> ValidateAnswerList(IEnumerable<AnswerVisualData> answers)
        {
            return answers.Where(answer => !RemoveZone(answer)).ToList();
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

        [HarmonyPatch(typeof(Item))]
        [HarmonyPatch(nameof(Item.GetGrayedCooldownPercent))]
        public static class ItemGetGrayedCooldownPercentPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref Item __instance, ref int __result)
            {
                if (__instance is not { id: "hearthstone" }) return;

                __result = 0;
            }
        }

        [HarmonyPatch(typeof(MultiAnswerGUI), nameof(MultiAnswerGUI.OnChosen))]
        public static class MultiAnswerGuiOnChosenPatch
        {
            [HarmonyPostfix]
            public static void Postfix(string answer)
            {
                if (!_cfg.EnableListExpansion) return;
                Log($"[Answer]: {answer}");

                if (string.Equals("cancel", answer) && !_dotSelection)
                {
                    //real cancel
                    _usingStone = false;
                    _dotSelection = false;
                    _isNpc = false;

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

                if (string.Equals("leave", answer.ToLowerInvariant()))
                {
                    //leave option for npcs
                    _usingStone = false;
                    _dotSelection = false;
                    _isNpc = false;
                    MainGame.me.player.components.character.control_enabled = true;

                    return;
                }

                if (_isNpc) return;
                _usingStone = false;
                _dotSelection = false;

                MainGame.me.player.components.character.control_enabled = true;
            }

            [HarmonyPrefix]
            public static void Prefix(ref string answer)
            {
                if (!_cfg.EnableListExpansion) return;
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
                if (!_cfg.EnableListExpansion) return;
                if (_isNpc) return;
                if (string.Equals("cancel", chosen))
                {
                    return;
                }

                if (MainGame.me.player.data.money < Fee)
                {
                    var location = MainGame.me.player_pos;
                    location.x += 125f;
                    location.y += 125f;
                    SpawnGerry("You need more coin!", Vector3.zero);
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
                            MainGame.me.save.ApplyCurrentEnvironmentPreset();
                            MainGame.me.player.components.character.control_enabled = true;
                            GJTimer.AddTimer(1.25f, delegate
                            {
                                CameraFader.current.FadeIn(0.15f);
                                GJTimer.AddTimer(0.20f, delegate
                                {
                                    vector.y += 125f;
                                    MainGame.me.player.data.money -= Fee;
                                    Sounds.PlaySound("coins_sound", vector, true);
                                    EffectBubblesManager.ShowImmediately(vector, $"-{Trading.FormatMoney(Fee, true)}", EffectBubblesManager.BubbleColor.Red, true, 3f);
                                });
                            });
                        });
                    }
                    else
                    {
                        vector.y += 125f;
                        MainGame.me.player.PlaceAtPos(vector);
                        MainGame.me.save.ApplyCurrentEnvironmentPreset();
                        MainGame.me.player.components.character.control_enabled = true;
                        MainGame.me.player.data.money -= Fee;
                        Sounds.PlaySound("coins_sound", vector, true);
                        EffectBubblesManager.ShowImmediately(vector, $"-{Trading.FormatMoney(Fee, true)}", EffectBubblesManager.BubbleColor.Red, true, 3f);
                    }
                }
                else
                {
                    MainGame.me.player.Say("I don't know where that is!");
                    MainGame.me.player.components.character.control_enabled = true;
                    _maGui.DestroyBubble();
                }
            }
        }

        [HarmonyPatch(typeof(Flow_MultiAnswer), "RegisterPorts")]
        public static class FlowMultiAnswerRegisterPortsPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref Flow_MultiAnswer __instance)
            {
                if (__instance == null) return;
                if (!_usingStone) return;

                if (!_cfg.EnableListExpansion) return;
                if (_dotSelection) return;

                __instance.answers.Insert(__instance.answers.Count - 1, @"...");
            }
        }

        [HarmonyPatch(typeof(MultiAnswerGUI), "ShowAnswers", typeof(List<AnswerVisualData>), typeof(bool))]
        public static class MultiAnswerGuiShowAnswersPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref MultiAnswerGUI __instance)
            {
                if (__instance == null) return;

                _maGui = __instance;

                if (!_usingStone) return;
                if (_cfg.IncreaseMenuAnimationSpeed)
                {
                    __instance.anim_delay /= 3f;
                    __instance.anim_time /= 3f;
                }
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
                    DoLoggingAndBeam();
                }

                if (Input.GetKeyUp(KeyCode.Z))
                {
                    DoLoggingAndBeam();
                }

                if (Input.GetKeyUp(KeyCode.Escape) ||
                    (LazyInput.gamepad_active && ReInput.players.GetPlayer(0).GetButtonDown(3)))
                {
                    if (_maGui != null)
                    {
                        Sounds.OnClosePressed();
                        _maGui.DestroyBubble();
                        _usingStone = false;
                        _dotSelection = false;
                        _isNpc = false;
                        MainGame.me.player.components.character.control_enabled = true;
                        _maGui = null;
                    }
                }

                static void DoLoggingAndBeam()
                {
                    Log($"[ZONE]: {GJL.L("zone_" + MainGame.me.player.GetMyWorldZoneId())}, ID: {"zone_" + MainGame.me.player.GetMyWorldZoneId()}, Vector: {MainGame.me.player_pos}");
                    var gdPoint = Util.FindNearestGdPoint();
                    if (gdPoint != null)
                    {
                        //SpawnGerry("Here!", gdPoint.pos);
                        var distance = Vector3.Distance(MainGame.me.player_pos, gdPoint.pos);
                        Log($"[GDPoint:] Nearest: {gdPoint.name}, Distance to player: {distance}, Vector: {gdPoint.pos}");
                    }
                    Beam();
                }
            }
        }

        [HarmonyPatch(typeof(SmartAudioEngine))]
        private static class SmartAudioEnginePatch
        {
            [HarmonyPatch(nameof(SmartAudioEngine.OnStartNPCInteraction))]
            [HarmonyPostfix]
            public static void OnStartNPCInteractionPostfix()
            {
                _isNpc = true;
            }

            [HarmonyPatch(nameof(SmartAudioEngine.OnEndNPCInteraction))]
            [HarmonyPostfix]
            public static void OnEndNPCInteractionPostfix()
            {
                _isNpc = false;
            }
        }

        [HarmonyPatch(typeof(VendorGUI))]
        public static class VendorGuiPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(VendorGUI.Hide), typeof(bool))]
            public static void VendorGuiHidePostfix()
            {
                _isNpc = false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(VendorGUI.OnClosePressed))]
            public static void VendorGUIOnClosePressedPostfix()
            {
                _isNpc = false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(VendorGUI.Open), typeof(WorldGameObject), typeof(GJCommons.VoidDelegate))]
            public static void VendorGuiOpenPostfix()
            {
                _isNpc = true;
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref WorldGameObject __instance, ref WorldGameObject other_obj)
            {
                _isNpc = __instance.obj_def.IsNPC();
              //  Log($"[WorldGameObject.Interact]: Instance: {__instance.obj_id}, InstanceIsPlayer: {__instance.is_player},  Other: {other_obj.obj_id}, OtherIsPlayer: {other_obj.is_player}");
            }
        }
    }
}
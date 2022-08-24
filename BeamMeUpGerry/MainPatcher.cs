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
using Random = System.Random;

namespace BeamMeUpGerry
{
    public class MainPatcher
    {
        private static readonly List<Location> LocationsPartOne = new()
        {
            new Location("zone_witch_hut","",new Vector3(-4964.0f, -1772.0f, -370.2f)),
            new Location("zone_cellar","mortuary",new Vector3(10841.9f, -9241.7f, -1923.1f), EnvironmentEngine.State.Inside),
            new Location("zone_alchemy","mortuary",new Vector3(8249.0f, -10180.7f, -2119.3f), EnvironmentEngine.State.Inside),
            new Location("zone_morgue","mortuary",new Vector3(9744.0f, -11327.5f, -2357.9f), EnvironmentEngine.State.Inside),
            new Location("zone_beegarden","",new Vector3(3234.0f, 1815.0f, 378.81f)),
            new Location("zone_hill","",new Vector3(8292.7f, 1396.6f, 292.71f)),
            new Location("zone_sacrifice","",new Vector3(9529.1f, -8427.1f, -1753.71f), EnvironmentEngine.State.Inside),
            new Location("zone_beatch","", new Vector3(22507.9f, 314.9f, 70.3f)),
            new Location("zone_vineyard","",new Vector3(6712.3f, 42.1f, 10.2f) ),
            new Location("zone_camp","",new Vector3(20690.7f, 2818.7f, 591.5f)),
            new Location("....","",Vector3.zero),
            new Location("cancel","",Vector3.zero),
        };

        private static readonly List<Location> LocationsPartTwo = new()
        {
            new Location("zone_souls","mortuary",new Vector3(11050.1f, -10807.1f, -2249.21f), EnvironmentEngine.State.Inside),
            new Location("zone_graveyard","",new Vector3(1635.7f, -1506.9f, -313.61f)),
            new Location("zone_euric_room","euric",new Vector3(20108.0f, -11599.6f, -2412.41f), EnvironmentEngine.State.Inside),
            new Location("zone_church","church",new Vector3(182.4f, -8218.1f, -1712.1f), EnvironmentEngine.State.Inside),
            new Location("zone_zombie_sawmill","",new Vector3(2204.3f, 3409.7f, 710.8f) ),
            new Location(strings.Coal,"",new Vector3(-505.5f, 6098.0f, 1270.3f)),
            new Location(strings.Clay,"",new Vector3(595.4f, -3185.8f, -663.6f) ),
            new Location(strings.Sand,"",  new Vector3(334.3f, 875.9f, 182.5f)),
            new Location(strings.Mill,"", new Vector3(11805.2f, -768.9f, -157.7f)  ),
            new Location(strings.Farmer,"",new Vector3(11800.7f, -3251.7f, -675.0f)),
            new Location("cancel","",Vector3.zero),
        };
        
        private static Config.Options _cfg;
        private static bool _dotSelection;
        private static MultiAnswerGUI _maGui;
        private static bool _usingStone;

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
            if (InTutorial()) return;
            if (_usingStone || _dotSelection || CrossModFields._talkingToNpc) return;

            var item = GetHearthstone();
            if (item != null)
            {
                if (CrossModFields.IsInDungeon)
                {
                    _usingStone = false;
                    SpawnGerry(strings.CantUseHere, Vector3.zero);
                }
                else
                {
                    _usingStone = true;
                    MainGame.me.player.UseItemFromInventory(item);
                }
                CrossModFields.TalkingToNpc("BeamMeUpGerry: Beam()",false);
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
            if (_cfg.Debug || error)
            {
                Tools.Log("BeamMeUpGerry", $"{message}", error);
            }
        }

        private static bool RemoveZone(AnswerVisualData answer)
        {
          
           var wheatExists = Tools.PlayerHasSeenZone("zone_wheat_land");
           var coalExists = Tools.PlayerHasSeenZone("zone_flat_under_waterflow");
            if (answer.id.Contains(strings.Farmer))
            {
                return wheatExists && Tools.PlayerKnowsNpcPartial("farmer");
            }

            if (answer.id.Contains(strings.Mill))
            {
                return wheatExists && Tools.PlayerKnowsNpcPartial("miller");
            }

            if (answer.id.Contains(strings.Coal))
            {
                return coalExists;
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

        private static string GetMoneyMessage()
        {
            var rng = new Random();
            var messageList = new List<string>
            {
                strings.M1,strings.M2,strings.M3,strings.M4,strings.M5,strings.M6,strings.M7,
                strings.M8,strings.M9,strings.M10
            };
            var shuffledList = messageList.OrderBy(_ => rng.Next()).ToList();
            return shuffledList[0];
        }

        private static WorldGameObject _gerry;
        private static bool _gerryRunning;

        private static void SpawnGerry(string message, Vector3 customPosition, bool money = false)
        {
            if (_gerryRunning) return;
            Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
            var location = MainGame.me.player_pos;
            location.x -= 75f;
            //location.y += 50f;
            if (customPosition != Vector3.zero)
            {
                location = customPosition;
            }

            if (_gerry == null)
            {
                _gerry = WorldMap.SpawnWGO(MainGame.me.world_root.transform, "talking_skull", location);
                _gerry.ReplaceWithObject("talking_skull", true);
                _gerryRunning = true;
            }

            
            GJTimer.AddTimer(0.5f, delegate
            {
                if (_gerry == null) return;
                _gerry.Say(!money ? message : $"{GetMoneyMessage()}", delegate
                {
                    GJTimer.AddTimer(0.25f, delegate
                    {
                        if (_gerry == null) return;
                        _gerry.ReplaceWithObject("talking_skull", true);
                        _gerry.DestroyMe();
                        _gerry = null;
                        _gerryRunning = false;
                        if (!money) return;

                        TakeMoney(MainGame.me.player_pos);
                    });
                }, null, SpeechBubbleGUI.SpeechBubbleType.Talk, SmartSpeechEngine.VoiceID.Skull);
            });
        }

        private static List<AnswerVisualData> ValidateAnswerList(IEnumerable<AnswerVisualData> answers)
        {
            return answers.Where(answer => !RemoveZone(answer)).ToList();
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

        //[HarmonyPatch(typeof(EnvironmentPreset))]
        //[HarmonyPatch(nameof(EnvironmentPreset.Load))]
        //public static class EnvironmentPresetLoadPatch
        //{
        //    [HarmonyPrefix]
        //    public static void Prefix(string id)
        //    {
        //        Log($"[EnvironmentPresetLoad]: {id}",true);
        //    }
        //}

        private static void ShowHud(Location chosen, bool animate = false)
        {
            GUIElements.me.EnableHUD(true);
            GUIElements.ChangeHUDAlpha(true, animate);
            GUIElements.ChangeBubblesVisibility(true);
            GUIElements.me.overhead_panel.gameObject.SetActive(true);
            GUIElements.me.relation.ChangeHUDAlpha(true, animate);
            GUIElements.me.relation.Update();

            if (chosen == null) return;
      
            EnvironmentEngine.me.SetEngineGlobalState(chosen.State);
            Log($"[ApplyCurrentEnvironmentPreset, id] = {chosen.Preset}");
            var environmentPreset = EnvironmentPreset.Load(chosen.Preset);
            EnvironmentEngine.me.ApplyEnvironmentPreset(environmentPreset);
        }

        private static float GenerateFee()
        {
            var dynamicFee = (float)Math.Round((0.1f * MainGame.me.player.data.money) / 100f, 2);
            const float minimumFee = 0.01f;
            var feeToPay = dynamicFee switch
            {
                < minimumFee => minimumFee,
                > 5f => 5f,
                _ => dynamicFee
            };

            Log($"[Fee]: {Trading.FormatMoney(feeToPay,true)}\n[DynFee]: {Trading.FormatMoney(dynamicFee,true)}\nMoney: {Trading.FormatMoney(MainGame.me.player.data.money,true)}, Minimum: {Trading.FormatMoney(minimumFee,true)}");
            return feeToPay;
        }

        private static void TakeMoney(Vector3 vector)
        {
            vector.y += 125f;
            var feeToPay = GenerateFee();
            MainGame.me.player.data.money -= feeToPay;
            Sounds.PlaySound("coins_sound", vector, true);
            EffectBubblesManager.ShowImmediately(vector, $"-{Trading.FormatMoney(feeToPay, true)}", EffectBubblesManager.BubbleColor.Red, true, 3f);
        }

        private static bool CanUseStone()
        {
            var inDungeon = CrossModFields.IsInDungeon;
            var talkingToNpc = CrossModFields._talkingToNpc;
            var inTutorial = !Tools.TutorialDone() || MainGame.me.save.IsInTutorial();
            var controlled = CrossModFields.PlayerIsControlled;
            if (inDungeon || talkingToNpc || inTutorial || controlled) return false;
            return true;
        }

        [HarmonyPatch(typeof(MultiAnswerGUI), nameof(MultiAnswerGUI.OnChosen))]
        public static class MultiAnswerGuiOnChosenPatch
        {
            [HarmonyPostfix]
            public static void Postfix(string answer)
            {
                //
                if (InTutorial()) return;
                if (!_cfg.EnableListExpansion) return;
                if (!CanUseStone()) return;

                Log($"[Answer]: {answer}");

                if (string.Equals("cancel", answer) && !_dotSelection)
                {
                    //real cancel
                    ShowHud(null,true);
                    _usingStone = false;
                    _dotSelection = false;
                    CrossModFields.TalkingToNpc("BeamMeUpGerry: MultiAnswerGuiOnChosenPatch Postfix: string = cancel, !_dotSelection", false);

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
                    CrossModFields.TalkingToNpc("BeamMeUpGerry: MultiAnswerGuiOnChosenPatch Postfix: string = leave, answer.ToLowerInvariant", false);
                    MainGame.me.player.components.character.control_enabled = true;

                    return;
                }

                //if (CrossModFields.TalkingToNpc) return;

                _usingStone = false;
                _dotSelection = false;

                MainGame.me.player.components.character.control_enabled = true;
            }

            [HarmonyPrefix]
            public static void Prefix(ref string answer)
            {
                //
                if (InTutorial()) return;
                if (!_cfg.EnableListExpansion) return;
                var canUseStone = CanUseStone();
                if (!canUseStone) return;
                // if (_isNpc) return;
                List<AnswerVisualData> answers;

                _dotSelection = false;

                if (answer == "...")
                {
                    // answers = LocationByVectorPartOne.Select(location => new AnswerVisualData() { id = location.Key }).ToList();
                    answers = LocationsPartOne.Select(location => new AnswerVisualData() { id = location.Zone }).ToList();
                    Show(out answer);
                    return;
                }

                if (answer == "....")
                {
                    //answers = LocationByVectorPartTwo.Select(location => new AnswerVisualData() { id = location.Key }).ToList();
                    answers = LocationsPartTwo.Select(location => new AnswerVisualData() { id = location.Zone }).ToList();
                    Show(out answer);
                    return;
                }

                void Show(out string answer)
                {
                    CrossModFields.TalkingToNpc("BeamMeUpGerry: MultiAnswerGuiOnChosenPatch Prefix: void Show", false);
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
                if (InTutorial()) return;
                //
                //  ShowHud();
                if (!_cfg.EnableListExpansion) return;
                if (!CanUseStone()) return;
                if (string.Equals("cancel", chosen))
                {
                    // ShowHud();
                    return;
                }

                if (MainGame.me.player.data.money < GenerateFee())
                {
                    var location = MainGame.me.player_pos;
                    location.x += 125f;
                    location.y += 125f;
                    SpawnGerry(strings.MoreCoin, Vector3.zero);
                    return;
                }

                var partOne = LocationsPartOne.Exists(a => a.Zone == chosen);
                var partTwo = LocationsPartTwo.Exists(a => a.Zone == chosen);

                Vector3 vector;
                Location chosenLocation = null;
                if (partOne)
                {
                    chosenLocation = LocationsPartOne.Find(a => a.Zone == chosen);
                    vector = chosenLocation.Coords;
                }
                else if (partTwo)
                {
                    chosenLocation = LocationsPartTwo.Find(a => a.Zone == chosen);
                    vector = chosenLocation.Coords;
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
                            GJTimer.AddTimer(1.25f, delegate
                            {
                                ShowHud(chosenLocation, true);
                                CameraFader.current.FadeIn(0.15f);
                                GJTimer.AddTimer(0.20f, delegate
                                {
                                    if (_cfg.DisableGerry)
                                    {
                                        TakeMoney(vector);
                                    }
                                    else
                                    {
                                        SpawnGerry("", Vector3.zero, true);
                                    }
                                });
                            });
                        });
                    }
                    else
                    {
                        ShowHud(null,false);
                        MainGame.me.player.PlaceAtPos(vector);
                        MainGame.me.player.components.character.control_enabled = true;
                        if (_cfg.DisableGerry)
                        {
                            TakeMoney(vector);
                        }
                        else
                        {
                            SpawnGerry("", Vector3.zero, true);
                        }
                    }
                    EnvironmentEngine.me.SetEngineGlobalState(EnvironmentEngine.State.Inside);
                }
                else
                {
                    Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                    MainGame.me.player.Say(strings.DontKnow);
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
                 if (InTutorial()) return;
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
                //
                if (InTutorial()) return;
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

        private static string GetLocalizedString(string content)
        {
            Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
            return content;
        }

        private static bool InTutorial()
        {
            return !Tools.TutorialDone() || MainGame.me.save.IsInTutorial();
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
                        ShowHud(null,true);
                        Sounds.OnClosePressed();
                        //_maGui.OnChosen("cancel");
                        //_maGui.OnChosen("leave");
                        _maGui.DestroyBubble();
                        _usingStone = false;
                        _dotSelection = false;
                        CrossModFields.TalkingToNpc("BeamMeUpGerry: TimeOfDayUpdate Prefix: _maGui != null", false);
                        MainGame.me.player.components.character.control_enabled = true;
                        _maGui = null;
                    }
                }

                static void DoLoggingAndBeam()
                {
                    if (InTutorial() && !CrossModFields.PlayerIsControlled)
                    {
                        var item = GetHearthstone();
                        Tools.SpawnGerry(GetLocalizedString(item == null ? strings.InTutorialNoStone : strings.InTutorial), Vector3.zero);
                        return;
                    }
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
    }
}
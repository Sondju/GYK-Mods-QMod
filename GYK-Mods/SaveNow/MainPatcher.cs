using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Harmony;
using UnityEngine;

namespace SaveNow
{
    public class MainPatcher
    {
        public static Vector3 Pos;
        public static string[] Xyz;
        public static float X, Y, Z;
        public static string DataPath, SavePath;
        private static readonly List<SaveSlotData> AllSaveGames = new();
        private static List<SaveSlotData> _sortedTrimmedSaveGames = new();
        private static Timer _aTimer;
        private static bool _canSave;

        private static Config.Options _cfg;

        public static void Patch()
        {
            _cfg = Config.GetOptions();
            _aTimer = new Timer();
            DataPath = "./QMods/SaveNow/dont-remove.dat";
            SavePath = "./QMods/SaveNow/SaveBackup/";
            var val = HarmonyInstance.Create("p1xel8ted.graveyardkeeper.savenow");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        //reads co-ords from player, and saves to file
        public static bool SaveLocation(bool menuExit, string saveFile)
        {
            Pos = MainGame.me.player.pos3;
            var x = Pos.x;
            var y = Pos.y;
            var z = Pos.z;
            string[] xyz =
            {
                x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture),
                z.ToString(CultureInfo.InvariantCulture)
            };
            File.WriteAllLines(DataPath, xyz);

            if (menuExit) return true;
            if (!_cfg.TurnOffSaveGameNotificationText)
            {
                if (!saveFile.Equals(string.Empty))
                {
                    if (_cfg.NewFileOnAutoSave)
                    {
                        EffectBubblesManager.ShowImmediately(Pos, "Auto-Save! : " + saveFile,
                            EffectBubblesManager.BubbleColor.Relation,
                            true, 3f);
                    }
                    else
                    {
                        EffectBubblesManager.ShowImmediately(Pos, "Auto-Save!",
                            EffectBubblesManager.BubbleColor.Relation,
                            true, 3f);
                    }
                }
                else
                {
                    EffectBubblesManager.ShowImmediately(Pos, "Game Saved!", EffectBubblesManager.BubbleColor.Relation,
                        true, 3f);
                }
            }

            return true;
        }

        public static void Resize<T>(List<T> list, int size)
        {
            var count = list.Count;
            if (size < count)
            {
                list.RemoveRange(size, count - size);
            }
        }

        //games already loading, lets add some more work
        [HarmonyPatch(typeof(MainGame), "StartGameLoading")]
        public static class TrimSaveCountBeforeOpening
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                var files = Directory
                    .GetFiles(PlatformSpecific.GetSaveFolder(), "*.info", SearchOption.TopDirectoryOnly)
                    .Select(file => new FileInfo(file)).ToList();
                var sortedFiles = files.OrderByDescending(o => o.CreationTime).ToList();
                Resize(sortedFiles, _cfg.AutoSavesToKeep);

                foreach (var file in Directory.GetFiles(PlatformSpecific.GetSaveFolder(), "*.*",
                             SearchOption.TopDirectoryOnly))
                {
                    if (!File.Exists(file)) continue;
                    var tFile = new FileInfo(file);
                    if (tFile.Name.Contains("backup"))
                    {
                        File.Delete(file);
                        continue;
                    }

                    if (!tFile.Extension.Contains("info") || !tFile.Extension.Contains("dat")) continue;
                    var stringToCompare = Path.GetFileNameWithoutExtension(tFile.FullName).ToLower().Trim();
                    if (sortedFiles.Any(x => string.Equals(Path.GetFileNameWithoutExtension(x.FullName).ToLower(),
                            stringToCompare, StringComparison.CurrentCultureIgnoreCase))) continue;
                    var sDat = Path.Combine(PlatformSpecific.GetSaveFolder(),
                        Path.GetFileNameWithoutExtension(tFile.FullName) + ".dat");
                    var sInfo = Path.Combine(PlatformSpecific.GetSaveFolder(),
                        Path.GetFileNameWithoutExtension(tFile.FullName) + ".info");
                    if (!Directory.Exists(SavePath))
                    {
                        Directory.CreateDirectory(SavePath);
                    }

                    var dDat = SavePath + Path.GetFileNameWithoutExtension(tFile.FullName) + ".dat";
                    var dInfo = SavePath + Path.GetFileNameWithoutExtension(tFile.FullName) + ".info";

                    if (_cfg.RemoveFromSaveListButKeepFile)
                    {
                        File.Move(sDat, dDat);
                        File.Move(sInfo, dInfo);
                    }
                    else
                    {
                        File.Delete(sDat);
                        File.Delete(sInfo);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(SaveSlotsMenuGUI), "RedrawSlots")]
        public static class ReadSaveSlots
        {
            //sorts the save list gui via newest to oldest (newest at top and highlights it). Also trims the list to the
            //amount specified in the config
            [HarmonyPrefix]
            public static void Prefix(ref List<SaveSlotData> slot_datas, ref bool focus_on_first)
            {
                slot_datas.Clear();
                AllSaveGames.Clear();
                _sortedTrimmedSaveGames.Clear();

                //load each save game in the directory
                foreach (var text in Directory.GetFiles(PlatformSpecific.GetSaveFolder(), "*.info",
                             SearchOption.TopDirectoryOnly))
                {
                    var data = SaveSlotData.FromJSON(File.ReadAllText(text));
                    if (data != null) //don't inline. adds games twice
                    {
                        data.filename_no_extension = Path.GetFileNameWithoutExtension(text);
                        AllSaveGames.Add(data);
                    }
                }

                _sortedTrimmedSaveGames = AllSaveGames.OrderByDescending(o => o.game_time).ToList();
                Resize(_sortedTrimmedSaveGames, 5);
                slot_datas = _sortedTrimmedSaveGames;
                focus_on_first = true;
            }
        }


        //reads co-ords from file and teleports player there
        public static void RestoreLocation()
        {
            Xyz = File.ReadAllLines(DataPath);
            X = float.Parse(Xyz[0]);
            Y = float.Parse(Xyz[1]);
            Z = float.Parse(Xyz[2]);
            Pos.Set(X, Y, Z);
            MainGame.me.player.PlaceAtPos(Pos);
            if (!_cfg.TurnOffTravelMessages)
            {
                EffectBubblesManager.ShowImmediately(Pos, "Woooah! What a rush! Gets me every time!",
                    EffectBubblesManager.BubbleColor.Relation, true, 4f);
            }

            _aTimer.AutoReset = true;
            _aTimer.Elapsed += OnTimedEvent;
            _aTimer.Interval = _cfg.SaveInterval;
            _aTimer.Enabled = _cfg.AutoSave;
            _aTimer.Start();
            if (!_cfg.DisableAutoSaveInfo)
            {
                EffectBubblesManager.ShowImmediately(Pos,
                    $"AutoSave: {_cfg.AutoSave}, Period: {_cfg.SaveInterval / 60000} minute(s), New Save on Auto Save: {_cfg.NewFileOnAutoSave}, Saves to keep: {_cfg.AutoSavesToKeep}",
                    EffectBubblesManager.BubbleColor.Red, true, 4f);
            }
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            AutoSave();
        }

        [HarmonyPatch(typeof(InGameMenuGUI), "OnPressedSaveAndExit")]
        public static class PatchSaveAndExit
        {
            public static bool Prefix()
            {
                return false;
            }

            //replaces the standard exit dialog with one that supports save on exit
            public static void Postfix(InGameMenuGUI __instance)
            {
                __instance.SetControllsActive(false);
                __instance.OnClosePressed();
                var messageText = "Are you sure you want to return to the main menu?" + "\n\n" +
                                  "Progress and current location will be saved.";
                if (_cfg.ExitToDesktop)
                {
                    messageText = "Are you sure you want to exit to desktop?" + "\n\n" +
                                  "Progress and current location will be saved.";
                }

                GUIElements.me.dialog.OpenYesNo(messageText
                    ,
                    delegate
                    {
                        if (SaveLocation(true, string.Empty))
                        {
                            PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save, delegate
                            {
                                if (_cfg.ExitToDesktop)
                                {
                                    GC.Collect();
                                    Resources.UnloadUnusedAssets();
                                    Application.Quit();
                                }
                                else
                                {
                                    LoadingGUI.Show(__instance.ReturnToMainMenu);
                                }
                            });
                        }
                    }, null, delegate { __instance.SetControllsActive(true); });
            }
        }

        //if this isn't here, when you sleep, it teleport you back to where the mod saved you last
        [HarmonyPatch(typeof(SleepGUI), "WakeUp")]
        public static class PatchSavePosWhenUsingBed
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                SaveLocation(false, string.Empty);
            }
        }

        //this is called last when loading a save game without patch
        [HarmonyPatch(typeof(GameSave))]
        [HarmonyPatch(nameof(GameSave.GlobalEventsCheck))]
        public static class PatchLoadGame
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                RestoreLocation();
            }
        }

        [HarmonyPatch(typeof(MovementComponent), "UpdateMovement", null)]
        public static class CheckPlayerState
        {
            [HarmonyPostfix]
            public static void Postfix(MovementComponent __instance)
            {
                _canSave = !__instance.player_controlled_by_script;
            }
        }


        //hooks into the time of day update and saves if the K key was pressed
        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.Update))]
        public static class PatchSaveGame
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (Input.GetKeyUp(KeyCode.K))
                {
                    PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                        delegate { SaveLocation(false, string.Empty); });
                }
            }
        }

        public static void AutoSave()
        {
            if (EnvironmentEngine.me.IsTimeStopped()) return;
            if (!Application.isFocused) return;
            if (!_canSave) return;
            if (!_cfg.NewFileOnAutoSave)
            {
                PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                    delegate { SaveLocation(false, MainGame.me.save_slot.filename_no_extension); });
            }
            else
            {
                GUIElements.me.ShowSavingStatus(true);
                var date = DateTime.Now.ToString("ddmmyyhhmmss");
                var newSlot = $"autosave.{date}".Trim();

                MainGame.me.save_slot.filename_no_extension = newSlot;
                PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                    delegate
                    {
                        SaveLocation(false, newSlot);
                        GUIElements.me.ShowSavingStatus(false);
                    });
            }
        }
    }
}
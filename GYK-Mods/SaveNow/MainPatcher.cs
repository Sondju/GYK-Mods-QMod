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
        public static string DataPath, ErrorPath, SavePath;
        private static readonly List<SaveSlotData> AllSaveGames = new();
        private static List<SaveSlotData> _sortedTrimmedSaveGames = new();
        
        private static Config.Options _cfg;

        public static void Patch()
        {
            _cfg = Config.GetOptions();
            var val = HarmonyInstance.Create("p1xel8ted.graveyardkeeper.savenow");
            val.PatchAll(Assembly.GetExecutingAssembly());
            DataPath = "./QMods/SaveNow/dont-remove.dat";
            ErrorPath = "./QMods/SaveNow/error.txt";
            SavePath = "./QMods/SaveNow/saves.txt";
        }

        //reads co-ords from player, and saves to file
        public static bool SaveLocation(bool menuExit)
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
                EffectBubblesManager.ShowImmediately(Pos, "Game Saved!", EffectBubblesManager.BubbleColor.Relation,
                    true, 3f);
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
                string message = null;
                var files = Directory.GetFiles(PlatformSpecific.GetSaveFolder(), "*.info", SearchOption.TopDirectoryOnly).Select(file => new FileInfo(file)).ToList();

                var sortedFiles = files.OrderByDescending(o => o.CreationTime).ToList();
                Resize(sortedFiles, _cfg.AutoSavesToKeep);
                
                foreach (var file in Directory.GetFiles(PlatformSpecific.GetSaveFolder(), "*.info", SearchOption.TopDirectoryOnly))
                {
                    var tFile = new FileInfo(file);
                    var stringToCompare = Path.GetFileNameWithoutExtension(tFile.FullName).ToLower().Trim();
                    if (sortedFiles.Any(x => string.Equals(Path.GetFileNameWithoutExtension(x.FullName).ToLower(),
                            stringToCompare, StringComparison.CurrentCultureIgnoreCase))) continue;
                    var sDat = Path.Combine(PlatformSpecific.GetSaveFolder(), Path.GetFileNameWithoutExtension(tFile.FullName)+ ".dat");
                    var sInfo = Path.Combine(PlatformSpecific.GetSaveFolder(), Path.GetFileNameWithoutExtension(tFile.FullName)+ ".info");
                    var dDat = Path.Combine(PlatformSpecific.GetSaveFolder(), "backup/", Path.GetFileNameWithoutExtension(tFile.FullName) + ".dat");
                    var dInfo = Path.Combine(PlatformSpecific.GetSaveFolder(), "backup/", Path.GetFileNameWithoutExtension(tFile.FullName)+ ".info");
                    message += $"Source dat: {sDat}\n";
                    message += $"Dest data: {dDat}\n";
                    message += $"Source info: {sInfo}\n";
                    message += $"Dest Info: {dInfo}\n";

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

            var aTimer = new Timer();
            aTimer.Elapsed += OnTimedEvent;
            aTimer.Interval = _cfg.SaveInterval;
            aTimer.Enabled = _cfg.AutoSave;
            if (!_cfg.DisableAutoSaveInfo)
            {
                EffectBubblesManager.ShowImmediately(Pos,
                    $"AutoSave: {_cfg.AutoSave}, Period: {_cfg.SaveInterval / 60000} minute(s), New Save on Auto Save: {_cfg.NewFileOnAutoSave}, Saves to keep: {_cfg.AutoSavesToKeep}",
                    EffectBubblesManager.BubbleColor.Red, true, 4f);
            }
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (!_cfg.NewFileOnAutoSave)
            {
                PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                    delegate { SaveLocation(false); });
            }
            else
            {
                AutoSave();
            }
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
                GUIElements.me.dialog.OpenYesNo(
                    "Are you sure you want to exit?" + "\n\n" + "Progress and current location will be saved.",
                    delegate
                    {
                        if (SaveLocation(true))
                        {
                            PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,delegate
                            {
                                LoadingGUI.Show(__instance.ReturnToMainMenu);
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
                SaveLocation(false);
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
                        delegate { SaveLocation(false); });
                }
            }
        }

        public static void AutoSave()
        {
            GUIElements.me.ShowSavingStatus(true);
            var date = DateTime.Now.ToString("ddmmyyhhmmss");
            var newSlot = $"Autosave.{date}".Trim();

            MainGame.me.save_slot.filename_no_extension = newSlot;
            PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                delegate
                {
                    SaveLocation(false);
                    GUIElements.me.ShowSavingStatus(false);
                });
        }
    }
}
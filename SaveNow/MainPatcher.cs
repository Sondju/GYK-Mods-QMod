using HarmonyLib;
using SaveNow.lang;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
using UnityEngine;
using Timer = System.Timers.Timer;

namespace SaveNow;

public class MainPatcher
{
    private static Vector3 _pos;
    private static string _dataPath;
    private static string _savePath;
    private static readonly List<SaveSlotData> AllSaveGames = new();
    private static List<SaveSlotData> _sortedTrimmedSaveGames = new();
    private static Timer _aTimer;
    private static bool _canSave;
    private static string _currentSave;
    private static readonly Dictionary<string, Vector3> SaveLocationsDictionary = new();

    private static Config.Options _cfg;
    private static string Lang { get; set; }

    public static void Patch()
    {
        try
        {
            _cfg = Config.GetOptions();
            _aTimer = new Timer();
            _dataPath = "./QMods/SaveNow/dont-remove.dat";
            _savePath = "./QMods/SaveNow/SaveBackup/";

            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.SaveNow");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            LoadSaveLocations();

            Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveNow]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }

    private static void WriteSavesToFile()
    {
        using var file = new StreamWriter(_dataPath, false);
        foreach (var entry in SaveLocationsDictionary)
        {
            var result = entry.Value.ToString().Substring(1, entry.Value.ToString().Length - 2);
            result = result.Replace(" ", "");
            file.WriteLine("{0}={1}", entry.Key, result);
        }
    }

    private static void LoadSaveLocations()
    {
        if (!File.Exists(_dataPath)) return;

        var lines = File.ReadAllLines(_dataPath, Encoding.Default);
        foreach (var line in lines)
        {
            if (!line.Contains('=')) continue;
            var splitLine = line.Split('=');
            var saveName = splitLine[0];
            var tempVector = splitLine[1].Split(',');
            var vectorToAdd = new Vector3(float.Parse(tempVector[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(tempVector[1].Trim(), CultureInfo.InvariantCulture), float.Parse(tempVector[2].Trim(), CultureInfo.InvariantCulture));

            var found = SaveLocationsDictionary.TryGetValue(saveName, out _);
            // Debug.LogError(Path.Combine(PlatformSpecific.GetSaveFolder(), saveName+".dat"));
            if (!File.Exists(Path.Combine(PlatformSpecific.GetSaveFolder(), saveName + ".dat"))) continue;
            if (!found) SaveLocationsDictionary.Add(saveName, vectorToAdd);
        }
    }

    private static void ShowMessage(string msg, Vector3 pos,
        EffectBubblesManager.BubbleColor color = EffectBubblesManager.BubbleColor.Relation, float time = 3f)
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        //the floaty bubbles are stuck in english apparently??
        if (Lang.Contains("ko") || Lang.Contains("ja") || Lang.Contains("zh"))
        {
            MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                SmartSpeechEngine.VoiceID.None, true);
        }
        else
        {
            var newPos = pos;
            newPos.y += 125f;
            EffectBubblesManager.ShowImmediately(newPos, msg,
                color,
                true, time);
        }
    }

    //reads co-ords from player, and saves to file
    private static bool SaveLocation(bool menuExit, string saveFile)
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);

        _pos = MainGame.me.player.pos3;
        _currentSave = MainGame.me.save_slot.filename_no_extension;

        var overwrite = SaveLocationsDictionary.TryGetValue(_currentSave, out _);
        if (overwrite)
        {
            SaveLocationsDictionary.Remove(_currentSave);
            SaveLocationsDictionary.Add(_currentSave, _pos);
        }
        else
        {
            SaveLocationsDictionary.Add(_currentSave, _pos);
        }

        WriteSavesToFile();

        if (menuExit) return true;
        if (!_cfg.TurnOffSaveGameNotificationText)
        {
            if (!saveFile.Equals(string.Empty))
            {
                if (_cfg.NewFileOnAutoSave)
                    ShowMessage(strings.AutoSave + ": " + saveFile, _pos);
                else
                    ShowMessage(strings.AutoSave + "!", _pos);
            }
            else
            {
                ShowMessage(strings.SaveMessage, _pos);
            }
        }

        return true;
    }

    private static void Resize<T>(List<T> list, int size)
    {
        var count = list.Count;
        if (size < count) list.RemoveRange(size, count - size);
    }

    //reads co-ords from file and teleports player there
    private static void RestoreLocation()
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);

        var homeVector = new Vector3(2841, -6396, -1332);
        var foundLocation =
            SaveLocationsDictionary.TryGetValue(MainGame.me.save_slot.filename_no_extension, out var posVector3);
        var pos = foundLocation ? posVector3 : homeVector;
        MainGame.me.player.PlaceAtPos(pos);
        if (!_cfg.TurnOffTravelMessages) ShowMessage(strings.Rush, pos);

        _aTimer.AutoReset = true;
        _aTimer.Elapsed += OnTimedEvent;
        _aTimer.Interval = _cfg.SaveInterval;
        _aTimer.Enabled = _cfg.AutoSave;
        _aTimer.Start();
        if (!_cfg.DisableAutoSaveInfo)
            ShowMessage(
                $"{strings.InfoAutoSave}: {_cfg.AutoSave}, {strings.InfoPeriod}: {_cfg.SaveInterval / 60000} {strings.InfoMinutes}, {strings.InfoNewSaveOnAutoSave}: {_cfg.NewFileOnAutoSave}, {strings.InfoSavesToKeep}: {_cfg.AutoSavesToKeep}",
                pos, EffectBubblesManager.BubbleColor.Red, 4f);
    }

    private static void OnTimedEvent(object source, ElapsedEventArgs e)
    {
        AutoSave();
    }

    private static void AutoSave()
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

    //games already loading, lets add some more work
    [HarmonyPatch(typeof(MainGame), "StartGameLoading")]
    public static class MainGameStartGameLoadingPatch
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
                if (tFile.Name.Contains("backup")) File.Delete(file);
            }

            foreach (var file in Directory.GetFiles(PlatformSpecific.GetSaveFolder(), "*.info*",
                         SearchOption.TopDirectoryOnly))
            {
                if (!File.Exists(file)) continue;
                var tFile = new FileInfo(file);
                if (!tFile.Extension.Contains("info")) continue;
                var stringToCompare = Path.GetFileNameWithoutExtension(tFile.FullName).ToLower().Trim();
                if (sortedFiles.Any(x => string.Equals(Path.GetFileNameWithoutExtension(x.FullName).ToLower(),
                        stringToCompare, StringComparison.CurrentCultureIgnoreCase))) continue;
                var sDat = Path.Combine(PlatformSpecific.GetSaveFolder(),
                    Path.GetFileNameWithoutExtension(tFile.FullName) + ".dat");
                var sInfo = Path.Combine(PlatformSpecific.GetSaveFolder(),
                    Path.GetFileNameWithoutExtension(tFile.FullName) + ".info");
                if (!Directory.Exists(_savePath)) Directory.CreateDirectory(_savePath);

                var dDat = _savePath + Path.GetFileNameWithoutExtension(tFile.FullName) + ".dat";
                var dInfo = _savePath + Path.GetFileNameWithoutExtension(tFile.FullName) + ".info";

                if (_cfg.RemoveFromSaveListButKeepFile)
                {
                    try
                    {
                        File.Copy(sDat, dDat, true);
                        File.Copy(sInfo, dInfo, true);
                        File.Delete(sDat);
                        File.Delete(sInfo);
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"Error backing up save games. {e.Message}");
                    }
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
    public static class SaveSlotsMenuGuiRedrawSlotsPatch
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
                if (data == null) continue;
                data.filename_no_extension = Path.GetFileNameWithoutExtension(text);
                AllSaveGames.Add(data);
            }

            _sortedTrimmedSaveGames = AllSaveGames.OrderByDescending(o => o.game_time).ToList();
            Resize(_sortedTrimmedSaveGames, _cfg.AutoSavesToKeep);
            slot_datas = _sortedTrimmedSaveGames;
            focus_on_first = true;
        }
    }

    [HarmonyPatch(typeof(InGameMenuGUI), "OnPressedSaveAndExit")]
    public static class InGameMenuGuiOnPressedSaveAndExitPatch
    {
        public static bool Prefix()
        {
            return false;
        }

        //replaces the standard exit dialog with one that supports save on exit
        public static void Postfix(InGameMenuGUI __instance)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);

            __instance.SetControllsActive(false);
            __instance.OnClosePressed();
            var messageText = strings.SaveAreYouSureMenu + "?\n\n" +
                              strings.SaveProgressSaved + ".";
            if (_cfg.ExitToDesktop)
                messageText = strings.SaveAreYouSureDesktop + "?\n\n" +
                              strings.SaveProgressSaved + ".";

            GUIElements.me.dialog.OpenYesNo(messageText
                ,
                delegate
                {
                    if (SaveLocation(true, string.Empty))
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
                }, null, delegate { __instance.SetControllsActive(true); });
        }
    }

    // if this isn't here, when you sleep, it teleport you back to where the mod saved you last
    [HarmonyPatch(typeof(SleepGUI), "WakeUp")]
    public static class SleepGuiWakeUpPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            SaveLocation(false, string.Empty);
        }
    }

    //change exit menu based on config
    [HarmonyPatch(typeof(InGameMenuGUI))]
    [HarmonyPatch(nameof(InGameMenuGUI.Open))]
    public static class PatchInGameMenuGuiOpen
    {
        [HarmonyPostfix]
        public static void Postfix(ref InGameMenuGUI __instance)
        {
            if (__instance == null) return;
            foreach (var comp in __instance.GetComponentsInChildren<UIButton>().Where(x => x.name.Contains("exit")))
                foreach (var label in comp.GetComponentsInChildren<UILabel>())
                    if (_cfg.ExitToDesktop)
                        label.text = strings.ExitButtonText;
        }
    }

    //this is called last when loading a save game without patch
    [HarmonyPatch(typeof(GameSave))]
    [HarmonyPatch(nameof(GameSave.GlobalEventsCheck))]
    public static class GameSaveGlobalEventsCheckPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            RestoreLocation();
        }
    }

    [HarmonyPatch(typeof(MovementComponent), "UpdateMovement", null)]
    public static class MovementComponentUpdateMovementPatch
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
    public static class TimeOfDayUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (Input.GetKeyUp(KeyCode.K))
                PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                    delegate { SaveLocation(false, string.Empty); });
        }
    }
}
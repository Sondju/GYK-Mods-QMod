using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using AutoLootHeavies.lang;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AutoLootHeavies;

public class MainPatcher
{
    private const string VectorPath = "./QMods/AutoLootHeavies/dont-touch.dat";

    private static Config.Options _cfg;

    private static Vector3 _lastKnownTimberLocation;
    private static Vector3 _lastKnownOreLocation;
    private static Vector3 _lastKnownStoneLocation;
    private static float _xAdjustment;
    private static float _lastBubbleTime;

    private static int _timberPileCount;
    private static int _stonePileCount;
    private static int _orePileCount;
    private static int _usedTimberSlots;
    private static int _usedStoneSlots;
    private static int _usedOreSlots;
    private static bool _needScanning = true;

    private static int _freeTimberSlots;
    private static int _freeStoneSlots;
    private static int _freeOreSlots;

    private static List<WorldGameObject> _storedStockpiles = new();

    //public static List<Item> ItemsDidntFit = new();
    private static readonly List<Item> ItemsToInsert = new();

    private static readonly Vector3 BlankVector3 = new(0, 0, 0);

    private static List<WorldGameObject> _objects;
    private static WorldGameObject _timberTemp, _oreTemp, _stoneTemp;

    private static bool _vectorsLoaded;

    private static readonly Dictionary<Vector3, string> VectorDictionary = new();

    private static float _lastScanTime;
    private static float _lastGetLocationScanTime;

    private static string Lang { get; set; }

    public static void Patch()
    {
        try
        {
            _cfg = Config.GetOptions();

            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.AutoLootHeavies");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _vectorsLoaded = false;
            _needScanning = true;

            Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoLootHeavies]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }


    private static void UpdateConfig()
    {
        Config.WriteOptions();
        _cfg = Config.GetOptions();
    }

    private static bool GetLocations()
    {
        if (!File.Exists(VectorPath)) return false;

        var lines = File.ReadAllLines(VectorPath, Encoding.Default);
        foreach (var line in lines)
        {
            var splitLine = line.Split(',');
            var keyToAdd = new Vector3(float.Parse(splitLine[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(splitLine[1].Trim(), CultureInfo.InvariantCulture), float.Parse(splitLine[2].Trim(), CultureInfo.InvariantCulture));
            var valueToAdd = splitLine[3].Trim();
            var found = VectorDictionary.TryGetValue(keyToAdd, out _);
            if (!found) VectorDictionary.Add(keyToAdd, valueToAdd);
        }

        Debug.LogError($"Loaded {VectorDictionary.Count} stockpiles into the dictionary.");
        return true;
    }

    private static void DropOjectAndNull(BaseCharacterComponent __instance, Item item)
    {
        DropResGameObject.Drop(__instance.tf.position, item,
            __instance.tf.parent,
            __instance.anim_direction,
            3f,
            Random.Range(0, 2), false);

        __instance.SetOverheadItem(null);
    }

    private static void ShowLootAddedIcon(Item item)
    {
        item.definition.item_size = 1;
        DropCollectGUI.OnDropCollected(item);
        item.definition.item_size = 2;
        Sounds.PlaySound("pickup", null, true);
    }

    private static bool PutToAllAndNull(BaseCharacterComponent __instance, WorldGameObject wgo,
        List<Item> itemsToInsert)
    {
        var pwo = MainGame.me.player;
        var needEnergy = 1f;
        if (_cfg.DisableImmersionMode) needEnergy = 0f;

        if (pwo.IsPlayerInvulnerable()) needEnergy = 0f;

        if (pwo.energy >= needEnergy)
        {
            pwo.energy -= needEnergy;
            EffectBubblesManager.ShowStackedEnergy(pwo, -needEnergy);

            wgo.PutToAllPossibleInventories(itemsToInsert, out _);
            __instance.SetOverheadItem(null);
            return true;
        }

        if (Time.time - _lastBubbleTime > 0.5f)
        {
            _lastBubbleTime = Time.time;
            EffectBubblesManager.ShowImmediately(pwo.bubble_pos,
                GJL.L("not_enough_something", $"({GameSettings.me.language})"),
                EffectBubblesManager.BubbleColor.Energy, true, 1f);
        }

        return false;
    }

    private static (int, int) GetGridLocation()
    {
        const int horizontal = 30;
        const int vertical = 5;
        var tupleList = new List<(int, int)>();

        if (tupleList.Count <= 0)
        {
            var grid = new int[vertical][];
            for (var x = 0; x < grid.Length; ++x) grid[x] = new int[horizontal];

            for (var x = 0; x < grid.Length; ++x)
            for (var y = 0; y < grid[x].Length; ++y)
            {
                var tu = (x, y);
                if (!tupleList.Contains(tu)) tupleList.Add(tu);
            }
        }

        var spot = tupleList.RandomElement();
        tupleList.Remove(spot);
        return spot;
    }

    private static void TeleportItem(BaseCharacterComponent __instance, Item item, string type)
    {
        var pwo = MainGame.me.player;
        var needEnergy = 3f;
        if (_cfg.DisableImmersionMode) needEnergy = 0f;

        if (pwo.IsPlayerInvulnerable()) needEnergy = 0f;

        if (pwo.energy >= needEnergy)
        {
            pwo.energy -= needEnergy;
            EffectBubblesManager.ShowStackedEnergy(pwo, -needEnergy);

            if (_cfg.DistanceBasedTeleport)
            {
                var location = type switch
                {
                    Constants.FileKeys.Ore => _lastKnownOreLocation,
                    Constants.FileKeys.Timber => _lastKnownTimberLocation,
                    Constants.FileKeys.Stone => _lastKnownStoneLocation,
                    _ => MainGame.me.player_pos
                };

                MainGame.me.player.DropItem(item, Direction.IgnoreDirection, location, 0f, false);
            }
            else
            {
                var loc = GetGridLocation();

                _xAdjustment = loc.Item1 * 75;

                var timber = _cfg.DesignatedTimberLocation;
                var ore = _cfg.DesignatedOreLocation;
                var stone = _cfg.DesignatedStoneLocation;

                timber.x += _xAdjustment;
                ore.x += _xAdjustment;
                stone.x += _xAdjustment;

                var location = type switch
                {
                    Constants.FileKeys.Ore => ore,
                    Constants.FileKeys.Timber => timber,
                    Constants.FileKeys.Stone => stone,
                    _ => MainGame.me.player_pos
                };
                MainGame.me.player.DropItem(item, Direction.IgnoreDirection, location, 0f, false);
            }

            __instance.SetOverheadItem(null);
        }
        else
        {
            DropOjectAndNull(__instance, item);

            if (Time.time - _lastBubbleTime < 0.5f) return;

            _lastBubbleTime = Time.time;

            EffectBubblesManager.ShowImmediately(pwo.bubble_pos,
                GJL.L("not_enough_something", $"({GameSettings.me.language})"),
                EffectBubblesManager.BubbleColor.Energy, true, 1f);
        }
    }

    private static void ShowMessage(string message, bool noStockpilesInRange, bool storageNowFull,
        bool cantStorageFull, string item)
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        var msg = message;
        if (noStockpilesInRange)
            msg = item switch
            {
                Constants.FileKeys.Ore => strings.NoOreInRange,
                Constants.FileKeys.Stone => strings.NoStoneInRange,
                Constants.FileKeys.Timber => strings.NoTimberInRange,
                _ => msg
            };

        if (storageNowFull)
            msg = item switch
            {
                Constants.FileKeys.Ore => strings.OreFull,
                Constants.FileKeys.Stone => strings.StoneFull,
                Constants.FileKeys.Timber => strings.TimberFull,
                _ => msg
            };

        if (cantStorageFull)
            msg = item switch
            {
                Constants.FileKeys.Ore => strings.NoOreStorageLeft,
                Constants.FileKeys.Stone => strings.NoStoneStorageLeft,
                Constants.FileKeys.Timber => strings.NoTimberStorageLeft,
                _ => msg
            };

        MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
            SmartSpeechEngine.VoiceID.None, true);
    }


    private static void ShowMessage(string msg, Vector3 pos)
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        //the floaty bubbles are stuck in english apparently??
        if (Lang.Contains("ko") || Lang.Contains("ja") || Lang.Contains("zh"))
            MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                SmartSpeechEngine.VoiceID.None, true);
        else
            EffectBubblesManager.ShowImmediately(pos,
                msg,
                EffectBubblesManager.BubbleColor.Relation,
                true, 3f);
    }

    private static void GetClosestStockPile()
    {
        if (!MainGame.game_started) return;
        if (VectorDictionary.Count <= 0)
        {
            Debug.LogError("Nothing loaded in the Vector dictionary.");
            return;
        }

        Debug.LogError($"Vector dictionary has {VectorDictionary.Count} vectors.");

        try
        {
            _lastKnownTimberLocation = VectorDictionary.Where(x => x.Value == Constants.FileKeys.Timber)
                .ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key))
                .Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            Debug.Log($"Closest Timber: {_lastKnownTimberLocation}");
        }
        catch (Exception)
        {
            Debug.LogError("No last known timber locations available.");
        }

        try
        {
            _lastKnownOreLocation = VectorDictionary.Where(x => x.Value == Constants.FileKeys.Ore)
                .ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key))
                .Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            Debug.Log($"Closest Timber: {_lastKnownOreLocation}");
        }
        catch (Exception)
        {
            Debug.LogError("No last known ore locations available.");
        }

        try
        {
            _lastKnownStoneLocation = VectorDictionary.Where(x => x.Value == Constants.FileKeys.Stone)
                .ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key))
                .Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            Debug.Log($"Closest Stone: {_lastKnownStoneLocation}");
        }
        catch (Exception)
        {
            Debug.LogError("No last known stone locations available.");
        }
    }

    private static void ScanStockpiles()
    {
        if (_needScanning)
            _needScanning = false;
        else
            _lastScanTime = Time.time;

        _objects = Object.FindObjectsOfType<WorldGameObject>(false).Where(x =>
                x.obj_id.Contains(Constants.ItemObjectId.Timber) | x.obj_id.Contains(Constants.ItemObjectId.Ore) |
                x.obj_id.Contains(Constants.ItemObjectId.Stone))
            .ToList();
        _storedStockpiles = _objects;
        Debug.LogError($"StockPile Count: {_storedStockpiles.Count}, Object count: {_objects.Count}");
        foreach (var obj in _objects.Where(obj => obj != null))
        {
            bool found;
            var vectorToAdd = new Vector3((float) Math.Ceiling(obj.pos3.x), (float) Math.Ceiling(obj.pos3.y),
                (float) Math.Ceiling(obj.pos3.z));
            if (obj.obj_id.Contains(Constants.ItemObjectId.Timber))
            {
                found = VectorDictionary.TryGetValue(vectorToAdd, out _);
                if (!found) VectorDictionary.Add(vectorToAdd, Constants.FileKeys.Timber);
            }
            else if (obj.obj_id.Contains(Constants.ItemObjectId.Ore))
            {
                found = VectorDictionary.TryGetValue(vectorToAdd, out _);
                if (!found) VectorDictionary.Add(vectorToAdd, Constants.FileKeys.Ore);
            }
            else if (obj.obj_id.Contains(Constants.ItemObjectId.Stone))
            {
                found = VectorDictionary.TryGetValue(vectorToAdd, out _);
                if (!found) VectorDictionary.Add(vectorToAdd, Constants.FileKeys.Stone);
            }
        }
    }

    private static void UpdateStockpiles()
    {
        _timberPileCount = 0;
        _stonePileCount = 0;
        _orePileCount = 0;
        _usedTimberSlots = 0;
        _usedStoneSlots = 0;
        _usedOreSlots = 0;

        foreach (var obj in _storedStockpiles.Where(obj => obj != null))
            if (obj.obj_id.Contains(Constants.ItemObjectId.Timber))
            {
                _usedTimberSlots += obj.data.inventory.Count;
                _timberPileCount++;
                _timberTemp = obj;
            }
            else if (obj.obj_id.Contains(Constants.ItemObjectId.Ore))
            {
                _usedOreSlots += obj.data.inventory.Count;
                _orePileCount++;
                _oreTemp = obj;
            }
            else if (obj.obj_id.Contains(Constants.ItemObjectId.Stone))
            {
                _usedStoneSlots += obj.data.inventory.Count;
                _stonePileCount++;
                _stoneTemp = obj;
            }

        _freeTimberSlots = 9 * _timberPileCount - _usedTimberSlots;
        _freeStoneSlots = 6 * _stonePileCount - _usedStoneSlots;
        _freeOreSlots = 7 * _orePileCount - _usedOreSlots;
        //  ShowMessage($"Free Timber: {FreeTimberSlots}, Free Stone: {FreeStoneSlots}, Free Ore: {FreeOreSlots}", false,false,false,"");
    }

    private struct Constants
    {
        public struct FileKeys
        {
            public const string Timber = "t";
            public const string Stone = "s";
            public const string Ore = "o";
        }

        public struct ItemDefinitionId
        {
            public const string Wood = "wood";
            public const string Stone = "stone";
            public const string Marble = "marble";
            public const string Ore = "ore_metal";
        }

        public struct ItemObjectId
        {
            public const string Timber = "mf_timber_1";
            public const string Stone = "mf_stones_1";
            public const string Ore = "mf_ore_1";
        }
    }


    [HarmonyPatch(typeof(InGameMenuGUI), nameof(InGameMenuGUI.OnClosePressed))]
    public static class InGameMenuGuiOnClosePressedPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        }
    }

    [HarmonyPatch(typeof(MainGame), nameof(MainGame.Update))]
    public class MainGameUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!_cfg.DistanceBasedTeleport) return;
            if (Time.time - _lastGetLocationScanTime < 10f) return;

            GetClosestStockPile();
            _lastGetLocationScanTime = Time.time;
        }
    }

    //hooks into the time of day update and saves if the K key was pressed
    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
    public static class TimeOfDayUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (Input.GetKeyUp(KeyCode.Alpha5))
            {
                if (!_cfg.Teleportation)
                {
                    _cfg.Teleportation = true;
                    ShowMessage(strings.TeleOn, MainGame.me.player_pos);
                    UpdateConfig();
                    GetLocations();
                    typeof(MainGame).GetMethod(nameof(MainGame.Update))?.Invoke(null, null);
                }
                else
                {
                    _cfg.Teleportation = false;
                    ShowMessage(strings.TeleOff, MainGame.me.player_pos);
                    UpdateConfig();
                }

                UpdateConfig();
            }

            if (Input.GetKeyUp(KeyCode.Alpha6))
            {
                if (!_cfg.DistanceBasedTeleport)
                {
                    if (!_cfg.Teleportation)
                    {
                        _cfg.Teleportation = true;
                        ShowMessage(strings.TeleOn, MainGame.me.player_pos);
                        UpdateConfig();
                    }

                    _cfg.DistanceBasedTeleport = true;
                    ShowMessage(strings.DistTeleOn, MainGame.me.player_pos);
                    UpdateConfig();
                    GetLocations();
                }
                else
                {
                    _cfg.DistanceBasedTeleport = false;
                    ShowMessage(strings.DistTeleOff, MainGame.me.player_pos);
                    UpdateConfig();
                }

                UpdateConfig();
            }

            if (Input.GetKeyUp(KeyCode.Alpha7))
            {
                _cfg.DesignatedTimberLocation = MainGame.me.player_pos;
                ShowMessage(strings.DumpTimber, _cfg.DesignatedTimberLocation);
                UpdateConfig();
            }

            if (Input.GetKeyUp(KeyCode.Alpha8))
            {
                _cfg.DesignatedOreLocation = MainGame.me.player_pos;
                ShowMessage(strings.DumpOre, _cfg.DesignatedOreLocation);
                UpdateConfig();
            }

            if (Input.GetKeyUp(KeyCode.Alpha9))
            {
                _cfg.DesignatedStoneLocation = MainGame.me.player_pos;
                ShowMessage(strings.DumpStone, _cfg.DesignatedStoneLocation);
                UpdateConfig();
            }
        }
    }

    [HarmonyPatch(typeof(MovementComponent), "UpdateMovement")]
    public static class MovementComponentUpdateMovementPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (MainGame.game_started)
            {
                if (_needScanning)
                {
                    ScanStockpiles();
                    UpdateStockpiles();
                }

                if (!_vectorsLoaded)
                    if (GetLocations())
                    {
                        GetClosestStockPile();
                        _vectorsLoaded = true;
                    }
            }

            return true;
        }
    }


    [HarmonyPatch(typeof(BaseCharacterComponent), nameof(BaseCharacterComponent.SetOverheadItem))]
    public class BaseCharacterComponentSetOverheadItemPatch
    {
        //this was prefix
        [HarmonyPostfix]
        public static void Postfix()
        {
            UpdateStockpiles();

            if (Time.time - _lastScanTime < _cfg.ScanIntervalInSeconds)
                // Debug.LogError($"Been less than {_cfg.ScanIntervalInSeconds} seconds since last scan. Skipping.");
                return;

            ScanStockpiles();
        }
    }

    [HarmonyPatch(typeof(PlatformSpecific), nameof(PlatformSpecific.SaveGame))]
    public class PlatformSpecificSaveGamePatch
    {
        //save co-ords as the game saves
        [HarmonyPrefix]
        public static void Prefix()
        {
            using var file = new StreamWriter(VectorPath, false);
            foreach (var entry in VectorDictionary)
            {
                var result = entry.Key.ToString().Substring(1, entry.Key.ToString().Length - 2);
                result = result.Replace(" ", "");
                file.WriteLine("{0},{1}", result, entry.Value);
            }
        }
    }


    [HarmonyPatch(typeof(BaseCharacterComponent), nameof(BaseCharacterComponent.DropOverheadItem))]
    public class BaseCharacterComponentDropOverheadItemPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref Item ___overhead_item,
            out (bool wood, bool stone, bool iron, bool runCode) __state)
        {
            var itemIdentifier = ___overhead_item.definition.id;

            var itemIsLog = itemIdentifier.ToLower().Contains(Constants.ItemDefinitionId.Wood);
            var itemIsStone = itemIdentifier.Contains(Constants.ItemDefinitionId.Stone) ||
                              itemIdentifier.Contains(Constants.ItemDefinitionId.Marble);
            var itemIsOre = itemIdentifier.Contains(Constants.ItemDefinitionId.Ore);

            if (itemIsLog || itemIsStone || itemIsOre)
            {
                __state = (itemIsLog, itemIsStone, itemIsOre, true);

                return false;
            }

            __state = (false, false, false, false);

            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(BaseCharacterComponent __instance, ref Item ___overhead_item,
            (bool wood, bool stone, bool iron, bool runCode) __state)
        {
            if (!__state.runCode) return;
            try
            {
                var isAttacking = __instance.playing_animation | __instance.playing_work_animation |
                                  __instance.attack.performing_attack;


                var pWgo = MainGame.me.player;
                var tWgo = _timberTemp; //timber
                var sWgo = _stoneTemp; //stone
                var oWgo = _oreTemp; //iron ore

                bool success;

                var item = ___overhead_item;
                if (item == null) return;
                if (isAttacking)
                {
                    DropOjectAndNull(__instance, item);
                    return;
                }


                ItemsToInsert.Clear();
                //ItemsDidntFit.Clear();
                ItemsToInsert.Add(item);

                if (__state.iron)
                {
                    if (oWgo == null)
                    {
                        if (_lastKnownOreLocation == BlankVector3)
                        {
                            DropOjectAndNull(__instance, item);
                            ShowMessage("", true, false, false, Constants.FileKeys.Ore);
                        }
                        else
                        {
                            if (_cfg.Teleportation)
                            {
                                var distance = Vector3.Distance(_cfg.DesignatedOreLocation, pWgo.pos3);
                                if (distance <= 750f)
                                    DropOjectAndNull(__instance, item);
                                else
                                    TeleportItem(__instance, item, Constants.FileKeys.Ore);
                            }
                            else
                            {
                                DropOjectAndNull(__instance, item);
                                ShowMessage("", true, false, false, Constants.FileKeys.Ore);
                            }
                        }
                    }
                    else
                    {
                        switch (_freeOreSlots)
                        {
                            //not sure how it can get to -1, but here because it did
                            case < 0:

                                if (_cfg.Teleportation)
                                {
                                    var distance = Vector3.Distance(_cfg.DesignatedOreLocation, pWgo.pos3);
                                    if (distance <= 750f)
                                        DropOjectAndNull(__instance, item);
                                    else
                                        TeleportItem(__instance, item, Constants.FileKeys.Ore);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                }

                                break;

                            case 0:

                                if (_cfg.Teleportation)
                                {
                                    var distance = Vector3.Distance(_cfg.DesignatedOreLocation, pWgo.pos3);
                                    if (distance <= 750f)
                                        DropOjectAndNull(__instance, item);
                                    else
                                        TeleportItem(__instance, item, Constants.FileKeys.Ore);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                    //ShowMessage("", false, true, true, Constants.FileKeys.Ores);
                                }

                                break;

                            case 1:

                                success = PutToAllAndNull(__instance, oWgo, ItemsToInsert);
                                if (success)
                                {
                                    ShowLootAddedIcon(item);
                                    ShowMessage("", false, true, false, Constants.FileKeys.Ore);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                }

                                break;

                            case > 0:

                                success = PutToAllAndNull(__instance, oWgo, ItemsToInsert);
                                if (success)
                                    ShowLootAddedIcon(item);
                                else
                                    DropOjectAndNull(__instance, item);

                                //if (ItemsDidntFit.Count > 0)
                                //{
                                //    ShowMessage("", false, false, true, Constants.FileKeys.Ore);
                                //}

                                break;
                        }
                    }
                }
                else if (__state.wood)
                {
                    if (tWgo == null)
                    {
                        if (_lastKnownTimberLocation == BlankVector3)
                        {
                            DropOjectAndNull(__instance, item);
                            ShowMessage("", true, false, false, Constants.FileKeys.Timber);
                        }
                        else
                        {
                            if (_cfg.Teleportation)
                            {
                                var distance = Vector3.Distance(_cfg.DesignatedTimberLocation, pWgo.pos3);
                                if (distance <= 750f)
                                    DropOjectAndNull(__instance, item);
                                else
                                    TeleportItem(__instance, item, Constants.FileKeys.Timber);
                            }
                            else
                            {
                                DropOjectAndNull(__instance, item);
                                ShowMessage("", true, false, false, Constants.FileKeys.Timber);
                            }
                        }
                    }
                    else
                    {
                        switch (_freeTimberSlots)
                        {
                            //not sure how it can get to -1, but here because it did
                            case < 0:

                                if (_cfg.Teleportation)
                                {
                                    var distance = Vector3.Distance(_cfg.DesignatedTimberLocation, pWgo.pos3);
                                    if (distance <= 750f)
                                        DropOjectAndNull(__instance, item);
                                    else
                                        TeleportItem(__instance, item, Constants.FileKeys.Timber);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                }

                                break;

                            case 0:

                                if (_cfg.Teleportation)
                                {
                                    var distance = Vector3.Distance(_cfg.DesignatedTimberLocation, pWgo.pos3);
                                    if (distance <= 750f)
                                        DropOjectAndNull(__instance, item);
                                    else
                                        TeleportItem(__instance, item, Constants.FileKeys.Timber);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                }

                                break;

                            case 1:

                                success = PutToAllAndNull(__instance, tWgo, ItemsToInsert);
                                if (success)
                                {
                                    ShowLootAddedIcon(item);
                                    ShowMessage("", false, true, false, Constants.FileKeys.Timber);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                }

                                break;

                            case > 0:

                                success = PutToAllAndNull(__instance, tWgo, ItemsToInsert);
                                if (success)
                                    ShowLootAddedIcon(item);
                                else
                                    DropOjectAndNull(__instance, item);

                                //if (ItemsDidntFit.Count > 0)
                                //{
                                //    ShowMessage("", false, false, true, Constants.FileKeys.Timber);
                                //}

                                break;
                        }
                    }
                }
                else if (__state.stone)
                {
                    if (sWgo == null)
                    {
                        if (_lastKnownStoneLocation == BlankVector3)
                        {
                            DropOjectAndNull(__instance, item);
                            ShowMessage("", true, false, false, Constants.FileKeys.Stone);
                        }
                        else
                        {
                            if (_cfg.Teleportation)
                            {
                                var distance = Vector3.Distance(_cfg.DesignatedStoneLocation, pWgo.pos3);
                                if (distance <= 750f)
                                    DropOjectAndNull(__instance, item);
                                else
                                    TeleportItem(__instance, item, Constants.FileKeys.Stone);
                            }
                            else
                            {
                                DropOjectAndNull(__instance, item);
                                ShowMessage("", true, false, false, Constants.FileKeys.Stone);
                            }
                        }
                    }
                    else
                    {
                        switch (_freeStoneSlots)
                        {
                            //not sure how it can get to -1, but here because it did
                            case < 0:

                                if (_cfg.Teleportation)
                                {
                                    var distance = Vector3.Distance(_cfg.DesignatedStoneLocation, pWgo.pos3);
                                    if (distance <= 750f)
                                        DropOjectAndNull(__instance, item);
                                    else
                                        TeleportItem(__instance, item, Constants.FileKeys.Stone);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                    // ShowMessage("", false, false, true, Constants.FileKeys.Stone);
                                }

                                break;

                            case 0:

                                if (_cfg.Teleportation)
                                {
                                    var distance = Vector3.Distance(_cfg.DesignatedStoneLocation, pWgo.pos3);
                                    if (distance <= 750f)
                                        DropOjectAndNull(__instance, item);
                                    else
                                        TeleportItem(__instance, item, Constants.FileKeys.Stone);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                    // ShowMessage("", false, false, true, Constants.FileKeys.Stone);
                                }

                                break;

                            case 1:

                                success = PutToAllAndNull(__instance, sWgo, ItemsToInsert);
                                if (success)
                                {
                                    ShowLootAddedIcon(item);
                                    ShowMessage("", false, true, false, Constants.FileKeys.Stone);
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                }

                                break;

                            case > 0:

                                success = PutToAllAndNull(__instance, sWgo, ItemsToInsert);
                                if (success)
                                    ShowLootAddedIcon(item);
                                else
                                    DropOjectAndNull(__instance, item);

                                //if (ItemsDidntFit.Count > 0)
                                //{
                                //    ShowMessage("", false, false, true, Constants.FileKeys.Stone);
                                //}

                                break;
                        }
                    }
                }
                else
                {
                    //shouldn't need this, but just in case
                    DropOjectAndNull(__instance, item);
                }
            }
            catch (Exception ex)
            {
                ShowMessage(
                    strings.ErrorMsg,
                    false,
                    false, false, "");
                try
                {
                    File.WriteAllText("./QMods/AutoLootHeavies/error.txt",
                        $@"Mod: AutoLootHeavies, Message: {ex.Message}, Source: {ex.Source}, Trace: {ex.StackTrace}
");
                }
                catch (Exception)
                {
                    //
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using AutoLootHeavies.lang;
using HarmonyLib;
using Object = UnityEngine.Object;

namespace AutoLootHeavies
{
    public class MainPatcher
    {
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

        private static Config.Options _cfg;

        public static Vector3 LastKnownTimberLocation;
        public static Vector3 LastKnownOreLocation;
        public static Vector3 LastKnownStoneLocation;
        public static float XAdjustment = 0;
        public static float LastBubbleTime = 0;

        public static int TimberPileCount;
        public static int StonePileCount;
        public static int OrePileCount;
        public static int UsedTimberSlots;
        public static int UsedStoneSlots;
        public static int UsedOreSlots;
        public static bool NeedScanning = true;

        public static int FreeTimberSlots;
        public static int FreeStoneSlots;
        public static int FreeOreSlots;

        public static List<WorldGameObject> StoredStockpiles = new();

        public static List<Item> ItemsDidntFit = new();
        public static List<Item> ItemsToInsert = new();

        public static Vector3 BlankVector3 = new(0, 0, 0);

        public static List<WorldGameObject> Objects;
        public static WorldGameObject TimberTemp, OreTemp, StoneTemp;

        public static readonly string VectorPath = "./QMods/AutoLootHeavies/dont-touch.dat";

        public static bool VectorsLoaded;

        private static readonly Dictionary<Vector3, string> VectorDictionary = new();

        public static float LastScanTime;
        public static float LastGetLocationScanTime;

        public static void Patch()
        {
            _cfg = Config.GetOptions();

            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.AutoLootHeavies");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);

            VectorsLoaded = false;
            NeedScanning = true;
        }

        public static void UpdateConfig()
        {
            Config.WriteOptions();
            _cfg = Config.GetOptions();
        }

        public static bool GetLocations()
        {
            if (!File.Exists(VectorPath)) return false;

            var lines = File.ReadAllLines(VectorPath, Encoding.Default);
            foreach (var line in lines)
            {
                var splitLine = line.Split(',');
                var keyToAdd = new Vector3(float.Parse(splitLine[0].Trim()),
                    float.Parse(splitLine[1].Trim()), float.Parse(splitLine[2].Trim()));
                var valueToAdd = splitLine[3].Trim();
                var found = VectorDictionary.TryGetValue(keyToAdd, out _);
                if (!found)
                {
                    VectorDictionary.Add(keyToAdd, valueToAdd);
                }
            }

            Debug.LogError($"Loaded {VectorDictionary.Count} stockpiles into the dictionary.");
            return true;
        }

        public static void DropOjectAndNull(BaseCharacterComponent __instance, Item item)
        {
            DropResGameObject.Drop(__instance.tf.position, item,
                __instance.tf.parent,
                __instance.anim_direction,
                3f,
                UnityEngine.Random.Range(0, 2), false);

            __instance.SetOverheadItem(null);
        }

        public static void ShowLootAddedIcon(Item item)
        {
            item.definition.item_size = 1;
            DropCollectGUI.OnDropCollected(item);
            item.definition.item_size = 2;
            Sounds.PlaySound("pickup", null, true);
        }

        public static bool PutToAllAndNull(BaseCharacterComponent __instance, WorldGameObject wgo,
            List<Item> itemsToInsert)
        {
            var pwo = MainGame.me.player;
            var needEnergy = 1f;
            if (_cfg.DisableImmersionMode)
            {
                needEnergy = 0f;
            }
            if (pwo.IsPlayerInvulnerable())
            {
                needEnergy = 0f;
            }

            if (pwo.energy >= needEnergy)
            {
                pwo.energy -= needEnergy;
                EffectBubblesManager.ShowStackedEnergy(pwo, -needEnergy);

                wgo.PutToAllPossibleInventories(itemsToInsert, out _);
                __instance.SetOverheadItem(null);
                return true;
            }

            if (Time.time - LastBubbleTime > 0.5f)
            {
                LastBubbleTime = Time.time;
                EffectBubblesManager.ShowImmediately(pwo.bubble_pos, GJL.L("not_enough_something", $"({GameSettings.me.language})"),
                    EffectBubblesManager.BubbleColor.Energy, true, 1f);
            }

            return false;
        }

        public static (int, int) GetGridLocation()
        {
            const int horizontal = 30;
            const int vertical = 5;
            var tupleList = new List<(int, int)>();

            if (tupleList.Count <= 0)
            {
                var grid = new int[vertical][];
                for (var x = 0; x < grid.Length; ++x)
                {
                    grid[x] = new int[horizontal];
                }

                for (var x = 0; x < grid.Length; ++x)
                {
                    for (var y = 0; y < grid[x].Length; ++y)
                    {
                        var tu = (x, y);
                        if (!tupleList.Contains(tu))
                        {
                            tupleList.Add(tu);
                        }
                    }
                }
            }

            var spot = tupleList.RandomElement();
            tupleList.Remove(spot);
            return spot;
        }

        public static void TeleportItem(BaseCharacterComponent __instance, Item item, string type)
        {
            var pwo = MainGame.me.player;
            var needEnergy = 3f;
            if (_cfg.DisableImmersionMode)
            {
                needEnergy = 0f;
            }

            if (pwo.IsPlayerInvulnerable())
            {
                needEnergy = 0f;
            }

            if (pwo.energy >= needEnergy)
            {
                pwo.energy -= needEnergy;
                EffectBubblesManager.ShowStackedEnergy(pwo, -needEnergy);

                if (_cfg.DistanceBasedTeleport)
                {
                    var location = type switch
                    {
                        Constants.FileKeys.Ore => LastKnownOreLocation,
                        Constants.FileKeys.Timber => LastKnownTimberLocation,
                        Constants.FileKeys.Stone => LastKnownStoneLocation,
                        _ => MainGame.me.player_pos
                    };

                    MainGame.me.player.DropItem(item, Direction.IgnoreDirection, location, 0f, false);
                }
                else
                {
                    var loc = GetGridLocation();

                    XAdjustment = loc.Item1 * 75;

                    var timber = _cfg.DesignatedTimberLocation;
                    var ore = _cfg.DesignatedOreLocation;
                    var stone = _cfg.DesignatedStoneLocation;

                    timber.x += XAdjustment;
                    ore.x += XAdjustment;
                    stone.x += XAdjustment;

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

                if (Time.time - LastBubbleTime < 0.5f)
                {
                    return;
                }

                LastBubbleTime = Time.time;

                EffectBubblesManager.ShowImmediately(pwo.bubble_pos, GJL.L("not_enough_something", $"({GameSettings.me.language})"),
                    EffectBubblesManager.BubbleColor.Energy, true, 1f);
            }
        }

        public static void ShowMessage(string message, bool noStockpilesInRange, bool storageNowFull,
            bool cantStorageFull, string item)
        {
            var lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
            var msg = message;
            if (noStockpilesInRange)
            {
                msg = item switch
                {
                    Constants.FileKeys.Ore => strings.NoOreInRange,
                    Constants.FileKeys.Stone => strings.NoStoneInRange,
                    Constants.FileKeys.Timber => strings.NoTimberInRange,
                    _ => msg
                };
            }

            if (storageNowFull)
            {
                msg = item switch
                {
                    Constants.FileKeys.Ore => strings.OreFull,
                    Constants.FileKeys.Stone => strings.StoneFull,
                    Constants.FileKeys.Timber => strings.TimberFull,
                    _ => msg
                };
            }

            if (cantStorageFull)
            {
                msg = item switch
                {
                    Constants.FileKeys.Ore => strings.NoOreStorageLeft,
                    Constants.FileKeys.Stone => strings.NoStoneStorageLeft,
                    Constants.FileKeys.Timber => strings.NoTimberStorageLeft,
                    _ => msg
                };
            }

            MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                SmartSpeechEngine.VoiceID.None, true);
        }


        public static void ShowMessage(string msg, Vector3 pos)
        {
            var lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
            //the floaty bubbles are stuck in english apparently??
            if (lang.Contains("ko") || lang.Contains("ja") || lang.Contains("zh"))
            {

                MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                    SmartSpeechEngine.VoiceID.None, true);
            }
            else
            {
                EffectBubblesManager.ShowImmediately(pos,
                    msg,
                    EffectBubblesManager.BubbleColor.Relation,
                    true, 3f);
            }
        }

        public static void GetClosestStockPile()
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
                LastKnownTimberLocation = VectorDictionary.Where(x => x.Value == Constants.FileKeys.Timber)
                    .ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key))
                    .Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                Debug.Log($"Closest Timber: {LastKnownTimberLocation}");
            }
            catch (Exception)
            {
                Debug.LogError("No last known timber locations available.");
            }

            try
            {
                LastKnownOreLocation = VectorDictionary.Where(x => x.Value == Constants.FileKeys.Ore)
                    .ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key))
                    .Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                Debug.Log($"Closest Timber: {LastKnownOreLocation}");
            }
            catch (Exception)
            {
                Debug.LogError("No last known ore locations available.");
            }

            try
            {
                LastKnownStoneLocation = VectorDictionary.Where(x => x.Value == Constants.FileKeys.Stone)
                    .ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key))
                    .Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                Debug.Log($"Closest Stone: {LastKnownStoneLocation}");
            }
            catch (Exception)
            {
                Debug.LogError("No last known stone locations available.");
            }
        }

        [HarmonyPatch(typeof(MainGame))]
        [HarmonyPatch(nameof(MainGame.Update))]
        public class UpdateStockPiles
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!_cfg.DistanceBasedTeleport) return;
                if (Time.time - LastGetLocationScanTime < 10f)
                {
                    return;
                }

                GetClosestStockPile();
                LastGetLocationScanTime = Time.time;
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
        public static class LoadLocations
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                if (MainGame.game_started)
                {
                    if (NeedScanning)
                    {
                        ScanStockpiles();
                        UpdateStockpiles();
                    }

                    if (!VectorsLoaded)
                    {
                        if (GetLocations())
                        {
                            GetClosestStockPile();
                            VectorsLoaded = true;
                        }
                    }
                }

                return true;
            }
        }

        public static void ScanStockpiles()
        {
            if (NeedScanning)
            {
                NeedScanning = false;
            }
            else
            {
                LastScanTime = Time.time;
            }

            Objects = Object.FindObjectsOfType<WorldGameObject>(includeInactive: false).Where(x =>
                    x.obj_id.Contains(Constants.ItemObjectId.Timber) | x.obj_id.Contains(Constants.ItemObjectId.Ore) |
                    x.obj_id.Contains(Constants.ItemObjectId.Stone))
                .ToList();
            StoredStockpiles = Objects;
            Debug.LogError($"StockPile Count: {StoredStockpiles.Count}, Object count: {Objects.Count}");
            foreach (var obj in Objects.Where(obj => obj != null))
            {
                bool found;
                var vectorToAdd = new Vector3((float)Math.Ceiling(obj.pos3.x), (float)Math.Ceiling(obj.pos3.y),
                    (float)Math.Ceiling(obj.pos3.z));
                if (obj.obj_id.Contains(Constants.ItemObjectId.Timber))
                {
                    found = VectorDictionary.TryGetValue(vectorToAdd, out _);
                    if (!found)
                    {
                        VectorDictionary.Add(vectorToAdd, Constants.FileKeys.Timber);
                    }
                }
                else if (obj.obj_id.Contains(Constants.ItemObjectId.Ore))
                {
                    found = VectorDictionary.TryGetValue(vectorToAdd, out _);
                    if (!found)
                    {
                        VectorDictionary.Add(vectorToAdd, Constants.FileKeys.Ore);
                    }
                }
                else if (obj.obj_id.Contains(Constants.ItemObjectId.Stone))
                {
                    found = VectorDictionary.TryGetValue(vectorToAdd, out _);
                    if (!found)
                    {
                        VectorDictionary.Add(vectorToAdd, Constants.FileKeys.Stone);
                    }
                }
            }
        }

        public static void UpdateStockpiles()
        {
            TimberPileCount = 0;
            StonePileCount = 0;
            OrePileCount = 0;
            UsedTimberSlots = 0;
            UsedStoneSlots = 0;
            UsedOreSlots = 0;

            foreach (var obj in StoredStockpiles.Where(obj => obj != null))
            {
                if (obj.obj_id.Contains(Constants.ItemObjectId.Timber))
                {
                    UsedTimberSlots += obj.data.inventory.Count;
                    TimberPileCount++;
                    TimberTemp = obj;
                }
                else if (obj.obj_id.Contains(Constants.ItemObjectId.Ore))
                {
                    UsedOreSlots += obj.data.inventory.Count;
                    OrePileCount++;
                    OreTemp = obj;
                }
                else if (obj.obj_id.Contains(Constants.ItemObjectId.Stone))
                {
                    UsedStoneSlots += obj.data.inventory.Count;
                    StonePileCount++;
                    StoneTemp = obj;
                }

            }

            FreeTimberSlots = (9 * TimberPileCount) - UsedTimberSlots;
            FreeStoneSlots = (6 * StonePileCount) - UsedStoneSlots;
            FreeOreSlots = (7 * OrePileCount) - UsedOreSlots;
          //  ShowMessage($"Free Timber: {FreeTimberSlots}, Free Stone: {FreeStoneSlots}, Free Ore: {FreeOreSlots}", false,false,false,"");
        }


        [HarmonyPatch(typeof(BaseCharacterComponent))]
        [HarmonyPatch(nameof(BaseCharacterComponent.SetOverheadItem))]
        public class SetPatching
        {
            //this was prefix
            [HarmonyPostfix]
            public static void Postfix()
            {
                UpdateStockpiles();

                if (Time.time - LastScanTime < _cfg.ScanIntervalInSeconds)
                {
                    // Debug.LogError($"Been less than {_cfg.ScanIntervalInSeconds} seconds since last scan. Skipping.");
                    return;
                }

                ScanStockpiles();
            }
        }

        [HarmonyPatch(typeof(PlatformSpecific))]
        [HarmonyPatch(nameof(PlatformSpecific.SaveGame))]
        public class PreSaveStockpileSave
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


        [HarmonyPatch(typeof(BaseCharacterComponent))]
        [HarmonyPatch(nameof(BaseCharacterComponent.DropOverheadItem))]
        public class DropPatching
        {
            [HarmonyPrefix]
            public static bool Prefix(ref Item ___overhead_item,
                out (bool wood, bool stone, bool iron, bool runCode) __state)
            {
                string itemIdentifier = ___overhead_item.definition.id;

                bool itemIsLog = itemIdentifier.ToLower().Contains(Constants.ItemDefinitionId.Wood);
                bool itemIsStone = itemIdentifier.Contains(Constants.ItemDefinitionId.Stone) || itemIdentifier.Contains(Constants.ItemDefinitionId.Marble);
                bool itemIsOre = itemIdentifier.Contains(Constants.ItemDefinitionId.Ore);

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
                    var tWgo = TimberTemp; //timber
                    var sWgo = StoneTemp; //stone
                    var oWgo = OreTemp; //iron ore

                    bool success;

                    var item = ___overhead_item;
                    if (item == null) return;
                    if (isAttacking)
                    {
                        DropOjectAndNull(__instance, item);
                        return;
                    }


                    ItemsToInsert.Clear();
                    ItemsDidntFit.Clear();
                    ItemsToInsert.Add(item);

                    if (__state.iron)
                    {
                        if (oWgo == null)
                        {
                            if (LastKnownOreLocation == BlankVector3)
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
                                    {
                                        DropOjectAndNull(__instance, item);
                                    }
                                    else
                                    {
                                        TeleportItem(__instance, item, Constants.FileKeys.Ore);
                                    }
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
                            switch (FreeOreSlots)
                            {
                                //not sure how it can get to -1, but here because it did
                                case < 0:

                                    if (_cfg.Teleportation)
                                    {
                                        var distance = Vector3.Distance(_cfg.DesignatedOreLocation, pWgo.pos3);
                                        if (distance <= 750f)
                                        {
                                            DropOjectAndNull(__instance, item);
                                        }
                                        else
                                        {
                                            TeleportItem(__instance, item, Constants.FileKeys.Ore);
                                        }
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
                                        {
                                            DropOjectAndNull(__instance, item);
                                        }
                                        else
                                        {
                                            TeleportItem(__instance, item, Constants.FileKeys.Ore);
                                        }
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
                                    {
                                        ShowLootAddedIcon(item);
                                    }
                                    else
                                    {
                                        DropOjectAndNull(__instance, item);
                                    }

                                    if (ItemsDidntFit.Count > 0)
                                    {
                                        ShowMessage("", false, false, true, Constants.FileKeys.Ore);
                                    }

                                    break;
                            }
                        }
                    }
                    else if (__state.wood)
                    {
                        if (tWgo == null)
                        {
                            if (LastKnownTimberLocation == BlankVector3)
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
                                    {
                                        DropOjectAndNull(__instance, item);
                                    }
                                    else
                                    {
                                        TeleportItem(__instance, item, Constants.FileKeys.Timber);
                                    }
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
                            switch (FreeTimberSlots)
                            {
                                //not sure how it can get to -1, but here because it did
                                case < 0:

                                    if (_cfg.Teleportation)
                                    {
                                        var distance = Vector3.Distance(_cfg.DesignatedTimberLocation, pWgo.pos3);
                                        if (distance <= 750f)
                                        {
                                            DropOjectAndNull(__instance, item);
                                        }
                                        else
                                        {
                                            TeleportItem(__instance, item, Constants.FileKeys.Timber);
                                        }
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
                                        {
                                            DropOjectAndNull(__instance, item);
                                        }
                                        else
                                        {
                                            TeleportItem(__instance, item, Constants.FileKeys.Timber);
                                        }
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
                                    {
                                        ShowLootAddedIcon(item);
                                    }
                                    else
                                    {
                                        DropOjectAndNull(__instance, item);
                                    }

                                    if (ItemsDidntFit.Count > 0)
                                    {
                                        ShowMessage("", false, false, true, Constants.FileKeys.Timber);
                                    }

                                    break;
                            }
                        }
                    }
                    else if (__state.stone)
                    {
                        if (sWgo == null)
                        {
                            if (LastKnownStoneLocation == BlankVector3)
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
                                    {
                                        DropOjectAndNull(__instance, item);
                                    }
                                    else
                                    {
                                        TeleportItem(__instance, item, Constants.FileKeys.Stone);
                                    }
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
                            switch (FreeStoneSlots)
                            {
                                //not sure how it can get to -1, but here because it did
                                case < 0:

                                    if (_cfg.Teleportation)
                                    {
                                        var distance = Vector3.Distance(_cfg.DesignatedStoneLocation, pWgo.pos3);
                                        if (distance <= 750f)
                                        {
                                            DropOjectAndNull(__instance, item);
                                        }
                                        else
                                        {
                                            TeleportItem(__instance, item, Constants.FileKeys.Stone);
                                        }
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
                                        {
                                            DropOjectAndNull(__instance, item);
                                        }
                                        else
                                        {
                                            TeleportItem(__instance, item, Constants.FileKeys.Stone);
                                        }
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
                                    {
                                        ShowLootAddedIcon(item);
                                    }
                                    else
                                    {
                                        DropOjectAndNull(__instance, item);
                                    }

                                    if (ItemsDidntFit.Count > 0)
                                    {
                                        ShowMessage("", false, false, true, Constants.FileKeys.Stone);
                                    }

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
}
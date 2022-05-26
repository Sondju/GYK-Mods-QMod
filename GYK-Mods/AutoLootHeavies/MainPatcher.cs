using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Harmony;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AutoLootHeavies
{
    public class MainPatcher
    {
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
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.AutoLootHeavies");
            val.PatchAll(Assembly.GetExecutingAssembly());
            VectorsLoaded = false;
        }

        public static void UpdateConfig()
        {
            Config.WriteOptions();
            _cfg = Config.GetOptions();
        }

        public static bool GetLocations()
        {
            //EffectBubblesManager.ShowImmediately(MainGame.me.player_pos,
            //    $"GetLocations()",
            //    EffectBubblesManager.BubbleColor.Red, true, 5f);
            if (!File.Exists(VectorPath)) return false;

            var lines = File.ReadAllLines(VectorPath, Encoding.Default);

            foreach (var line in lines)
            {
                var splitLine = line.Split(',');

                var keyToAdd = new Vector3(float.Parse(splitLine[0].Trim()),
                    float.Parse(splitLine[1].Trim()), float.Parse(splitLine[2].Trim()));
                var valueToAdd = splitLine[3].Trim();
                if (!VectorDictionary.ContainsKey(keyToAdd))
                {
                    VectorDictionary.Add(keyToAdd, valueToAdd);
                }
            }

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
                EffectBubblesManager.ShowImmediately(pwo.bubble_pos, GJL.L("not_enough_something", "(en)"),
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
                        "o" => LastKnownOreLocation,
                        "t" => LastKnownTimberLocation,
                        "s" => LastKnownStoneLocation,
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
                        "o" => ore,
                        "t" => timber,
                        "s" => stone,
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

                EffectBubblesManager.ShowImmediately(pwo.bubble_pos, GJL.L("not_enough_something", "(en)"),
                    EffectBubblesManager.BubbleColor.Energy, true, 1f);
                
            }
        }

        public static void ShowMessage(string message, bool noStockpilesInRange, bool storageNowFull,
            bool cantStorageFull, string item)
        {
            var msg = message;
            if (noStockpilesInRange)
            {
                msg = item switch
                {
                    "o" => "Dang, no ore stockpiles in range...",
                    "s" => "Dang, no stone stockpiles in range...",
                    "t" => "Dang, no timber stockpiles in range...",
                    _ => msg
                };
            }

            if (storageNowFull)
            {
                msg = item switch
                {
                    "o" => "Phew...ore storage is now full!",
                    "s" => "Phew...stone storage is now full!",
                    "t" => "Phew...timber storage is now full!",
                    _ => msg
                };
            }

            if (cantStorageFull)
            {
                msg = item switch
                {
                    "o" => "I can't do that, I have no ore storage left!",
                    "s" => "I can't do that, I have no stone storage left!",
                    "t" => "I can't do that, I have no timber storage left!",
                    _ => msg
                };
            }

            MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                SmartSpeechEngine.VoiceID.None, true);
        }

        public static void GetClosestStockPile()
        {
            LastKnownTimberLocation = VectorDictionary.Where(x => x.Value == "t").ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key)).Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            LastKnownOreLocation = VectorDictionary.Where(x => x.Value == "o").ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key)).Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
            LastKnownStoneLocation = VectorDictionary.Where(x => x.Value == "s").ToDictionary(v => v.Key, v => Vector3.Distance(MainGame.me.player_pos, v.Key)).Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
        }

        [HarmonyPatch(typeof(MainGame))]
        [HarmonyPatch(nameof(MainGame.Update))]
        public class UpdateStockPiles
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (_cfg.DistanceBasedTeleport)
                {
                    if (Time.time - LastGetLocationScanTime < 10f)
                    {
                        return;
                    }

                    GetClosestStockPile();
                    LastGetLocationScanTime = Time.time;
                }
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
                    _cfg.Teleportation = !_cfg.Teleportation;
                    if (_cfg.Teleportation)
                    {
                        EffectBubblesManager.ShowImmediately(MainGame.me.player_pos,
                            "Teleportation is now ON.",
                            EffectBubblesManager.BubbleColor.Relation,
                            true, 3f);
                        GetLocations();
                        typeof(MainGame).GetMethod(nameof(MainGame.Update))?.Invoke(null, null);
                    }
                    else
                    {
                        EffectBubblesManager.ShowImmediately(MainGame.me.player_pos,
                            "Teleportation is now OFF.",
                            EffectBubblesManager.BubbleColor.Relation,
                            true, 3f);
                    }
                }

                if (Input.GetKeyUp(KeyCode.Alpha6))
                {
                    if (!_cfg.DistanceBasedTeleport)
                    {
                        if (!_cfg.Teleportation)
                        {
                            _cfg.Teleportation = true;
                            EffectBubblesManager.ShowImmediately(MainGame.me.player_pos,
                                "Teleportation is now ON.",
                                EffectBubblesManager.BubbleColor.Relation,
                                true, 3f);
                            UpdateConfig();
                        }
                        _cfg.DistanceBasedTeleport = true;
                        EffectBubblesManager.ShowImmediately(MainGame.me.player_pos,
                            "Distance-based teleportation is now ON.",
                            EffectBubblesManager.BubbleColor.Relation,
                            true, 3f);
                        UpdateConfig();
                        GetLocations();

                    }
                    else
                    {
                        _cfg.DistanceBasedTeleport = false;
                        EffectBubblesManager.ShowImmediately(MainGame.me.player_pos,
                            "Distance-based teleportation is now OFF.",
                            EffectBubblesManager.BubbleColor.Relation,
                            true, 3f);
                        UpdateConfig();

                    }
                    UpdateConfig();
                }

                if (Input.GetKeyUp(KeyCode.Alpha7))
                {
                    _cfg.DesignatedTimberLocation = MainGame.me.player_pos;
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Dump timber here!",
                        EffectBubblesManager.BubbleColor.Relation,
                        true, 3f);
                    UpdateConfig();
                }

                if (Input.GetKeyUp(KeyCode.Alpha8))
                {
                    _cfg.DesignatedOreLocation = MainGame.me.player_pos;
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Dump ore here!",
                        EffectBubblesManager.BubbleColor.Relation,
                        true, 3f);
                    UpdateConfig();
                }

                if (Input.GetKeyUp(KeyCode.Alpha9))
                {
                    _cfg.DesignatedStoneLocation = MainGame.me.player_pos;
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Dump stone & marble here!",
                        EffectBubblesManager.BubbleColor.Relation,
                        true, 3f);
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
                if (!VectorsLoaded)
                {
                    if (GetLocations())
                    {
                        GetClosestStockPile();
                        VectorsLoaded = true;
                    }
                }
                return true;
            }
        }
        
        [HarmonyPatch(typeof(BaseCharacterComponent))]
        [HarmonyPatch(nameof(BaseCharacterComponent.SetOverheadItem))]
        public class SetPatching
        {
            //this was prefix
            [HarmonyPostfix]
            public static void Postfix()
            {
                TimberPileCount = 0;
                StonePileCount = 0;
                OrePileCount = 0;
                UsedTimberSlots = 0;
                UsedStoneSlots = 0;
                UsedOreSlots = 0;

                foreach (var obj in StoredStockpiles.Where(obj => obj != null))
                {
                    if (obj.obj_id.Contains("mf_timber_1"))
                    {
                        UsedTimberSlots += obj.data.inventory.Count;
                        TimberPileCount++;
                        TimberTemp = obj;
                    }
                    else if (obj.obj_id.Contains("mf_ore_1"))
                    {
                        UsedOreSlots += obj.data.inventory.Count;
                        OrePileCount++;
                        OreTemp = obj;
                    }
                    else if (obj.obj_id.Contains("mf_stones_1"))
                    {
                        UsedStoneSlots += obj.data.inventory.Count;
                        StonePileCount++;
                        StoneTemp = obj;
                    }
                }


                FreeTimberSlots = (9 * TimberPileCount) - UsedTimberSlots;
                FreeStoneSlots = (6 * StonePileCount) - UsedStoneSlots;
                FreeOreSlots = (7 * OrePileCount) - UsedOreSlots;


                if (Time.time - LastScanTime < _cfg.ScanIntervalInSeconds)
                {
                    //EffectBubblesManager.ShowImmediately(MainGame.me.player_pos,
                    //    $"Scan Skipped {LastScanTime}",
                    //    EffectBubblesManager.BubbleColor.Red, true, 3f);
                    return;
                }
                //EffectBubblesManager.ShowImmediately(MainGame.me.player_pos,
                //    $"Scan Started {LastScanTime}",
                //    EffectBubblesManager.BubbleColor.White, true, 3f);
                LastScanTime = Time.time;
                Objects = Object.FindObjectsOfType<WorldGameObject>().Where(x =>
                        x.obj_id.Contains("mf_timber_1") | x.obj_id.Contains("mf_ore_1") |
                        x.obj_id.Contains("mf_stones_1"))
                    .ToList();
                StoredStockpiles = Objects;

                foreach (var obj in Objects.Where(obj => obj != null))
                {
                    if (obj.obj_id.Contains("mf_timber_1"))
                    {
                        if (!VectorDictionary.ContainsKey(obj.pos3))
                        {
                            VectorDictionary.Add(obj.pos3, "t");
                        }
                    }
                    else if (obj.obj_id.Contains("mf_ore_1"))
                    {
                        if (!VectorDictionary.ContainsKey(obj.pos3))
                        {
                            VectorDictionary.Add(obj.pos3, "o");
                        }
                    }
                    else if (obj.obj_id.Contains("mf_stones_1"))
                    {
                        if (!VectorDictionary.ContainsKey(obj.pos3))
                        {
                            VectorDictionary.Add(obj.pos3, "s");
                        }
                    }
                }
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
            public static bool Prefix(ref Item ___overhead_item, out (bool wood, bool stone, bool iron, bool runCode) __state)
            {

                var itemIsLog = ___overhead_item.definition.GetItemName().Contains("Log");
                var itemIsStone = ___overhead_item.definition.GetItemName().Contains("Stone") ||
                                  ___overhead_item.definition.GetItemName().Contains("Marble"); ;
                var itemIsOre = ___overhead_item.definition.GetItemName().Contains("Iron"); ;

                if (itemIsLog || itemIsStone || itemIsOre)
                {

                    __state = (itemIsLog, itemIsStone, itemIsOre, true);

                    return false;
                }

                __state = (false, false, false, false);

                return true;
            }

            [HarmonyPostfix]
            public static void Postfix(BaseCharacterComponent __instance, ref Item ___overhead_item, (bool wood, bool stone, bool iron, bool runCode) __state)
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
                                ShowMessage("", true, false, false, "o");
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
                                        TeleportItem(__instance, item, "o");
                                    }
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                    ShowMessage("", true, false, false, "o");
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
                                            TeleportItem(__instance, item, "o");
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
                                            TeleportItem(__instance, item, "o");
                                        }
                                    }
                                    else
                                    {
                                        DropOjectAndNull(__instance, item);
                                        //ShowMessage("", false, true, true, "o");
                                    }

                                    break;

                                case 1:

                                    success = PutToAllAndNull(__instance, oWgo, ItemsToInsert);
                                    if (success)
                                    {
                                        ShowLootAddedIcon(item);
                                        ShowMessage("", false, true, false, "o");
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

                                    if (ItemsDidntFit.Count > 0)
                                    {
                                        ShowMessage("", false, false, true, "o");
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
                                ShowMessage("", true, false, false, "t");
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
                                        TeleportItem(__instance, item, "t");
                                    }
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                    ShowMessage("", true, false, false, "t");
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
                                            TeleportItem(__instance, item, "t");
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
                                            TeleportItem(__instance, item, "t");
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
                                        ShowMessage("", false, true, false, "t");
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

                                    if (ItemsDidntFit.Count > 0)
                                    {
                                        ShowMessage("", false, false, true, "t");
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
                                ShowMessage("", true, false, false, "s");
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
                                        TeleportItem(__instance, item, "s");
                                    }
                                }
                                else
                                {
                                    DropOjectAndNull(__instance, item);
                                    ShowMessage("", true, false, false, "s");
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
                                            TeleportItem(__instance, item, "s");
                                        }
                                    }
                                    else
                                    {
                                        DropOjectAndNull(__instance, item);
                                        // ShowMessage("", false, false, true, "s");
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
                                            TeleportItem(__instance, item, "s");
                                        }
                                    }
                                    else
                                    {
                                        DropOjectAndNull(__instance, item);
                                        // ShowMessage("", false, false, true, "s");
                                    }

                                    break;

                                case 1:

                                    success = PutToAllAndNull(__instance, sWgo, ItemsToInsert);
                                    if (success)
                                    {
                                        ShowLootAddedIcon(item);
                                        ShowMessage("", false, true, false, "s");
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

                                    if (ItemsDidntFit.Count > 0)
                                    {
                                        ShowMessage("", false, false, true, "s");
                                    }

                                    break;
                            }
                        }
                    }
                    //else
                    //{
                    //    DropOjectAndNull(__instance, item);
                    //}
                }
                catch (Exception ex)
                {
                    ShowMessage(
                        "Something went wrong with the magic of teleporting resources - please let me know. A log was saved in the mod directory.", false,
                        false, false, "");
                    try
                    {
                        File.WriteAllText("./QMods/AutoLootHeavies/error.txt",
                            $"Mod: AutoLootHeavies, Message: {ex.Message}, Source: {ex.Source}, Trace: {ex.StackTrace}\n");
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
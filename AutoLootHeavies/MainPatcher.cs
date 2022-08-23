using HarmonyLib;
using Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace AutoLootHeavies;

public class MainPatcher
{
    private static readonly List<Stockpile> SortedStockpiles = new();
    private static Config.Options _cfg;
    private static float _lastBubbleTime;
    private static float _lastScanTime;
    private static bool _needScanning = true;
    private static List<WorldGameObject> _objects;
    private static bool _stockpilesRefreshed;

    private static float _xAdjustment;
    public static void Patch()
    {
        try
        {
            _cfg = Config.GetOptions();

            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.AutoLootHeavies");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _needScanning = true;
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
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
    
    private static void Log(string message, bool error = false)
    {
        Tools.Log("AutoLootHeavies", $"{message}", error);
    }

    private static bool TryPutToInventoryAndNull(BaseCharacterComponent __instance, WorldGameObject wgo,
        List<Item> itemsToInsert)
    {
        List<Item> failed = new();
        failed.Clear();
        var pwo = MainGame.me.player;
        var needEnergy = 1f;
        if (_cfg.DisableImmersionMode) needEnergy = 0f;

        if (pwo.IsPlayerInvulnerable()) needEnergy = 0f;

        if (pwo.energy >= needEnergy)
        {
            wgo.TryPutToInventory(itemsToInsert, out failed);
            if (failed.Count <= 0)
            {
                pwo.energy -= needEnergy;
                EffectBubblesManager.ShowStackedEnergy(pwo, -needEnergy);
                __instance.SetOverheadItem(null);
                return true;
            }

            return false;
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

  
    private static void ScanStockpiles()
    {
        _objects = Object.FindObjectsOfType<WorldGameObject>(true).Where(x =>
                x.obj_id.Contains(Constants.ItemObjectId.Timber) | x.obj_id.Contains(Constants.ItemObjectId.Ore) |
                x.obj_id.Contains(Constants.ItemObjectId.Stone)).Where(x => x.data.inventory_size > 0)
            .ToList();

        //clean the sorted stockpile list in case players have removed them
        foreach (var stockpile in SortedStockpiles.Where(stockpile => !_objects.Exists(a => a == stockpile.GetStockpileObject())))
        {
            SortedStockpiles.Remove(stockpile);
        }
    }

    private static void ShowLootAddedIcon(Item item)
    {
        item.definition.item_size = 1;
        DropCollectGUI.OnDropCollected(item);
        item.definition.item_size = 2;
        Sounds.PlaySound("pickup", null, true);
    }

    private static void TeleportItem(BaseCharacterComponent __instance, Item item)
    {
        var pwo = MainGame.me.player;
        var needEnergy = 3f;
        if (_cfg.DisableImmersionMode) needEnergy = 0f;

        if (pwo.IsPlayerInvulnerable()) needEnergy = 0f;

        if (pwo.energy >= needEnergy)
        {
            pwo.energy -= needEnergy;
            EffectBubblesManager.ShowStackedEnergy(pwo, -needEnergy);

            var loc = GetGridLocation();

            _xAdjustment = loc.Item1 * 75;

            var timber = _cfg.DesignatedTimberLocation;
            var ore = _cfg.DesignatedOreLocation;
            var stone = _cfg.DesignatedStoneLocation;

            timber.x += _xAdjustment;
            ore.x += _xAdjustment;
            stone.x += _xAdjustment;

            MainGame.me.player.DropItem(item, Direction.IgnoreDirection, timber, 0f, false);

            __instance.SetOverheadItem(null);
        }
        else
        {
            DropOjectAndNull(__instance, item);

            if (Time.time - _lastBubbleTime < 0.5f) return;

            _lastBubbleTime = Time.time;

            EffectBubblesManager.ShowImmediately(pwo.bubble_pos,
                GJL.L("not_enough_something", $"(en)"),
                EffectBubblesManager.BubbleColor.Energy, true, 1f);
        }
    }

    private static void UpdateConfig()
    {
        Config.WriteOptions();
        _cfg = Config.GetOptions();
    }
    
    private static void UpdateStockpiles()
    {
        //check each type, distance etc and create a new Stockpile object
        foreach (var stockpile in _objects)
        {
            var location = new Vector3((float)Math.Ceiling(stockpile.pos3.x), (float)Math.Ceiling(stockpile.pos3.y),
                (float)Math.Ceiling(stockpile.pos3.z));

            Stockpile.StockpileType type;
            if (stockpile.obj_id.Contains(Constants.ItemObjectId.Ore))
            {
                type = Stockpile.StockpileType.Ore;
            }
            else if (stockpile.obj_id.Contains(Constants.ItemObjectId.Stone))
            {
                type = Stockpile.StockpileType.Stone;
            }
            else if (stockpile.obj_id.Contains(Constants.ItemObjectId.Timber))
            {
                type = Stockpile.StockpileType.Timber;
            }
            else
            {
                type = Stockpile.StockpileType.Unknown;
            }

            var distance = Vector3.Distance(MainGame.me.player_pos, location);

            var newStockpile = new Stockpile(location, type, distance, stockpile);

            var exists = SortedStockpiles.Find(a => a.GetStockpileObject() == stockpile);
            if (exists != null)
            {
                exists.SetDistanceFromPlayer(distance);
            }
            else
            {
                if (type != Stockpile.StockpileType.Unknown)
                {
                    SortedStockpiles.Add(newStockpile);
                }
            }
        }

        //sort them by distance from player
        SortedStockpiles.Sort((x, y) => x.GetDistanceFromPlayer().CompareTo(y.GetDistanceFromPlayer()));
    }

    private struct Constants
    {
        public struct FileKeys
        {
            public const string Ore = "o";
            public const string Stone = "s";
            public const string Timber = "t";
        }

        public struct ItemDefinitionId
        {
            public const string Marble = "marble";
            public const string Ore = "ore_metal";
            public const string Stone = "stone";
            public const string Wood = "wood";
        }

        public struct ItemObjectId
        {
            public const string Ore = "mf_ore_1";
            public const string Stone = "mf_stones_1";
            public const string Timber = "mf_timber_1";
        }
    }
    [HarmonyPatch(typeof(MovementComponent), "UpdateMovement")]
    public static class MovementComponentUpdateMovementPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!MainGame.game_started) return;

            if (_needScanning) return;

            ScanStockpiles();
        }
    }

    [HarmonyPatch(typeof(BaseCharacterComponent), nameof(BaseCharacterComponent.DropOverheadItem))]
    public class BaseCharacterComponentDropOverheadItemPatch
    {
        [HarmonyPostfix]
        public static void Postfix(BaseCharacterComponent __instance, ref Item ___overhead_item,
            ref (bool wood, bool stone, bool iron, bool runCode) __state)
        {
            if (!__state.runCode) return;

            List<Item> insert = new();
            var item = ___overhead_item;
            var itemId = ___overhead_item.id;
            insert.Clear();

            insert.Add(___overhead_item);

            foreach (var stockpile in SortedStockpiles)
            {
                Log($"[ALH]: Trying to insert {itemId} into {stockpile.GetStockpileObject()}, {stockpile.GetDistanceFromPlayer()} units away.");
                var success = TryPutToInventoryAndNull(__instance, stockpile.GetStockpileObject(), insert);
                if (success)
                {
                    Log($"[ALH]: Successfully inserted {itemId} into {stockpile.GetStockpileObject()}, {stockpile.GetDistanceFromPlayer()} units away.");
                    ShowLootAddedIcon(item);
                    break;
                }

                Log($"[ALH]: Failed to insert {itemId} into {stockpile.GetStockpileObject()}, {stockpile.GetDistanceFromPlayer()} units away.");
            }

            if (___overhead_item != null)
            {
                if (_cfg.TeleportWhenStockPilesFull)
                {
                    TeleportItem(__instance, ___overhead_item);
                }
                else
                {
                    DropOjectAndNull(__instance, ___overhead_item);
                }
            }
        }

        [HarmonyPrefix]
        public static bool Prefix(ref Item ___overhead_item, ref (bool wood, bool stone, bool iron, bool runCode) __state)
        {
            var itemIdentifier = ___overhead_item.definition.id;

            var itemIsLog = itemIdentifier.ToLower().Contains(Constants.ItemDefinitionId.Wood);
            var itemIsStone = itemIdentifier.Contains(Constants.ItemDefinitionId.Stone) ||
                              itemIdentifier.Contains(Constants.ItemDefinitionId.Marble);
            var itemIsOre = itemIdentifier.Contains(Constants.ItemDefinitionId.Ore);

            var run = itemIsLog || itemIsStone || itemIsOre;
            if (!run) return true;
            UpdateStockpiles();

            var stocks = string.Empty;
            foreach (var s in SortedStockpiles)
            {
                var stockpile = s.GetStockpileObject();
                stocks += $"\nStockpile: {s.GetStockpileType()}\n";
                stocks += $"Location: {s.GetLocation()}\n";
                stocks += $"Distance from player: {s.GetDistanceFromPlayer()}\n";
                stocks += $"Total Space: {stockpile.data.inventory_size}\n";
                stocks += $"Free Space: {stockpile.data.inventory_size - stockpile.data.inventory.Count}\n";
                stocks += "---------------";
            }

            Log($"{stocks}");

            __state = (itemIsLog, itemIsStone, itemIsOre, true);
            return false;
        }
    }

    //        if (Input.GetKeyUp(KeyCode.Alpha9))
    //        {
    //            _cfg.DesignatedStoneLocation = MainGame.me.player_pos;
    //            Tools.ShowMessage(strings.DumpStone, _cfg.DesignatedStoneLocation);
    //            UpdateConfig();
    //        }
    //    }
    //}
    [HarmonyPatch(typeof(BaseCharacterComponent), nameof(BaseCharacterComponent.SetOverheadItem))]
    public class BaseCharacterComponentSetOverheadItemPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (Time.time - _lastScanTime < _cfg.ScanIntervalInSeconds)
            {
                _lastScanTime = Time.time;
                ScanStockpiles();
            }
            UpdateStockpiles();
        }
    }

    [HarmonyPatch(typeof(MainGame), nameof(MainGame.Update))]
    public class MainGameUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (_stockpilesRefreshed) return;
            ScanStockpiles();
            _stockpilesRefreshed = true;
        }
    }

    //hooks into the time of day update and saves if the K key was pressed
    //[HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
    //public static class TimeOfDayUpdatePatch
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix()
    //    {
    //        if (Input.GetKeyUp(KeyCode.Alpha5))
    //        {
    //            if (!_cfg.TeleportWhenStockPilesFull)
    //            {
    //                _cfg.TeleportWhenStockPilesFull = true;
    //                Tools.ShowMessage(strings.TeleOn, Vector3.zero);
    //                UpdateConfig();
    //                GetLocations();
    //                //typeof(MainGame).GetMethod(nameof(MainGame.Update))?.Invoke(null, null);
    //            }
    //            else
    //            {
    //                _cfg.TeleportWhenStockPilesFull = false;
    //                Tools.ShowMessage(strings.TeleOff, Vector3.zero);
    //                UpdateConfig();
    //            }

    //            UpdateConfig();
    //        }

    //        if (Input.GetKeyUp(KeyCode.Alpha6))
    //        {
    //            if (!_cfg.TeleportToDumpsite)
    //            {
    //                if (!_cfg.TeleportWhenStockPilesFull)
    //                {
    //                    _cfg.TeleportWhenStockPilesFull = true;
    //                    Tools.ShowMessage(strings.TeleOn, Vector3.zero);
    //                    UpdateConfig();
    //                }

    //                _cfg.TeleportToDumpsite = true;
    //                Tools.ShowMessage(strings.DistTeleOn, Vector3.zero);
    //                UpdateConfig();
    //                GetLocations();
    //            }
    //            else
    //            {
    //                _cfg.TeleportToDumpsite = false;
    //                Tools.ShowMessage(strings.DistTeleOff, Vector3.zero);
    //                UpdateConfig();
    //            }

    //            UpdateConfig();
    //        }

    //        if (Input.GetKeyUp(KeyCode.Alpha7))
    //        {
    //            _cfg.DesignatedTimberLocation = MainGame.me.player_pos;
    //            Tools.ShowMessage(strings.DumpTimber, _cfg.DesignatedTimberLocation);
    //            UpdateConfig();
    //        }

    //        if (Input.GetKeyUp(KeyCode.Alpha8))
    //        {
    //            _cfg.DesignatedOreLocation = MainGame.me.player_pos;
    //            Tools.ShowMessage(strings.DumpOre, _cfg.DesignatedOreLocation);
    //            UpdateConfig();
    //        }
}
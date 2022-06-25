using AppleTreesEnhanced.lang;
using HarmonyLib;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AppleTreesEnhanced;

public class MainPatcher
{
    private static Config.Options _cfg;
    private static string Lang { get; set; }

    private static readonly string[] WorldReadyHarvests = {
        "bush_1_berry","bush_2_berry","bush_3_berry"
    };

    private static bool _updateDone;


    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.AppleTreesEnhanced");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _cfg = Config.GetOptions();

            Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
            _updateDone = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[AppleTreesEnhanced]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }

    private static void ShowMessage(WorldGameObject obj, string message, int qty)
    {
        if (_cfg.ShowHarvestReadyMessages)
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
            var newObjPos = obj.pos3;

            if (obj.obj_id.Contains("berry")) newObjPos.y += 100f;

            if (obj.obj_id.Contains("tree")) newObjPos.y += 250f;

            EffectBubblesManager.ShowImmediately(newObjPos, message + $" ({qty})",
                EffectBubblesManager.BubbleColor.Relation,
                true, 3f);
        }
    }

    private struct Constants
    {
        public struct HarvestGrowing
        {
            public const string GardenAppleTree = "tree_apple_garden_empty";
            public const string GardenBerryBush = "bush_berry_garden_empty";
            public const string WorldBerryBush1 = "bush_1";
            public const string WorldBerryBush2 = "bush_2";
            public const string WorldBerryBush3 = "bush_3";
        }

        public struct HarvestItem
        {
            public const string AppleTree = "fruit:apple_red_crop";
            public const string BerryBush = "fruit:berry";
        }

        public struct HarvestReady
        {
            public const string GardenAppleTree = "tree_apple_garden_ready";
            public const string GardenBerryBush = "bush_berry_garden_ready";
            public const string WorldBerryBush1 = "bush_1_berry";
            public const string WorldBerryBush2 = "bush_2_berry";
            public const string WorldBerryBush3 = "bush_3_berry";
        }
        public struct HarvestSpawner
        {
            public const string GardenAppleTree = "tree_apple_garden_crops_growing";
            public const string GardenBerryBush = "bush_berry_garden";
            public const string WorldBerryBush1 = "bush_1_berry_respawn";
            public const string WorldBerryBush2 = "bush_2_berry_respawn";
            public const string WorldBerryBush3 = "bush_3_berry_respawn";
        }
    }

    [HarmonyPatch(typeof(MainGame), nameof(MainGame.Update))]
    public static class MainGameUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {

            if (!MainGame.game_started || !MainGame.loaded_from_scene_main || _updateDone) return;
            var readyGardenTrees = Object.FindObjectsOfType<WorldGameObject>(true).Where(a => a.obj_id == "tree_apple_garden_ready");
            var readyGardenBushes = Object.FindObjectsOfType<WorldGameObject>(true).Where(a => a.obj_id== "bush_berry_garden_ready");
            var readyWorldBushes = Object.FindObjectsOfType<WorldGameObject>(true).Where(a => WorldReadyHarvests.Contains(a.obj_id));

            foreach (var item in readyGardenTrees)
            {
                ProcessGardenAppleTree(item);
            }

            foreach (var item in readyGardenBushes)
            {
                ProcessGardenBerryBush(item);
            }

            foreach (var item in readyWorldBushes)
            {
                switch (item.obj_id)
                {
                    case Constants.HarvestReady.WorldBerryBush1:
                        ProcessBerryBush1(item);
                        break;
                    case Constants.HarvestReady.WorldBerryBush2:
                        ProcessBerryBush2(item);
                        break;
                    case Constants.HarvestReady.WorldBerryBush3:
                        ProcessBerryBush3(item);
                        break;
                }
            }

            _updateDone = true;
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

    private static void ProcessDropAndRespawn(WorldGameObject wgo, string replaceString, string craftString, string harvestItem, string message, int rand)
    {
        for (var i = 0; i < rand; i++)
        {
            var item = new Item(harvestItem, 1);
            wgo.DropItem(item, Direction.None, force: 5f,
                check_walls: false);
        }

        //Debug.LogWarning(
        // $"[AppleTreesEnhanced] Intercepted {Constants.HarvestReady.GardenAppleTree}, dropping {Constants.HarvestItem.AppleTree}, setting object to {Constants.HarvestGrowing.GardenAppleTree} and starting craft of {Constants.HarvestSpawner.GardenAppleTree}");
        wgo.ReplaceWithObject(replaceString, true);
        wgo.GetComponent<ChunkedGameObject>().Init(true);
        wgo.TryStartCraft(craftString);
        ShowMessage(wgo, message, rand);
    }

    private static void ProcessGardenAppleTree(WorldGameObject wgo)
    {
        if (!_cfg.IncludeGardenTrees) return;
        var rand = 15;

        if (_cfg.RealisticHarvest)
        {
            rand = Random.Range(6, 16);
        }

        if (_cfg.RealisticHarvest)
        {
            var o = wgo;
            var dropRand = Random.Range(2, 61);
            GJTimer.AddTimer(dropRand, delegate
            {
                if (o.obj_id == Constants.HarvestGrowing.GardenAppleTree) return;
                ProcessDropAndRespawn(o, Constants.HarvestGrowing.GardenAppleTree,
                    Constants.HarvestSpawner.GardenAppleTree, Constants.HarvestItem.AppleTree,
                    strings.ApplesReady, rand);
            });
        }
        else
        {
            ProcessDropAndRespawn(wgo, Constants.HarvestGrowing.GardenAppleTree,
                Constants.HarvestSpawner.GardenAppleTree, Constants.HarvestItem.AppleTree,
                strings.ApplesReady, rand);
        }
    }

    private static void ProcessGardenBerryBush(WorldGameObject wgo)
    {
        if (!_cfg.IncludeGardenBerryBushes) return;
        var rand = 4;

        if (_cfg.RealisticHarvest)
        {
            rand = Random.Range(2, 5);
        }
        if (_cfg.RealisticHarvest)
        {
            var o = wgo;
            var dropRand = Random.Range(2, 61);
            GJTimer.AddTimer(dropRand, delegate
            {
                if (o.obj_id == Constants.HarvestGrowing.GardenBerryBush) return;
                ProcessDropAndRespawn(o, Constants.HarvestGrowing.GardenBerryBush,
                    Constants.HarvestSpawner.GardenBerryBush, Constants.HarvestItem.BerryBush,
                    strings.BerriesReady, rand);
            });
        }
        else
        {
            ProcessDropAndRespawn(wgo, Constants.HarvestGrowing.GardenBerryBush,
                Constants.HarvestSpawner.GardenBerryBush, Constants.HarvestItem.BerryBush,
                strings.BerriesReady, rand);
        }
    }

    private static void ProcessBerryBush1(WorldGameObject wgo)
    {
        if (!_cfg.IncludeWorldBerryBushes) return;
        var rand = 4;

        if (_cfg.RealisticHarvest)
        {
            rand = Random.Range(2, 5);
        }

        if (_cfg.RealisticHarvest)
        {
            var o = wgo;
            var dropRand = Random.Range(2, 61);
            GJTimer.AddTimer(dropRand, delegate
            {
                if (o.obj_id == Constants.HarvestGrowing.WorldBerryBush1) return;
                ProcessDropAndRespawn(o, Constants.HarvestGrowing.WorldBerryBush1,
                    Constants.HarvestSpawner.WorldBerryBush2, Constants.HarvestItem.BerryBush,
                    strings.BerriesReady, rand);
            });
        }
        else
        {
            ProcessDropAndRespawn(wgo, Constants.HarvestGrowing.WorldBerryBush1,
                Constants.HarvestSpawner.WorldBerryBush1, Constants.HarvestItem.BerryBush,
                strings.BerriesReady, rand);
        }
    }

    private static void ProcessBerryBush2(WorldGameObject wgo)
    {
        if (!_cfg.IncludeWorldBerryBushes) return;
        var rand = 4;

        if (_cfg.RealisticHarvest)
        {
            rand = Random.Range(2, 5);
        }

        if (_cfg.RealisticHarvest)
        {
            var o = wgo;
            var dropRand = Random.Range(2, 61);
            GJTimer.AddTimer(dropRand, delegate
            {
                if (o.obj_id == Constants.HarvestGrowing.WorldBerryBush2) return;
                ProcessDropAndRespawn(o, Constants.HarvestGrowing.WorldBerryBush2,
                    Constants.HarvestSpawner.WorldBerryBush2, Constants.HarvestItem.BerryBush,
                    strings.BerriesReady, rand);
            });
        }
        else
        {
            ProcessDropAndRespawn(wgo, Constants.HarvestGrowing.WorldBerryBush2,
                Constants.HarvestSpawner.WorldBerryBush2, Constants.HarvestItem.BerryBush,
                strings.BerriesReady, rand);
        }
    }

    private static void ProcessBerryBush3(WorldGameObject wgo)
    {
        if (!_cfg.IncludeWorldBerryBushes) return;
        var rand = 4;

        if (_cfg.RealisticHarvest)
        {
            rand = Random.Range(2, 5);
        }

        if (_cfg.RealisticHarvest)
        {
            var o = wgo;
            var dropRand = Random.Range(2, 61);
            GJTimer.AddTimer(dropRand, delegate
            {
                if (o.obj_id == Constants.HarvestGrowing.WorldBerryBush3) return;
                ProcessDropAndRespawn(o, Constants.HarvestGrowing.WorldBerryBush3,
                    Constants.HarvestSpawner.WorldBerryBush3, Constants.HarvestItem.BerryBush,
                    strings.BerriesReady, rand);
            });
        }
        else
        {
            ProcessDropAndRespawn(wgo, Constants.HarvestGrowing.WorldBerryBush3,
                Constants.HarvestSpawner.WorldBerryBush3, Constants.HarvestItem.BerryBush,
                strings.BerriesReady, rand);
        }
    }

    [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.ReplaceWithObject))]
    public static class WorldGameObjectSmartInstantiate
    {
        [HarmonyPostfix]
        public static void Postfix(ref WorldGameObject __instance, ref string new_obj_id)
        {
            if (string.Equals(new_obj_id, Constants.HarvestReady.GardenAppleTree))
            {
                ProcessGardenAppleTree(__instance);

            }
            else if (string.Equals(new_obj_id, Constants.HarvestReady.GardenBerryBush))
            {
                ProcessGardenBerryBush(__instance);

            }
            else if (string.Equals(new_obj_id, Constants.HarvestReady.WorldBerryBush1))
            {
                ProcessBerryBush1(__instance);
            }
            else if (string.Equals(new_obj_id, Constants.HarvestReady.WorldBerryBush2))
            {
                ProcessBerryBush2(__instance);
            }
            else if (string.Equals(new_obj_id, Constants.HarvestReady.WorldBerryBush3))
            {
                ProcessBerryBush3(__instance);
            }
        }
    }
}
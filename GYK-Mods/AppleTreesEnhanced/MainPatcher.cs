using System.Globalization;
using HarmonyLib;
using System.Reflection;
using System.Threading;
using AppleTreesEnhanced.lang;

namespace AppleTreesEnhanced
{
    public class MainPatcher
    {
        private static Config.Options _cfg;
        private struct Constants
        {
            public struct HarvestReady
            {
                public const string GardenBerryBush = "bush_berry_garden_ready";
                public const string WorldBerryBush1 = "bush_1_berry";
                public const string WorldBerryBush2 = "bush_2_berry";
                public const string WorldBerryBush3 = "bush_3_berry";
                public const string GardenAppleTree = "tree_apple_garden_ready";
                
            }

            public struct HarvestGrowing
            {
                public const string GardenBerryBush = "bush_berry_garden_empty";
                public const string WorldBerryBush1 = "bush_1";
                public const string WorldBerryBush2 = "bush_2";
                public const string WorldBerryBush3 = "bush_3";
                public const string GardenAppleTree = "tree_apple_garden_empty";
            }

            public struct HarvestSpawner
            {
                public const string GardenBerryBush = "bush_berry_garden";
                public const string WorldBerryBush1 = "bush_1_berry_respawn";
                public const string WorldBerryBush2 = "bush_2_berry_respawn";
                public const string WorldBerryBush3 = "bush_3_berry_respawn";
                public const string GardenAppleTree = "tree_apple_garden_crops_growing";
            }

            public struct HarvestItem
            {
                public const string BerryBush = "fruit:berry";
                public const string AppleTree = "fruit:apple_red_crop";
            }
        }

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.AppleTreesEnhanced");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            _cfg = Config.GetOptions();
        }

        private static void ShowMessage(WorldGameObject obj, string message)
        {
            var lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
            var newObjPos = obj.pos3;

            if (obj.obj_id.Contains("berry"))
            {
                newObjPos.y += 100f;
            }
            if (obj.obj_id.Contains("tree"))
            {
                newObjPos.y += 250f;
            }
            
            EffectBubblesManager.ShowImmediately(newObjPos, message,
                    EffectBubblesManager.BubbleColor.Relation,
                    true, 3f);
            
        }


        [HarmonyPatch(typeof(WorldGameObject))]
        [HarmonyPatch(nameof(WorldGameObject.ReplaceWithObject))]
        public static class WorldGameObjectSmartInstantiate
        {
      
            [HarmonyPostfix]
            public static void Postfix(ref WorldGameObject __instance, ref string new_obj_id)
            {
                if (_cfg.IncludeGardenBerryBushes)
                {
                    if (string.Equals(new_obj_id, Constants.HarvestReady.GardenBerryBush))
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            __instance.DropItem(new Item(Constants.HarvestItem.BerryBush, 1), Direction.None, force: 5f, check_walls: false);
                        }

                        __instance.ReplaceWithObject(Constants.HarvestGrowing.GardenBerryBush, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft(Constants.HarvestSpawner.GardenBerryBush);
                        ShowMessage(__instance, strings.BerriesReady);
                    }
                }

                if (_cfg.IncludeWorldBerryBushes)
                {
                    if (string.Equals(new_obj_id, Constants.HarvestReady.WorldBerryBush1))
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            __instance.DropItem(new Item(Constants.HarvestItem.BerryBush, 1), Direction.None, force: 5f, check_walls: false);
                        }

                        __instance.ReplaceWithObject(Constants.HarvestGrowing.WorldBerryBush1, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft(Constants.HarvestSpawner.WorldBerryBush1);
                        ShowMessage(__instance, strings.BerriesReady);
                    }

                    if (string.Equals(new_obj_id, Constants.HarvestReady.WorldBerryBush2))
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            __instance.DropItem(new Item(Constants.HarvestItem.BerryBush, 1), Direction.None, force: 5f, check_walls: false);
                        }

                        __instance.ReplaceWithObject(Constants.HarvestGrowing.WorldBerryBush2, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft(Constants.HarvestSpawner.WorldBerryBush2);
                        ShowMessage(__instance, strings.BerriesReady);
                    }

                    if (string.Equals(new_obj_id, Constants.HarvestReady.WorldBerryBush3))
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            __instance.DropItem(new Item(Constants.HarvestItem.BerryBush, 1), Direction.None, force: 5f, check_walls: false);
                        }
                
                        __instance.ReplaceWithObject(Constants.HarvestGrowing.WorldBerryBush3, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft(Constants.HarvestSpawner.WorldBerryBush3);
                        ShowMessage(__instance, strings.BerriesReady);
                    }
                }

                if (_cfg.IncludeGardenTrees)
                {
                    if (string.Equals(new_obj_id, Constants.HarvestReady.GardenAppleTree))
                    {
                        for (var i = 0; i < 15; i++)
                        {
                            __instance.DropItem(new Item(Constants.HarvestItem.AppleTree, 1), Direction.None, force: 5f, check_walls: false);
                        }
                        
                        __instance.ReplaceWithObject(Constants.HarvestGrowing.GardenAppleTree, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft(Constants.HarvestSpawner.GardenAppleTree);
                        ShowMessage(__instance, strings.ApplesReady);
                    }
                }
            }

          
        }
    }
}
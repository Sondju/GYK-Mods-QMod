using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace AppleTreesEnhanced
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.AppleTreesEnhanced");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            _cfg = Config.GetOptions();
        }

        [HarmonyPatch(typeof(WorldGameObject))]
        [HarmonyPatch(nameof(WorldGameObject.ReplaceWithObject))]
        public static class WorldGameObjectSmartInstantiate
        {
            [HarmonyPostfix]
            public static void Postfix(WorldGameObject __instance, ref string new_obj_id)
            {
                if (_cfg.IncludeGardenBerryBushes)
                {
                    if (string.Equals(new_obj_id, "bush_berry_garden_ready"))
                    {
                        new_obj_id = "bush_berry_garden_empty";

                        for (var i = 0; i < 4; i++)
                        {
                            __instance.DropItem(new Item("fruit:berry", 1), (Direction)Random.Range(1, 5));
                        }

                        __instance.ReplaceWithObject(new_obj_id, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft("bush_berry_garden");
                    }
                }

                if (_cfg.IncludeWorldBerryBushes)
                {
                    if (string.Equals(new_obj_id, "bush_1_berry"))
                    {
                        new_obj_id = "bush_1";
                        for (var i = 0; i < 4; i++)
                        {
                            __instance.DropItem(new Item("fruit:berry", 1), (Direction)Random.Range(1, 5));
                        }
                        __instance.ReplaceWithObject(new_obj_id, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft("bush_1_berry_respawn");
                    }

                    if (string.Equals(new_obj_id, "bush_2_berry"))
                    {
                        new_obj_id = "bush_2";
                        for (var i = 0; i < 4; i++)
                        {
                            __instance.DropItem(new Item("fruit:berry", 1), (Direction)Random.Range(1, 5));
                        }
                        __instance.ReplaceWithObject(new_obj_id, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft("bush_2_berry_respawn");
                    }

                    if (string.Equals(new_obj_id, "bush_3_berry"))
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            __instance.DropItem(new Item("fruit:berry", 1), (Direction)Random.Range(1, 5));
                        }
                        new_obj_id = "bush_3";
                        __instance.ReplaceWithObject(new_obj_id, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft("bush_3_berry_respawn");
                    }
                }

                if (_cfg.IncludeGardenTrees)
                {
                    if (string.Equals(new_obj_id, "tree_apple_garden_ready"))
                    {
                        for (var i = 0; i < 15; i++)
                        {
                            __instance.DropItem(new Item("fruit:apple_red_crop", 1), (Direction)Random.Range(1, 5));
                        }
                        
                        new_obj_id = "tree_apple_garden_empty";
                        __instance.ReplaceWithObject(new_obj_id, true);
                        __instance.GetComponent<ChunkedGameObject>().Init(true);
                        __instance.TryStartCraft("tree_apple_garden_crops_growing");
                    }
                }
            }
        }
    }
}
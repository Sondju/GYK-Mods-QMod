using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MiscBitsAndBobs
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        public static string[] TavernItems =
        {
            "npc_tavern_barman", "tavern_cellar_rack", "tavern_cellar_rack_1", "tavern_cellar_rack_2", "tavern_cellar_rack_3", "tavern_cellar_rack_4", "tavern_cellar_rack_5"
        };

        public static ItemDefinition.ItemType[] ToolItems =
        {
            ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
            ItemDefinition.ItemType.Pickaxe, ItemDefinition.ItemType.FishingRod, ItemDefinition.ItemType.BodyArmor,
            ItemDefinition.ItemType.HeadArmor, ItemDefinition.ItemType.Sword
        };


        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.TavernTweaks");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
            _cfg = Config.GetOptions();
        }

        [HarmonyPatch(typeof(InventoryGUI))]
        [HarmonyPatch(nameof(InventoryGUI.OnItemOver))]
        public static class PatchCantDestroy
        {
            [HarmonyPrefix]
            public static void Prefix(InventoryGUI __instance)
            {
                if (!_cfg.AllowHandToolDestroy) return;
                if (__instance == null) return;
                var itemDef = __instance.selected_item?.definition;
                if (itemDef == null) return;
                if (ToolItems.Contains(itemDef.type))
                {
                    itemDef.player_cant_throw_out = false;
                }
            }
        }

        //patch tools to be stack-able
        [HarmonyPatch(typeof(GameBalance), "LoadGameBalance")]
        public static class PatchTools
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                foreach (var itemDefinition in GameBalance.me.items_data.Where(itemDefinition => ToolItems.Contains(itemDefinition.type)).Where(itemDefinition => itemDefinition.stack_count < _cfg.ToolStackSize))
                {
                    itemDefinition.stack_count += _cfg.ToolStackSize;
                }
            }
        }

        //makes the racks and the barman inventory larger
        [HarmonyPatch(typeof(WorldGameObject), "InitNewObject")]
        public static class PatchTavernInventorySize
        {
            [HarmonyPostfix]
            private static void Postfix(WorldGameObject __instance)
            {
                // File.AppendAllText("./qmods/objects.txt", __instance.obj_id + "\n");
                if (TavernItems.Contains(__instance.obj_id))
                {
                    __instance.data.SetInventorySize(__instance.obj_def.inventory_size + (int)_cfg.TavernInvIncrease);
                }
            }
        }

        [HarmonyPatch(typeof(GameGUI))]
        [HarmonyPatch(nameof(GameGUI.Open))]
        public static class QuietMusicPatch1
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (_cfg.QuietMusicInGui)
                {
                    SmartAudioEngine.me.SetDullMusicMode();
                }
            }
        }

        [HarmonyPatch(typeof(GameGUI))]
        [HarmonyPatch(nameof(GameGUI.Hide))]
        public static class QuietMusicPatch2
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (_cfg.QuietMusicInGui)
                {
                    SmartAudioEngine.me.SetDullMusicMode(false);
                }
            }
        }


        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class ShowInvOnly
        {
            [HarmonyPrefix]
            private static void Prefix(InventoryPanelGUI __instance, ref MultiInventory multi_inventory)
            {
                if (_cfg.DontShowEmptyRowsInInventory)
                {
                    __instance.dont_show_empty_rows = true;
                }

                if (!_cfg.ShowOnlyPersonalInventory) return;
                var multiInventory = new MultiInventory();
                var num = 0;
                foreach (var inventory in multi_inventory.all)
                {
                    multiInventory.AddInventory(inventory);
                    num++;
                    if (num == 1)
                    {
                        break;
                    }
                }

                multi_inventory = multiInventory;
            }
        }

        //makes halloween an annual event instead of the original 2018...
        [HarmonyPatch(typeof(GameSave))]
        [HarmonyPatch(nameof(GameSave.GlobalEventsCheck))]
        internal class PatchHalloweenEvent
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                return false;
            }

            [HarmonyPostfix]
            private static void Postfix()
            {
                if (_cfg.HalloweenNow)
                {
                    foreach (var globalEventBase in new List<GlobalEventBase>()
                             {
                                 new("halloween", DateTime.Now, new TimeSpan(14, 0, 0, 0))
                                 {
                                     on_start_script = new Scene1100_To_SceneHelloween(),
                                     on_finish_script = new SceneHelloween_To_Scene1100()
                                 }
                             })
                        globalEventBase.Process();
                }
                else
                {
                    var year = DateTime.Now.Year;
                    foreach (var globalEventBase in new List<GlobalEventBase>()
                             {
                                 new("halloween", new DateTime(year, 10, 29), new TimeSpan(14, 0, 0, 0))
                                 {
                                     on_start_script = new Scene1100_To_SceneHelloween(),
                                     on_finish_script = new SceneHelloween_To_Scene1100()
                                 }
                             })
                        globalEventBase.Process();
                }
            }
        }
    }
}
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace WheresMaStorage
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        private static WorldGameObject _wgo;
        private static bool _talkingToNpc;

        private static readonly string[] IncludedZones =
        {
            "garden",
            "graveyard",
            "alchemy",
            "mf_wood",
            "church",
            "cellar",
            "morgue",
            "home",
            "vineyard",
            "beegarden",
            "tree_garden",
            "stone_workyard",
            "storage",
            "sacrifice",
            "cremation",
            "zombie_sawmill",
            "player_tavern_cellar",
            "refugees_camp",
            "souls",
            "players_tavern",
            //"tavern",
            //"vilage"
        };

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.WheresMaStorage");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _cfg = Config.GetOptions();
        }

        [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
        public static class GameBalanceLoadGameBalancePatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                foreach (var od in GameBalance.me.objs_data.Where(od => od.interaction_type == ObjectDefinition.InteractionType.Chest))
                {
                    od.inventory_size += _cfg.AdditionalInventorySpace;
                }

                foreach (var id in GameBalance.me.items_data.Where(id => id.stack_count is > 1 and < 999))
                {
                    id.stack_count = _cfg.StackSizeForStackables;
                }
            }
        }


        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPrefix]
            public static void Prefix(WorldGameObject __instance, WorldGameObject other_obj)
            {
                if (!MainGame.game_started || __instance == null) return;
                if (other_obj == MainGame.me.player)
                {
                    _wgo = __instance;
                }
            }

            [HarmonyPostfix]
            public static void Postfix(WorldGameObject __instance, WorldGameObject other_obj)
            {
                if (!MainGame.game_started || __instance == null) return;
                if (other_obj == MainGame.me.player)
                {
                    _wgo = null;
                }
            }
        }

        //todo: fix the ShowOnlyPersonalInventory only showing the first tier for vendors
        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class InventoryPanelGuiDoOpeningPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref InventoryPanelGUI __instance, ref MultiInventory multi_inventory)
            {
                if (_cfg.DontShowEmptyRowsInInventory)
                {
                    __instance.dont_show_empty_rows = true;
                }


                if (_cfg.ShowOnlyPersonalInventory || (_wgo != null && _wgo.obj_def.interaction_type is ObjectDefinition.InteractionType.Chest))
                {
                    var onlyMineInventory = new MultiInventory();
                    onlyMineInventory.AddInventory(multi_inventory.all[0]);
                    multi_inventory = onlyMineInventory;
                }
                else
                {
                    var myInv = new MultiInventory();
                    multi_inventory.all.ForEach(inv =>
                    {
                        if (inv == null) return;
                        if (inv._obj_id.Contains("refugee") || inv._obj_id.Contains("pump") || inv._obj_id.Contains("reputation") || inv._obj_id.Contains("builddesk") ||
                            inv.name.Contains("stockpile")) return;
                        if (inv.data.inventory.Count <= 0) return;
                        myInv.AddInventory(inv);
                        // Debug.LogError($"[MBB] Inv Name: {inv.name}, Obj ID: {inv._obj_id}");
                    });

                    multi_inventory = myInv;
                }
            }
        }

        [HarmonyPatch(typeof(InventoryPanelGUI), nameof(InventoryPanelGUI.SetGrayToNotMainWidgets))]
        public static class InventoryPanelGuiSetGrayToNotMainWidgets
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                return false;
            }
        }

        //todo: Test the mod isn't crashing with a new game 
        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.GetMultiInventory))]
        public static class WorldGameObjectGetMultiInventoryPatch
        {
            [HarmonyPostfix]
            public static void Postfix(
                WorldGameObject __instance,
                ref MultiInventory __result,
                bool include_toolbelt = false)
            {
                if (!_cfg.SharedCraftInventory) return;
                if (!__instance.is_player) return;


                var worldZoneInventories = new List<Inventory>
                {
                    new(MainGame.me.player.data, "Player", string.Empty)
                };

                if (include_toolbelt)
                {
                    var data = new Item
                    {
                        inventory = MainGame.me.player.data.secondary_inventory,
                        inventory_size = 7
                    };
                    worldZoneInventories.Add(new Inventory(data, "Toolbelt", string.Empty));
                }

                foreach (var worldZoneDef in GameBalance.me.world_zones_data)
                {
                    if (!IncludedZones.Contains(worldZoneDef.id)) continue;
                    var worldZone = WorldZone.GetZoneByID(worldZoneDef.id);
                    if (worldZone == null) continue;
                    var worldZoneMulti = worldZone.GetMultiInventory(player_mi: MultiInventory.PlayerMultiInventory.ExcludePlayer, sortWGOS: true);
                    if (worldZoneMulti != null)
                    {
                        worldZoneInventories.AddRange(worldZoneMulti.Where(inv => inv != null).Where(inv => inv.data.inventory.Count > 0));
                    }
                }

                __result = new MultiInventory();
                __result.SetInventories(worldZoneInventories);
            }
        }

        [HarmonyPatch(typeof(InventoryWidget), nameof(InventoryWidget.FilterItems))]
        public static class InventoryWidgetFilterItemsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget __instance, ref InventoryWidget.ItemFilterDelegate filter_delegate, ref List<BaseItemCellGUI> ___items)
            {
                if (!_cfg.HideInvalidSelections) return;
                foreach (var baseItemCellGui in ___items)
                {
                    switch (filter_delegate(baseItemCellGui.item, __instance))
                    {
                        case InventoryWidget.ItemFilterResult.Active:
                            baseItemCellGui.SetGrayState(false);
                            break;
                            case InventoryWidget.ItemFilterResult.Inactive:
                            baseItemCellGui.Deactivate();
                            break;

                        case InventoryWidget.ItemFilterResult.Hide:
                            baseItemCellGui.Deactivate();
                            break;

                        case InventoryWidget.ItemFilterResult.Unknown:
                            baseItemCellGui.DrawUnknown();
                            break;
                    }
 
                }
                typeof(InventoryWidget).GetMethod("RecalculateWidgetSize", AccessTools.all)
                    ?.Invoke(__instance, new object[]
                    {
                    });

            }
        }
    }
}
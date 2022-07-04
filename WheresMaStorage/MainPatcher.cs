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
        private static bool _alreadyRun;

        private static WorldGameObject _wgo;
        // private static bool _talkingToNpc;

        private static readonly string[] WidgetsPartials =
        {
            "mf_stones",  "mf_ore",  "mf_timber",  "tavern", "refugee",  "storage",  "pump"
        };

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
            "players_tavern"
        };

        public static void Patch()
        {
            try
            {
                _cfg = Config.GetOptions();
                _alreadyRun = false;

                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.WheresMaStorage");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _cfg = Config.GetOptions();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WheresMaStorage]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
            }
        }

        [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
        public static class GameBalanceLoadGameBalancePatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (_alreadyRun) return;
                _alreadyRun = true;
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
                Debug.LogError($"[WMS]: Object: {__instance.obj_id}, Zone: {__instance.GetMyWorldZoneId()}");
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

        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class InventoryPanelGuiDoOpeningPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref InventoryPanelGUI __instance, ref MultiInventory multi_inventory)
            {
                Debug.LogError($"[WMS]: Panels: {__instance.name}, {__instance.gameObject.name}");
                if (__instance.name.Contains("vendor")) return;
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
            }

            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref List<UIWidget> ____separators, ref List<InventoryWidget> ____widgets, ref List<CustomInventoryWidget> ____custom_widgets)
            {
                if (__instance.name.Contains("vendor")) return;
                foreach (var sep in ____separators)
                {
                    sep.Hide();
                }

                foreach (var customWidget in ____custom_widgets)
                {
                    var id = customWidget.inventory_data.id;

                    if (WidgetsPartials.Any(id.Contains))
                    {
                        customWidget.Deactivate();
                    }
                }

                foreach (var inventoryWidget in ____widgets)
                {
                    var id = inventoryWidget.inventory_data.id;

                    {
                        if (WidgetsPartials.Any(id.Contains))
                        {
                            inventoryWidget.Deactivate();
                        }
                    }
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

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.GetMultiInventory))]
        public static class WorldGameObjectGetMultiInventoryPatch
        {
            [HarmonyPostfix]
            public static void Postfix(
                WorldGameObject __instance,
                ref MultiInventory __result,
                // List<WorldGameObject> exceptions = null,
                //string force_world_zone = "",
                //MultiInventory.PlayerMultiInventory player_mi = MultiInventory.PlayerMultiInventory.DontChange,
                bool include_toolbelt = false
                // bool sortWGOS = false,
                // bool include_bags = false
                )
            {
                if (__instance.obj_def.interaction_type != ObjectDefinition.InteractionType.Craft &&
                    !__instance.is_player) return;

                if (!_cfg.SharedCraftInventory) return;

                var worldZoneInventories = new List<Inventory> { new(MainGame.me.player.data, "Player", string.Empty) };

                if (include_toolbelt)
                {
                    var data = new Item
                    {
                        inventory = MainGame.me.player.data.secondary_inventory,
                        inventory_size = 7
                    };
                    worldZoneInventories.Add(new Inventory(data, "Toolbelt", string.Empty));
                }

                // foreach (var worldZoneDef in GameBalance.me.world_zones_data)
                foreach (var worldZoneDef in GameBalance.me.world_zones_data.Where(a => IncludedZones.Contains(a.id)))
                {
                    // Debug.LogError($"[WMS]: ZoneData: {worldZoneDef.id}");
                    var worldZone = WorldZone.GetZoneByID(worldZoneDef.id, false);
                    if (worldZone == null) continue;
                    var worldZoneMulti = worldZone.GetMultiInventory(player_mi: MultiInventory.PlayerMultiInventory.ExcludePlayer, sortWGOS: true);
                    if (worldZoneMulti == null) continue;
                    worldZoneInventories.AddRange(worldZoneMulti.Where(i => i != null && i.data.inventory.Count != 0));
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
                if (__instance.gameObject.transform.parent.transform.parent.transform.parent.name.Contains("vendor"))
                    return;
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
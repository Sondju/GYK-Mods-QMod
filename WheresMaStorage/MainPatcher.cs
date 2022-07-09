using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using WheresMaStorage.lang;

namespace WheresMaStorage
{
    public class MainPatcher
    {
        private const string Barman = "barman";
        private const string Chest = "chest";
        private const string Multi = "multi";
        private const string NpcBarman = "npc_tavern_barman";
        private const string Player = "player";
        private const string Refugee = "refugee";
        private const string Storage = "storage";
        private const string Tavern = "tavern";
        private const string TavernCellar = "tavern_cellar";
        private const string Vendor = "vendor";

        private static readonly string[] AlwaysHidePartials =
        {
            "refugee_camp_well", "refugee_camp_tent", "pump"
        };

        private static readonly ItemDefinition.ItemType[] GraveItems =
        {
            ItemDefinition.ItemType.GraveStone, ItemDefinition.ItemType.GraveFence, ItemDefinition.ItemType.GraveCover,
            ItemDefinition.ItemType.GraveStoneReq, ItemDefinition.ItemType.GraveFenceReq, ItemDefinition.ItemType.GraveCoverReq,
        };

        private static readonly string[] StockpileWidgetsPartials =
        {
            "mf_stones",  "mf_ore",  "mf_timber"
        };

        private static Config.Options _cfg;
        private static bool _gameBalanceAlreadyRun;

        private static int _invSize;
        private static WorldGameObject _previousWgo;
        private static WorldGameObject _wgo;
        private static string Lang { get; set; }

        public static void Patch()
        {
            try
            {
                _cfg = Config.GetOptions();
                _gameBalanceAlreadyRun = false;

                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.WheresMaStorage");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _cfg = Config.GetOptions();
                _invSize = 20 + _cfg.AdditionalInventorySpace;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WheresMaStorage]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
            }
        }

        private static void SetInventorySizeText(BaseInventoryWidget inventoryWidget, InventoryPanelGUI inventoryPanelGUI)
        {
            if (!_cfg.ShowWorldZoneInTitles && !_cfg.ShowUsedSpaceInTitles) return;

            string wzLabel;
            var subNameSplit = inventoryWidget.inventory_data.sub_name.Split('#');
            var objId = GJL.L(subNameSplit[0].ToLowerInvariant().Trim() + "_inventory");
            var zoneId = string.Empty;
            if (subNameSplit.Length > 1)
            {
                zoneId = subNameSplit[1].ToLowerInvariant().Trim();
            }
            // Debug.LogError($"[WMS]: Obj: {objId}, Zone: {zoneId}");
            if (inventoryWidget.inventory_data.sub_name.Length > 0)
            {
                var wzId = WorldZone.GetZoneByID(zoneId, false);
                wzLabel = wzId != null ? GJL.L("zone_" + wzId.id) : strings.Wilderness;
            }
            else
            {
                wzLabel = strings.Wilderness;
            }

            Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);

            var cultureInfo = Thread.CurrentThread.CurrentUICulture;
            var textInfo = cultureInfo.TextInfo;
            wzLabel = textInfo.ToTitleCase(wzLabel);

            var test = new Inventory(inventoryWidget.inventory_data.MakeInventoryCopy());
            var cap = test.size;
            var used = test.data.inventory.Count;

            //  var currentText = inventoryWidget.header_label.text.Split('-');
            inventoryWidget.header_label.overflowMethod = UILabel.Overflow.ResizeFreely;

            // currentText[0] = currentText[0].Trim();
            // var isPlayer = objId.ToLowerInvariant().Contains(Player) || objId.ToLowerInvariant().Contains("test");
            var isPlayer = inventoryPanelGUI.name.ToLowerInvariant().Contains(Player) || (inventoryPanelGUI.name.ToLowerInvariant().Contains(Multi) && _wgo == null);
            //   var header = string.Concat(isPlayer ? strings.Player : currentText[0], isPlayer ? $" - {used}/{cap}" : _cfg.ShowWorldZoneInTitles ? $" ({wzLabel}) - {used}/{cap}" : $" - {used}/{cap}");

            var header = isPlayer ? strings.Player : objId;

            if (_cfg.ShowWorldZoneInTitles && !isPlayer)
            {
                header = string.Concat(header, $" ({wzLabel})");
            }

            if (_cfg.ShowUsedSpaceInTitles)
            {
                header = string.Concat(header, $" - {used}/{cap}");
            }

            inventoryWidget.header_label.text = header;
        }

        //fixes not being able to craft paper based things i.e flyers due to stack size changes
        [HarmonyPatch(typeof(CraftDefinition), "takes_item_durability", MethodType.Getter)]
        public static class CraftDefinitionTakesItemDurabilityPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref CraftDefinition __instance, ref bool __result)
            {
                if (__instance == null) return;
                if (__instance.needs.Exists(item => item.id.Equals("pen:ink_pen")) && __instance.dur_needs_item > 0)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.CollectDrop))]
        public static class DropResGameObjectCollectDrop
        {
            //set stack size back up before collecting
            [HarmonyPrefix]
            public static void Prefix(ref DropResGameObject __instance)
            {
                if (!GraveItems.Contains(__instance.res.definition.type)) return;
                __instance.res.definition.stack_count = _cfg.StackSizeForStackables;
            }
        }

        [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.GetRemoveCraftForItem))]
        public static class GameBalanceGetRemoveCraftForItemPatch
        {
            //needed for grave removals to work
            [HarmonyPostfix]
            public static void Postfix(ref CraftDefinition __result)
            {
                foreach (var item in __result.output.Where(a => GraveItems.Contains(a.definition.type)))
                {
                    item.definition.stack_count = 1;
                }
            }
        }

        [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
        public static class GameBalanceLoadGameBalancePatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (_gameBalanceAlreadyRun) return;
                _gameBalanceAlreadyRun = true;
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

        [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.ApplyLanguageChange))]
        public static class GameSettingsApplyLanguageChange
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
            }
        }

        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class InventoryPanelGuiDoOpeningPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref List<UIWidget> ____separators, ref List<InventoryWidget> ____widgets, ref List<CustomInventoryWidget> ____custom_widgets)
            {
                Debug.LogError($"[WMS]: Panel Name: {__instance.name}");
                var isChest = __instance.name.ToLowerInvariant().Contains(Chest);
                var isVendor = __instance.name.ToLowerInvariant().Contains(Vendor);
                var isPlayer = __instance.name.ToLowerInvariant().Contains(Player) || (__instance.name.ToLowerInvariant().Contains(Multi) && _wgo == null);

                if ((_cfg.RemoveGapsBetweenSections && isPlayer) || (_cfg.RemoveGapsBetweenSectionsVendor && isVendor))
                {
                    foreach (var sep in ____separators)
                    {
                        sep.Hide();
                    }
                }

                if (isPlayer || isChest)
                {
                    foreach (var inventoryWidget in ____widgets)
                    {
                        SetInventorySizeText(inventoryWidget, __instance);
                    }
                }

                foreach (var customWidget in ____custom_widgets.Where(x => AlwaysHidePartials.Any(x.inventory_data.id.Contains)))
                {
                    customWidget.Deactivate();
                }

                foreach (var inventoryWidget in ____widgets.Where(x => AlwaysHidePartials.Any(x.inventory_data.id.Contains)))
                {
                    inventoryWidget.Deactivate();
                }

                if (isVendor || (_previousWgo != null && (_previousWgo.obj_id.ToLowerInvariant().Contains(Barman) || _previousWgo.obj_id.ToLowerInvariant().Contains(TavernCellar) || _previousWgo.obj_id.ToLowerInvariant().Contains(Refugee)))) return;

                foreach (var customWidget in from customWidget in ____custom_widgets let id = customWidget.inventory_data.id where (_cfg.HideRefugeeWidgets && id.Contains(Refugee)) || (_cfg.HideStockpileWidgets && StockpileWidgetsPartials.Any(id.Contains)) || (_cfg.HideTavernWidgets && id.Contains(Tavern)) || (_cfg.HideWarehouseShopWidgets && id.Contains(Storage)) select customWidget)
                {
                    customWidget.Deactivate();
                }

                foreach (var inventoryWidget in from inventoryWidget in ____widgets let id = inventoryWidget.inventory_data.id where (_cfg.HideRefugeeWidgets && id.Contains(Refugee)) || (_cfg.HideStockpileWidgets && StockpileWidgetsPartials.Any(id.Contains)) || (_cfg.HideTavernWidgets && id.Contains(Tavern)) || (_cfg.HideWarehouseShopWidgets && id.Contains(Storage)) select inventoryWidget)
                {
                    inventoryWidget.Deactivate();
                }
            }

            [HarmonyPrefix]
            public static void Prefix(ref InventoryPanelGUI __instance, ref MultiInventory multi_inventory)
            {
                var isVendor = __instance.name.ToLowerInvariant().Contains(Vendor);

                if (isVendor) return;

                if (_cfg.DontShowEmptyRowsInInventory)
                {
                    __instance.dont_show_empty_rows = true;
                }

                if (_cfg.ShowOnlyPersonalInventory || (_wgo != null && _wgo.obj_def.interaction_type is ObjectDefinition.InteractionType.Chest) || (_previousWgo != null && _previousWgo.obj_id.ToLowerInvariant().Contains(Barman)))
                {
                    var onlyMineInventory = new MultiInventory();
                    onlyMineInventory.AddInventory(multi_inventory.all[0]);
                    multi_inventory = onlyMineInventory;
                }
            }
        }

        [HarmonyPatch(typeof(InventoryPanelGUI), nameof(InventoryPanelGUI.SetGrayToNotMainWidgets))]
        public static class InventoryPanelGuiSetGrayToNotMainWidgets
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                return !_cfg.DisableInventoryDimming;
            }
        }

        [HarmonyPatch(typeof(InventoryWidget), nameof(InventoryWidget.FilterItems))]
        public static class InventoryWidgetFilterItemsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget __instance,
                ref InventoryWidget.ItemFilterDelegate filter_delegate, ref List<BaseItemCellGUI> ___items)
            {
                if (__instance.gameObject.transform.parent.transform.parent.transform.parent.name.ToLowerInvariant()
                    .Contains(Vendor))
                    return;

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

                var activeCount = ___items.Count(x => !x.is_inactive_state);

                if (activeCount <= 0 && !__instance.inventory_data.id.Contains(Player))
                {
                    __instance.Hide();
                }
                else
                {
                    typeof(InventoryWidget).GetMethod("RecalculateWidgetSize", AccessTools.all)
                        ?.Invoke(__instance, new object[]
                        {
                        });
                }
            }
        }

        [HarmonyPatch(typeof(InventoryPanelGUI), nameof(InventoryPanelGUI.Redraw))]
        public static class InventoryWidgetRedrawPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref List<InventoryWidget> ____widgets)
            {
                Debug.LogError($"[WMS]: ReDraw Hit");
                var isChest = __instance.name.ToLowerInvariant().Contains(Chest);
                var isPlayer = __instance.name.ToLowerInvariant().Contains(Player) || (__instance.name.ToLowerInvariant().Contains(Multi) && _wgo == null);
                if ((isPlayer || isChest) && _cfg.ShowUsedSpaceInTitles)
                {
                    foreach (var inventoryWidget in ____widgets.Where(a=>!a.header_label.text.ToLowerInvariant().Contains("shipping")))
                    {
                        SetInventorySizeText(inventoryWidget, __instance);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.GetMultiInventory))]
        public static class WorldGameObjectGetMultiInventoryPatch
        {
            [HarmonyPostfix]
            public static void Postfix(
                WorldGameObject __instance,
                ref MultiInventory __result,
                bool include_toolbelt = false
            )
            {
                if (__instance.obj_def.interaction_type != ObjectDefinition.InteractionType.Craft &&
                    !__instance.is_player) return;

                if (!_cfg.SharedCraftInventory) return;

                var worldZoneInventories = new List<Inventory>();
                var playerInv = new Inventory(MainGame.me.player.data, "Player", string.Empty);
                playerInv.data.SetInventorySize(_invSize);
                worldZoneInventories.Add(playerInv);

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
                    var worldZone = WorldZone.GetZoneByID(worldZoneDef.id, false);
                    if (worldZone == null) continue;
                    var worldZoneMulti = worldZone.GetMultiInventory(player_mi: MultiInventory.PlayerMultiInventory.ExcludePlayer, sortWGOS: true);
                    if (worldZoneMulti == null) continue;
                    foreach (var inv in worldZoneMulti.Where(inv => inv != null))
                    {
                        inv.data.sub_name = inv._obj_id + "#" + worldZoneDef.id;
                        Debug.Log($"[WheresMaStorage]: Inventory ObjectID: {inv._obj_id}, WorldZoneID: {worldZoneDef.id}, WorldZone: {GJL.L("zone_" + worldZoneDef.id)}");
                    }

                    worldZoneInventories.AddRange(worldZoneMulti.Where(i => i != null && i.data.inventory.Count != 0));
                }

                __result = new MultiInventory();
                __result.SetInventories(worldZoneInventories);
                
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), "InitNewObject")]
        public static class WorldGameObjectInitPatch
        {
            [HarmonyPostfix]
            public static void Postfix(WorldGameObject __instance)
            {
                if (__instance.is_player)
                {
                    __instance.data.SetInventorySize(_invSize);
                }
                if (string.Equals(__instance.obj_id, NpcBarman))
                {
                    __instance.data.SetInventorySize(__instance.obj_def.inventory_size + _cfg.AdditionalInventorySpace);
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPostfix]
            public static void Postfix(WorldGameObject __instance)
            {
                if (!MainGame.game_started || __instance == null) return;

                _wgo = null;
            }

            [HarmonyPrefix]
            public static void Prefix(WorldGameObject __instance, WorldGameObject other_obj)
            {
                if (!MainGame.game_started || __instance == null) return;

                if (other_obj == MainGame.me.player)
                {
                    _wgo = __instance;
                }

                _previousWgo = __instance;
            }
        }
    }
}
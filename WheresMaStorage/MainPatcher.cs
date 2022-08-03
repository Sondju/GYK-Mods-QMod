using HarmonyLib;
using Helper;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using WheresMaStorage.lang;

namespace WheresMaStorage
{
    [HarmonyAfter("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs")]
    public class MainPatcher
    {
        private const string Barman = "barman";
        private const string Chest = "chest";

        //private const string Church = "church_pulpit";
        //private const string Grindstone = "grindstone";
        private const string Multi = "multi";

        private const string NpcBarman = "npc_tavern_barman";
        private const string Player = "player";
        private const string Refugee = "refugee";
        private const string Storage = "storage";
        private const string Tavern = "tavern";
        private const string Writer = "writer";
        private const string TavernCellar = "tavern_cellar";
        private const string Vendor = "vendor";
        private const string Gerry = "gerry";

        private static readonly string[] ZoneExclusions =
        {
            "slava_test",
            "morgue_outside",
            "cabinet",
            "farm",
            "hill",
            "tavern",
            "vilage",
            "flat_under_waterflow",
            "flat_under_waterflow_2",
            "flat_under_waterflow_3",
            "swamp",
            "witch_hut",
            "wheat_land",
            "beatch",
            "forest_under_village",
            "east_border",
            "sealight",
            "camp",
            "marble_deposit",
            "burned_house",
            "nountain_fort",
            "cliff",
            "cellar_storage",
            //"refugees_camp",
            "euric_room",
            "alarich_tent_inside"
        };

        private static readonly string[] AlwaysHidePartials =
        {
            "refugee_camp_well", "refugee_camp_tent", "pump", "pallet"
        };

        private static readonly string[] MakeStackable =
        {
            "book","chapter"
        };

        private static readonly ItemDefinition.ItemType[] GraveItems =
        {
            ItemDefinition.ItemType.GraveStone, ItemDefinition.ItemType.GraveFence, ItemDefinition.ItemType.GraveCover,
            ItemDefinition.ItemType.GraveStoneReq, ItemDefinition.ItemType.GraveFenceReq, ItemDefinition.ItemType.GraveCoverReq,
        };

        private static readonly ItemDefinition.ItemType[] ToolItems =
        {
            ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
            ItemDefinition.ItemType.Pickaxe, ItemDefinition.ItemType.FishingRod, ItemDefinition.ItemType.BodyArmor,
            ItemDefinition.ItemType.HeadArmor, ItemDefinition.ItemType.Sword, ItemDefinition.ItemType.Preach,
        };

        private static readonly string[] StockpileWidgetsPartials =
                {
            "mf_stones",  "mf_ore",  "mf_timber"
        };

        private static Config.Options _cfg;

        private static bool _gameBalanceAlreadyRun;

        private static int _invSize;

        private static bool _isChest, _isBarman, _isTavernCellar, _isRefugee, _isCraft, _isWritersTable, _isVendor, _zombieWorker;//, _playerInteraction;//, _isChurchPulpit, _isGrindstone

        //private static readonly List<Inventory> ResourceCraftInventories = new();
        //private static readonly List<Inventory> WorldInventories = new();
        private static MultiInventory _mi = new();

        private static WorldGameObject _wgo, _previousWgo;

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
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        private static void Log(string message, bool error = false)
        {
            if (_cfg.Debug || error)
            {
                Tools.Log("WheresMaStorage", $"{message}", error);
            }
        }

        private static void SetInventorySizeText(BaseInventoryWidget inventoryWidget)
        {
            if (inventoryWidget.inventory_data.id.Contains(Writer)) return;
            if (inventoryWidget.header_label.text.Contains(Gerry)) return;
            if (!_cfg.ShowWorldZoneInTitles && !_cfg.ShowUsedSpaceInTitles) return;

            string wzLabel;
            string objId;
            bool isPlayer;
            var subNameSplit = inventoryWidget.inventory_data.sub_name.Split('#');
            if (string.IsNullOrEmpty(subNameSplit[0]))
            {
                objId = strings.Player;
                isPlayer = true;
            }
            else
            {
                objId = GJL.L(subNameSplit[0].ToLowerInvariant().Trim() + "_inventory");
                isPlayer = false;
            }

            var zoneId = string.Empty;
            if (subNameSplit.Length > 1)
            {
                zoneId = subNameSplit[1].ToLowerInvariant().Trim();
            }

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

            inventoryWidget.header_label.overflowMethod = UILabel.Overflow.ResizeFreely;

            var header = objId;

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

        private static void SetOthersFalse()
        {
            _isVendor = false;
            _isCraft = false;
            _isChest = false;
            _isBarman = false;
            _isTavernCellar = false;
            _isRefugee = false;
            _isWritersTable = false;
            //_playerInteraction = false;
            //_isChurchPulpit = false;
            //_isGrindstone = false;
        }

        //this method gets inserted into the CraftReally method using the transpiler below, overwriting any inventory the game sets. It only effects zombie requests.
        public static MultiInventory GetMi(CraftDefinition craft, MultiInventory orig, WorldGameObject otherGameObject)
        {
            if (!Tools.TutorialDone()) return orig;
            if ((otherGameObject.has_linked_worker && otherGameObject.linked_worker.obj_id.Contains("zombie")) || otherGameObject.obj_id.Contains("zombie") || otherGameObject.obj_id.StartsWith("mf_"))
            {
                Log($"[InvRedirect]: Redirected craft inventory to player MultiInventory! Object: {otherGameObject.obj_id}, Craft: {craft.id}");
                _zombieWorker = true;
                return _mi;
            }

            Log($"[InvRedirect]: Original inventory sent back to requester! Object: {otherGameObject.obj_id}, Craft: {craft.id}");
            _zombieWorker = false;
            return orig;
        }

        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(CraftComponent), "CraftReally")]
        public static class CraftComponentPatch
        {
            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if (!Tools.TutorialDone()) return codes.AsEnumerable();
                if (!_cfg.SharedCraftInventory) return codes.AsEnumerable();
                var usedMultiField = AccessTools.Field(typeof(CraftComponent), "used_multi_inventory");
                var otherObj = AccessTools.Field(typeof(CraftComponent), "other_obj");
                var miGetter = typeof(MainPatcher).GetMethod("GetMi");
                var insertIndex = -1;
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("item_needs") && codes[i - 1].opcode == OpCodes.Ldarg_1)
                    {
                        insertIndex = i;
                        break;
                    }
                }
                var newCodes = new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldarg_1),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, usedMultiField),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, otherObj),
                    new(OpCodes.Call, miGetter),
                    new(OpCodes.Stfld, usedMultiField)
                };
                if (insertIndex != -1)
                {
                    codes.InsertRange(insertIndex, newCodes);
                }

                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(AutopsyGUI))]
        public static class AutopsyGuiPatch
        {
            internal static IEnumerable<MethodBase> TargetMethods()
            {
                var inner = typeof(AutopsyGUI).GetNestedType("<>c__DisplayClass23_0", AccessTools.all)
                            ?? throw new Exception("Inner Not Found");

                foreach (var method in inner.GetMethods(AccessTools.all))
                {
                    if (method.Name.Contains("<OnBodyItemPress>") && method.GetParameters().Length == 2)
                    {
                        yield return method;
                    }
                }
            }

            [HarmonyTranspiler]
            [CanBeNull]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
            {
                var instructionsList = new List<CodeInstruction>(instructions);
                if (!Tools.TutorialDone()) return instructionsList.AsEnumerable();
                if (!_cfg.HideInvalidSelections) return instructionsList.AsEnumerable();
                instructionsList[5].opcode = OpCodes.Ldc_I4_1;

                return instructionsList.AsEnumerable();

                //var str = string.Empty;
                //var codes = new List<CodeInstruction>(instructions);
                //for (int i = 0; i < codes.Count; i++)
                //{
                //    str += ($"IL:[{i}] - {codes[i].opcode} - {codes[i].operand}\n");
                //}
                //File.WriteAllText("./qmods/il.txt", str);
                //return codes.AsEnumerable();
            }
        }

        //some crafting objects re-acquire the inventories when starting a craft, overwriting our multi.This stops that.
        [HarmonyPatch(typeof(BaseCraftGUI), "multi_inventory", MethodType.Getter)]
        public static class BaseCraftGuiMiGetterPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref BaseCraftGUI __instance, ref MultiInventory __result)
            {
                // //if (!Tools.TutorialDone()) return;
                if (!_zombieWorker)
                {
                    Log($"[BaseCraftGUI.multi_inventory (Getter)]: {__instance.name}, Craftery: {__instance.GetCrafteryWGO().obj_id}");
                }
                __result = _mi;
            }
        }

        [HarmonyBefore("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs")]
        [HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.CollectDrop))]
        public static class DropResGameObjectCollectDrop
        {
            //set stack size back up before collecting
            [HarmonyPrefix]
            public static void Prefix(ref DropResGameObject __instance)
            {
                ////if (!Tools.TutorialDone()) return;
                if (!GraveItems.Contains(__instance.res.definition.type)) return;
                __instance.res.definition.stack_count = _cfg.StackSizeForStackables;
            }
        }

        [HarmonyBefore("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs")]
        [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.GetRemoveCraftForItem))]
        public static class GameBalanceGetRemoveCraftForItemPatch
        {
            //needed for grave removals to work
            [HarmonyPostfix]
            public static void Postfix(ref CraftDefinition __result)
            {
                ////if (!Tools.TutorialDone()) return;
                foreach (var item in __result.output.Where(a => GraveItems.Contains(a.definition.type)))
                {
                    item.definition.stack_count = 1;
                }
            }
        }

        [HarmonyBefore("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs")]
        [HarmonyPatch(typeof(CraftDefinition), "takes_item_durability", MethodType.Getter)]
        public static class CraftDefinitionTakesItemDurabilityPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref CraftDefinition __instance, ref bool __result)
            {
                if (!_cfg.EnableChiselInkStacking) return;
                if (__instance == null) return;
                if (__instance.needs.Exists(item => item.id.Equals("pen:ink_pen")) && __instance.dur_needs_item > 0)
                {
                    __result = false;
                }
                if (__instance.needs.Exists(item => item.id.Contains("chisel")) && __instance.dur_needs_item > 0)
                {
                    __result = false;
                }
            }
        }

        [HarmonyBefore("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs")]
        [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
        public static class GameBalanceLoadGameBalancePatch
        {
            [HarmonyBefore("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs")]
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (_gameBalanceAlreadyRun) return;
                _gameBalanceAlreadyRun = true;

                if (_cfg.AllowHandToolDestroy)
                {
                    foreach (var itemDef in GameBalance.me.items_data.Where(a => ToolItems.Contains(a.type)))
                    {
                        itemDef.player_cant_throw_out = false;
                    }
                }

                if (_cfg.EnableToolAndPrayerStacking || _cfg.EnableChiselInkStacking)
                {
                    foreach (var item in GameBalance.me.items_data.Where(item => item.stack_count == 1))
                    {
                        if (_cfg.EnableToolAndPrayerStacking)
                        {
                            if (ToolItems.Contains(item.type) || GraveItems.Contains(item.type) ||
                                MakeStackable.Any(item.id.Contains))
                            {
                                item.stack_count = item.stack_count + _cfg.StackSizeForStackables > 999 ? 999 : _cfg.StackSizeForStackables;
                            }
                        }

                        if (!_cfg.EnableChiselInkStacking) continue;

                        if (item.id.Contains("ink") || item.id.Contains("pen") || item.id.Contains("chisel"))
                        {
                            item.stack_count = item.stack_count + _cfg.StackSizeForStackables > 999 ? 999 : _cfg.StackSizeForStackables;
                        }
                    }
                }

                if (_cfg.ModifyInventorySize)
                {
                    foreach (var od in GameBalance.me.objs_data.Where(od =>
                                 od.interaction_type == ObjectDefinition.InteractionType.Chest))
                    {
                        od.inventory_size += _cfg.AdditionalInventorySpace;
                    }
                }

                if (!_cfg.ModifyStackSize) return;

                foreach (var id in GameBalance.me.items_data.Where(id => id.stack_count is > 1 and <= 999))
                {
                    id.stack_count = id.stack_count + _cfg.StackSizeForStackables > 999 ? 999 : _cfg.StackSizeForStackables;
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

        [HarmonyPatch(typeof(GraveGUI), "GravePartsFilter", typeof(Item), typeof(ItemDefinition.ItemType))]
        public static class GraveGuiGravePartsFilterPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget.ItemFilterResult __result)
            {
                ////if (!Tools.TutorialDone()) return;
                if (!MainGame.game_started) return;
                if (!_cfg.HideInvalidSelections) return;

                if (__result != InventoryWidget.ItemFilterResult.Active)
                {
                    __result = InventoryWidget.ItemFilterResult.Inactive;
                }
            }
        }

        [HarmonyPatch(typeof(MixedCraftGUI), "AlchemyItemPickerFilter", typeof(Item), typeof(InventoryWidget))]
        public static class MixedCraftGuiAlchemyItemPickerFilterPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget.ItemFilterResult __result)
            {
                // //if (!Tools.TutorialDone()) return;
                if (!MainGame.game_started) return;
                if (!_cfg.HideInvalidSelections) return;

                if (__result != InventoryWidget.ItemFilterResult.Active)
                {
                    __result = InventoryWidget.ItemFilterResult.Inactive;
                }
            }
        }

        [HarmonyPatch(typeof(SoulContainerWidget), "SoulItemsFilter", typeof(Item))]
        public static class SoulContainerWidgetSoulItemsFilterPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget.ItemFilterResult __result)
            {
                ////if (!Tools.TutorialDone()) return;
                if (!MainGame.game_started) return;
                if (!_cfg.HideInvalidSelections) return;

                if (__result != InventoryWidget.ItemFilterResult.Active)
                {
                    __result = InventoryWidget.ItemFilterResult.Inactive;
                }
            }
        }

        [HarmonyPatch(typeof(SoulHealingWidget), "SoulItemsFilter", typeof(Item))]
        public static class SoulHealingWidgetSoulItemsFilterPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget.ItemFilterResult __result)
            {
                // //if (!Tools.TutorialDone()) return;
                if (!MainGame.game_started) return;
                if (!_cfg.HideInvalidSelections) return;

                if (__result != InventoryWidget.ItemFilterResult.Active)
                {
                    __result = InventoryWidget.ItemFilterResult.Inactive;
                }
            }
        }

        [HarmonyPatch(typeof(BaseGUI), "Hide", typeof(bool))]
        public static class InGameMenuGuiOpenPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                ////if (!Tools.TutorialDone()) return;
                if (BaseGUI.all_guis_closed)
                {
                    SetOthersFalse();
                }
            }
        }

        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class InventoryPanelGuiDoOpeningPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref MultiInventory multi_inventory, ref List<UIWidget> ____separators, ref List<InventoryWidget> ____widgets, ref List<CustomInventoryWidget> ____custom_widgets)
            {
                // //if (!Tools.TutorialDone()) return;
                var isChestPanel = __instance.name.ToLowerInvariant().Contains(Chest);
                var isVendorPanel = __instance.name.ToLowerInvariant().Contains(Vendor);
                var isPlayerPanel = __instance.name.ToLowerInvariant().Contains(Player) || (__instance.name.ToLowerInvariant().Contains(Multi) && _wgo == null);
                var isResourcePanelProbably = !isChestPanel && !isVendorPanel && !isPlayerPanel;

                //foreach (var inventoryWidget in ____widgets)
                //{
                //    Log($"[InventoryWidget DoOpening Postfix]: InvID: {inventoryWidget.inventory_data.id}, HeaderText: {inventoryWidget.header_label.text}, HeaderPrintedText: {inventoryWidget.header_label.printedText}");
                //}

                if ((_cfg.RemoveGapsBetweenSections && isPlayerPanel) || (_cfg.RemoveGapsBetweenSectionsVendor && isVendorPanel) || isResourcePanelProbably)
                {
                    foreach (var sep in ____separators)
                    {
                        sep.Hide();
                    }
                }

                if (isResourcePanelProbably || isPlayerPanel || isChestPanel)
                {
                    foreach (var inventoryWidget in ____widgets)
                    {
                        //Log($"[InventoryWidget]: InvID: {inventoryWidget.inventory_data.id}, HeaderText: {inventoryWidget.header_label.text}, HeaderPrintedText: {inventoryWidget.header_label.printedText}");
                        SetInventorySizeText(inventoryWidget);
                    }
                }

                if (isResourcePanelProbably)
                {
                    foreach (var customWidget in ____custom_widgets)
                    {
                        customWidget.Deactivate();
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

                if (isVendorPanel || _isBarman || _isTavernCellar || _isRefugee) return;
                foreach (var customWidget in from customWidget in ____custom_widgets let id = customWidget.inventory_data.id where (_cfg.HideRefugeeWidgets && id.Contains(Refugee)) || (_cfg.HideStockpileWidgets && StockpileWidgetsPartials.Any(id.Contains)) || (_cfg.HideTavernWidgets && id.Contains(Tavern)) || (_cfg.HideWarehouseShopWidgets && id.Contains(Storage)) select customWidget)
                {
                    customWidget.Deactivate();
                }

                foreach (var inventoryWidget in from inventoryWidget in ____widgets let id = inventoryWidget.inventory_data.id where (_cfg.HideRefugeeWidgets && id.Contains(Refugee)) || (_cfg.HideStockpileWidgets && StockpileWidgetsPartials.Any(id.Contains)) || (_cfg.HideTavernWidgets && id.Contains(Tavern)) || (_cfg.HideWarehouseShopWidgets && id.Contains(Storage)) select inventoryWidget)
                {
                    if (!inventoryWidget.inventory_data.id.Contains(Writer))
                    {
                        inventoryWidget.Deactivate();
                    }
                }
            }

            [HarmonyPrefix]
            public static void Prefix(ref InventoryPanelGUI __instance, ref MultiInventory multi_inventory)
            {
                // //if (!Tools.TutorialDone()) return;
                var isVendorPanel = __instance.name.ToLowerInvariant().Contains(Vendor);
                // Log($"Barman:{_isBarman}, Cellar:{_isTavernCellar}, Refugee:{_isRefugee}, Chest:{_isChest}, Vendor:{isVendor}");
                if (isVendorPanel) return;

                if (_cfg.DontShowEmptyRowsInInventory)
                {
                    __instance.dont_show_empty_rows = true;
                }

                if (_isCraft || _isVendor) return;

                if (_cfg.ShowOnlyPersonalInventory || _isBarman || _isTavernCellar || _isRefugee || _isChest || _isWritersTable)
                {
                    // Log($"[InventoryPanelGUI.DoOpening-Prefix]: Panel: {__instance.name}, _isBarman: {_isBarman}, _isTavernCellar: {_isTavernCellar}, _isRefugee: {_isRefugee}, _isChest: {_isChest}, _isVendor: {_isVendor}");

                    var onlyMineInventory = new MultiInventory();
                    onlyMineInventory.AddInventory(multi_inventory.all[0]);
                    multi_inventory = onlyMineInventory;
                }
            }
        }

        [HarmonyPatch(typeof(InventoryPanelGUI), nameof(InventoryPanelGUI.Redraw))]
        public static class InventoryPanelGuiRedrawPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref List<InventoryWidget> ____widgets)
            {
                ////if (!Tools.TutorialDone()) return;
                if (MainGame.me.save.IsInTutorial()) return;
                var isChest = __instance.name.ToLowerInvariant().Contains(Chest);
                var isPlayer = __instance.name.ToLowerInvariant().Contains(Player) || (__instance.name.ToLowerInvariant().Contains(Multi) && _wgo == null);

                //foreach (var inventoryWidget in ____widgets)
                //{
                //    Log($"[InventoryWidget Redraw Postfix]: InvID: {inventoryWidget.inventory_data.id}, HeaderText: {inventoryWidget.header_label.text}, HeaderPrintedText: {inventoryWidget.header_label.printedText}");
                //}

                if ((isPlayer || isChest) && _cfg.ShowUsedSpaceInTitles)
                {
                    foreach (var inventoryWidget in ____widgets)
                    {
                        // Log($"[InventoryWidget Redraw]: InvID: {inventoryWidget.inventory_data.id}, HeaderText: {inventoryWidget.header_label.text}, HeaderPrintedText: {inventoryWidget.header_label.printedText}");
                        SetInventorySizeText(inventoryWidget);
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
                if (!Tools.TutorialDone()) return true;
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
                ////if (!Tools.TutorialDone()) return;

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

                typeof(InventoryWidget).GetMethod("RecalculateWidgetSize", AccessTools.all)
                    ?.Invoke(__instance, new object[]
                    {
                    });
            }
        }

        [HarmonyPatch(typeof(RatCellGUI))]
        public static class RatCellGuiPatch
        {
            internal static IEnumerable<MethodBase> TargetMethods()
            {
                var inner = typeof(RatCellGUI).GetNestedType("<>c", AccessTools.all)
                            ?? throw new Exception("Inner Not Found");

                foreach (var method in inner.GetMethods(AccessTools.all))
                {
                    if (method.Name.Contains("<OnRatInsertionButtonPressed>") && method.GetParameters().Length == 2)
                    {
                        yield return method;
                    }
                }
            }

            [HarmonyTranspiler]
            [CanBeNull]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
            {
                var instructionsList = new List<CodeInstruction>(instructions);
                if (!Tools.TutorialDone()) return instructionsList.AsEnumerable();
                if (!_cfg.HideInvalidSelections) return instructionsList.AsEnumerable();
                instructionsList[5].opcode = OpCodes.Ldc_I4_1;

                return instructionsList.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(OrganEnhancerGUI))]
        public static class OrganEnhancerGuiPatch
        {
            internal static IEnumerable<MethodBase> TargetMethods()
            {
                var inner = typeof(OrganEnhancerGUI).GetNestedType("<>c", AccessTools.all)
                            ?? throw new Exception("Inner Not Found");

                foreach (var method in inner.GetMethods(AccessTools.all))
                {
                    if (method.Name.Contains("<OnItemSelect>") && method.GetParameters().Length == 2)
                    {
                        yield return method;
                    }
                }
            }

            [HarmonyTranspiler]
            [CanBeNull]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen, MethodBase original)
            {
                var instructionsList = new List<CodeInstruction>(instructions);
                if (!Tools.TutorialDone()) return instructionsList.AsEnumerable();
                if (!_cfg.HideInvalidSelections) return instructionsList.AsEnumerable();
                instructionsList[5].opcode = OpCodes.Ldc_I4_1;
                instructionsList[49].opcode = OpCodes.Ldc_I4_1;

                return instructionsList.AsEnumerable();
            }
        }

        //[HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.GetMultiInventory))]
        //public static class WorldGameObjectGetMultiInventoryPatch
        //{
        //    [HarmonyPostfix]
        //    public static void Postfix(
        //        WorldGameObject __instance,
        //        ref MultiInventory __result,
        //        bool include_toolbelt = false
        //    )

        //    {
        //        ////if (!Tools.TutorialDone()) return;
        //        _zombieWorker = (__instance.has_linked_worker && __instance.linked_worker.obj_id.Contains("zombie")) || __instance.obj_def.id.Contains("zombie");

        //        if (!_cfg.SharedCraftInventory) return;

        //        if (__instance.vendor != null)
        //        {
        //            if (!_zombieWorker)
        //            {
        //                Log(
        //                    $"[WorldGameObject.GetMultiInventory-Postfix]: REJECTED: __instance.vendor = {__instance.vendor.id} ");
        //            }

        //            return;
        //        }

        //        if (!_zombieWorker)
        //        {
        //            if (__instance.obj_def.IsNPC())
        //            {
        //                Log(
        //                    $"[WorldGameObject.GetMultiInventory-Postfix]: REJECTED: __instance.obj_def.IsNPC {__instance.obj_def.id} ");
        //                return;
        //            }
        //        }

        //        if (_cfg.CacheEligibleInventories && _mi.all.Count > 0)
        //        {
        //            //if (!_zombieWorker)
        //            //{
        //            //    Log(
        //            //        $"[WorldGameObject.GetMultiInventory-Postfix]: Cached __instance: {__instance.obj_id}, _previousWgo: {_previousWgo.obj_id}");
        //            //}

        //            if (__instance.is_player)
        //            {
        //                if (!_zombieWorker)
        //                {
        //                    Log(
        //                        $"[WorldGameObject.GetMultiInventory-Postfix]: Sending cached inventory to Player: {__instance.obj_id}");
        //                }

        //                var pMi = new MultiInventory();
        //                var newInv = _mi.all.Where(a => !a.name.Contains("Toolbelt")).ToList();
        //                pMi.SetInventories(newInv);
        //                //instance = pMi;
        //                __result = pMi;

        //                return;
        //            }

        //            if (__instance == _previousWgo || __instance.obj_id.StartsWith("mf_"))
        //            {
        //                Log(
        //                    $"[WorldGameObject.GetMultiInventory-Postfix]: _previousWgo == __instance. Sending cache: {__instance.obj_id}");

        //                __result = _mi;
        //                return;
        //            }
        //        }

        //        if (__instance.is_player || __instance == _wgo || _zombieWorker || __instance.obj_id.StartsWith("mf_"))
        //        {
        //            _previousWgo = __instance;
        //            _mi = new MultiInventory();
        //            var playerInv = new Inventory(MainGame.me.player.data, "Player", string.Empty);
        //            playerInv.data.SetInventorySize(_invSize);

        //            _mi.AddInventory(playerInv);

        //            if (include_toolbelt)
        //            {
        //                var data = new Item
        //                {
        //                    inventory = MainGame.me.player.data.secondary_inventory,
        //                    inventory_size = 7
        //                };
        //                _mi.AddInventory(new Inventory(data, "Toolbelt", ""), -1);
        //            }

        //            foreach (var worldZoneDef in GameBalance.me.world_zones_data)
        //            {
        //                var worldZone = WorldZone.GetZoneByID(worldZoneDef.id, false);
        //                if (worldZone == null) continue;

        //                if (ZoneExclusions.Contains(worldZone.id)) continue;
        //                var worldZoneMulti =
        //                    worldZone.GetMultiInventory(player_mi: MultiInventory.PlayerMultiInventory.ExcludePlayer,
        //                        sortWGOS: true);
        //                if (worldZoneMulti == null) continue;
        //                foreach (var inv in worldZoneMulti.Where(inv => inv != null))// && inv.data.inventory.Count != 0))
        //                {
        //                    if (!_cfg.IncludeRefugeeDepot)
        //                    {
        //                        if (worldZone.id.ToLowerInvariant().Contains("refugee"))
        //                        {
        //                            if (inv.data.id.ToLowerInvariant().Contains("depot")) continue;
        //                            //Log($"[RefugeeInv]: Zone: {worldZone.id}, Inv: {inv.data.id}");
        //                        }
        //                    }

        //                    inv.data.sub_name = inv._obj_id + "#" + worldZoneDef.id;
        //                    _mi.AddInventory(inv);
        //                }
        //            }

        //            if (!_zombieWorker)
        //            {
        //                Log(
        //                    $"[WorldGameObject.GetMultiInventory-Postfix]: Sending non-cached to __instance: {__instance.obj_id}, isPlayer: {__instance.is_player}, _previousWgo: {_previousWgo.obj_id}, Zombie: {_zombieWorker}");
        //            }

        //            //instance = _mi;
        //            __result = _mi;
        //        }
        //    }
        //}

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
                ////if (!Tools.TutorialDone()) return;
                _zombieWorker = (__instance.has_linked_worker && __instance.linked_worker.obj_id.Contains("zombie")) || __instance.obj_def.id.Contains("zombie");

                if (!_cfg.SharedCraftInventory) return;

                if (__instance.vendor != null)
                {
                    if (!_zombieWorker)
                    {
                        Log(
                            $"[WorldGameObject.GetMultiInventory-Postfix]: REJECTED: __instance.vendor = {__instance.vendor.id} ");
                    }

                    return;
                }

                if (!_zombieWorker)
                {
                    if (__instance.obj_def.IsNPC())
                    {
                        Log(
                            $"[WorldGameObject.GetMultiInventory-Postfix]: REJECTED: __instance.obj_def.IsNPC {__instance.obj_def.id} ");
                        return;
                    }
                }

                var craftery = GUIElements.me.craft.GetCrafteryWGO();
                if (_cfg.CacheEligibleInventories && _mi.all.Count > 0)
                {
     
                    if (craftery != null || __instance.obj_id.StartsWith("mf_"))
                    {
                        Log(
                            $"[WorldGameObject.GetMultiInventory-Postfix]: Sending cache: {__instance.obj_id}");

                        __result = _mi;
                        return;
                    }
                }

                if (__instance.is_player || craftery != null || _zombieWorker || __instance.obj_id.StartsWith("mf_"))
                {
                    _previousWgo = __instance;
                    _mi = new MultiInventory();
                    var playerInv = new Inventory(MainGame.me.player.data, "Player", string.Empty);
                    playerInv.data.SetInventorySize(_invSize);

                    _mi.AddInventory(playerInv);

                    if (include_toolbelt)
                    {
                        var data = new Item
                        {
                            inventory = MainGame.me.player.data.secondary_inventory,
                            inventory_size = 7
                        };
                        _mi.AddInventory(new Inventory(data, "Toolbelt", ""), -1);
                    }

                    foreach (var worldZoneDef in GameBalance.me.world_zones_data)
                    {
                        var worldZone = WorldZone.GetZoneByID(worldZoneDef.id, false);
                        if (worldZone == null) continue;

                        if (ZoneExclusions.Contains(worldZone.id)) continue;
                        var worldZoneMulti =
                            worldZone.GetMultiInventory(player_mi: MultiInventory.PlayerMultiInventory.ExcludePlayer,
                                sortWGOS: true);
                        if (worldZoneMulti == null) continue;
                        foreach (var inv in worldZoneMulti.Where(inv => inv != null))// && inv.data.inventory.Count != 0))
                        {
                            if (!_cfg.IncludeRefugeeDepot)
                            {
                                if (worldZone.id.ToLowerInvariant().Contains("refugee"))
                                {
                                    if (inv.data.id.ToLowerInvariant().Contains("depot")) continue;
                                    //Log($"[RefugeeInv]: Zone: {worldZone.id}, Inv: {inv.data.id}");
                                }
                            }

                            inv.data.sub_name = inv._obj_id + "#" + worldZoneDef.id;
                            _mi.AddInventory(inv);
                        }
                    }

                    if (!_zombieWorker)
                    {
                        Log(
                            $"[WorldGameObject.GetMultiInventory-Postfix]: Sending non-cached to __instance: {__instance.obj_id}, isPlayer: {__instance.is_player}, _previousWgo: {_previousWgo.obj_id}, Zombie: {_zombieWorker}");
                    }

                    //instance = _mi;
                    __result = _mi;
                }
            }
        }

        // private static MultiInventory instance;

        [HarmonyPatch(typeof(WorldGameObject), "InitNewObject")]
        public static class WorldGameObjectInitPatch
        {
            [HarmonyPostfix]
            public static void Postfix(WorldGameObject __instance)
            {
                // //if (!Tools.TutorialDone()) return;
                if (!_cfg.ModifyInventorySize) return;

                if (__instance.is_player)
                {
                    __instance.data.SetInventorySize(_invSize);
                }

                if (string.Equals(__instance.obj_id, NpcBarman))
                {
                    __instance.data.SetInventorySize(__instance.obj_def.inventory_size +
                                                     _cfg.AdditionalInventorySpace);
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPrefix]
            public static void Prefix(WorldGameObject __instance, WorldGameObject other_obj)
            {
                ////if (!Tools.TutorialDone()) return;
                if (!MainGame.game_started || __instance == null) return;

                _previousWgo = _wgo;
                _wgo = __instance;

                //Log($"Instance: {__instance.obj_id}, InstanceIsPlayer: {__instance.is_player}, OtherObj: {other_obj.obj_id}, OtherIsPlayer: {other_obj.is_player}, InstanceInteractionType: {__instance.obj_def.interaction_type}, OtherObjInteractionType: {other_obj.obj_def.interaction_type}, InstanceHasCraft: {__instance.obj_def.has_craft}, InstanceCraftPreset: {__instance.obj_def.craft_preset},, InstanceScript: {__instance.obj_def.attached_script}");
                _isVendor = __instance.vendor != null;
                _isCraft = other_obj.is_player && __instance.obj_def.interaction_type != ObjectDefinition.InteractionType.Chest && __instance.obj_def.has_craft;
                _isChest = __instance.obj_def.interaction_type == ObjectDefinition.InteractionType.Chest;
                _isBarman = __instance.obj_id.ToLowerInvariant().Contains(Barman);
                _isTavernCellar = __instance.obj_id.ToLowerInvariant().Contains(TavernCellar);
                _isRefugee = __instance.obj_id.ToLowerInvariant().Contains(Refugee);
                _isWritersTable = __instance.obj_id.ToLowerInvariant().Contains(Writer);
            }
        }

        //[HarmonyPatch(typeof(MultiInventory), nameof(MultiInventory.RemoveItems))]
        //public static class MultiInventoryRemoveItemsPatch
        //{
        //    [HarmonyPrefix]
        //    public static void Prefix(ref MultiInventory __instance, ref List<Inventory> ____inventories)
        //    {
        //        if (instance != __instance) return;
        //        if (_zombieWorker && !_isVendor)
        //        {
        //            ____inventories = _mi.all;
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(MultiInventory), nameof(MultiInventory.GetTotalCount))]
        //public static class MultiInventoryGetTotalCountPatch
        //{
        //    [HarmonyPrefix]
        //    public static void Prefix(ref MultiInventory __instance, ref List<Inventory> ____inventories)
        //    {
        //        if (instance != __instance) return;
        //        //if (item_id.Contains("witch")) return;
        //        //if (item_id.Contains("npc")) return;
        //        //Log($"[MultiInventory.GetTotalCount ItemID]: {item_id}");
        //        //foreach (var inv in ____inventories)
        //        //{
        //        //    if (inv.data.id.Contains("witch")) return;
        //        //    if (inv.data.id.Contains("npc")) return;
        //        //    Log($"[MultiInventory.GetTotalCount InvData]: {inv.data.id}, {inv.name}");
        //        //}
        //        if (_zombieWorker && !_isVendor)
        //        {
        //            ____inventories = _mi.all;
        //        }
        //    }
        //}
    }
}
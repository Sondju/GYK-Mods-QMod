using HarmonyLib;
using Helper;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using WheresMaStorage.lang;

namespace WheresMaStorage
{
    [HarmonyAfter("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs")]
    public class MainPatcher
    {
        private const string Chest = "chest";
        private const string Gerry = "gerry";
        private const float LogGap = 3f;
        private const string Multi = "multi";
        private const string NpcBarman = "npc_tavern_barman";
        private const string Player = "player";
        private const string Refugee = "refugee";
        private const string Storage = "storage";
        private const string Tavern = "tavern";
        private const string Vendor = "vendor";
        private const string Writer = "writer";

        private static readonly string[] AlwaysHidePartials =
        {
            "refugee_camp_well", "refugee_camp_tent", "pump", "pallet"
        };

        private static readonly string[] ChiselItems =
        {
            "chisel"
        };

        private static readonly ItemDefinition.ItemType[] GraveItems =
        {
            ItemDefinition.ItemType.GraveStone, ItemDefinition.ItemType.GraveFence, ItemDefinition.ItemType.GraveCover,
            ItemDefinition.ItemType.GraveStoneReq, ItemDefinition.ItemType.GraveFenceReq, ItemDefinition.ItemType.GraveCoverReq,
        };

        //private static readonly string[] ZoneExclusions =
        //{
        //    "slava_test",
        //    "morgue_outside",
        //    "cabinet",
        //    "farm",
        //    "hill",
        //    "tavern",
        //    "vilage",
        //    "flat_under_waterflow",
        //    "flat_under_waterflow_2",
        //    "flat_under_waterflow_3",
        //    "swamp",
        //    "witch_hut",
        //    "wheat_land",
        //    "beatch",
        //    "forest_under_village",
        //    "east_border",
        //    "sealight",
        //    "camp",
        //    "marble_deposit",
        //    "burned_house",
        //    "nountain_fort",
        //    "cliff",
        //    "cellar_storage",
        //    //"refugees_camp",
        //    "euric_room",
        //    "alarich_tent_inside"
        //};
        private static readonly string[] PenPaperInkItems =
        {
            "book","chapter","ink","pen"
        };

        private static readonly string[] StockpileWidgetsPartials =
                {
            "mf_stones",  "mf_ore",  "mf_timber"
        };

        private static readonly ItemDefinition.ItemType[] ToolItems =
        {
            ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
            ItemDefinition.ItemType.Pickaxe, ItemDefinition.ItemType.FishingRod, ItemDefinition.ItemType.BodyArmor,
            ItemDefinition.ItemType.HeadArmor, ItemDefinition.ItemType.Sword, ItemDefinition.ItemType.Preach,
        };

        private static Config.Options _cfg;
        private static bool _gameBalanceAlreadyRun;
        private static bool _gratitudeCraft;
        private static int _invSize;

        private static MultiInventory _mi = new();
        private static MultiInventory _refugeeMi = new();
        private static float _timeOne, _timeTwo, _timeThree, _timeFour, _timeFive;
        private static float _timeSix, _timeSeven, _timeEight, _timeNine, _timeTen;
        private static bool _usingBag;
        private static bool _zombieWorker;

        //this method gets inserted into the CraftReally method using the transpiler below, overwriting any inventory the game sets during crafting
        public static MultiInventory GetMi(CraftDefinition craft, MultiInventory orig, WorldGameObject otherGameObject)
        {
            if (!_cfg.SharedInventory) return orig;

            if (craft.id.StartsWith("refugee_garden") || craft.id.StartsWith("refugee_builddesk"))
            {
                if (!(Time.time - _timeSix > LogGap)) return _mi;
                _timeSix = Time.time;

                Log(
                    $"[RefugeeGarden&Desk-InvRedirect]: Returned player multi-inventory to refugee garden!: Requester: {otherGameObject.obj_id}, Craft: {craft.id}");

                return _mi;
            }

            if (!_cfg.IncludeRefugeeDepot)
            {
                if (otherGameObject.is_player && craft.id.StartsWith("camp") || craft.id.Contains("refugee") ||
                    otherGameObject.obj_id.Contains("refugee"))
                {
                    if (!(Time.time - _timeSeven > LogGap)) return _refugeeMi;
                    _timeSeven = Time.time;
                    Log($"[Refugee-InvRedirect]: Returned refugee multi-inventory to them!: Requester: {otherGameObject.obj_id}, Craft: {craft.id}");
                    return _refugeeMi;
                }
            }

            if ((otherGameObject.has_linked_worker && otherGameObject.linked_worker.obj_id.Contains("zombie")) || otherGameObject.obj_id.Contains("zombie") || otherGameObject.obj_id.StartsWith("mf_") || _gratitudeCraft || (_cfg.IncludeRefugeeDepot && (otherGameObject.obj_id.Contains("refugee") || craft.id.Contains("camp")) && !(otherGameObject.obj_id.Contains("well") || otherGameObject.obj_id.Contains("hive"))))
            {
                if ((otherGameObject.has_linked_worker && otherGameObject.linked_worker.obj_id.Contains("zombie")) || otherGameObject.obj_id.Contains("zombie"))
                {
                    _zombieWorker = true;
                }

                if (!(Time.time - _timeEight > LogGap)) return _mi;
                _timeEight = Time.time;
                Log($"[InvRedirect]: Redirected craft inventory to player MultiInventory! Object: {otherGameObject.obj_id}, Craft: {craft.id}, Gratitude: {_gratitudeCraft}");

                return _mi;
            }

            _zombieWorker = false;

            if (!(Time.time - _timeNine > LogGap)) return orig;
            _timeNine = Time.time;
            Log($"[InvRedirect]: Original inventory sent back to requester! IsPlayer: {otherGameObject.is_player}, Object: {otherGameObject.obj_id}, Craft: {craft.id}, Gratitude: {_gratitudeCraft}");
            return orig;
        }

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

        private static string GetLocalizedString(string content)
        {
            Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
            return content;
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
            Log($"[Bag]: {inventoryWidget.inventory_data.is_bag}, ID: {inventoryWidget.inventory_data.id}");
            if (inventoryWidget.inventory_data.id.Contains(Writer)) return;
            if (inventoryWidget.header_label.text.Contains(Gerry)) return;
            if (!_cfg.ShowWorldZoneInTitles && !_cfg.ShowUsedSpaceInTitles) return;

            string wzLabel;
            string objId;
            bool isPlayer;
            var subNameSplit = inventoryWidget.inventory_data.sub_name.Split('#');
            if (string.IsNullOrEmpty(subNameSplit[0]))
            {
                objId = GetLocalizedString(strings.Player);
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
                wzLabel = wzId != null ? GJL.L("zone_" + wzId.id) : GetLocalizedString(strings.Wilderness);
            }
            else
            {
                wzLabel = GetLocalizedString(strings.Wilderness);
            }

            var cultureInfo = CrossModFields.Culture;
            var textInfo = cultureInfo.TextInfo;
            wzLabel = textInfo.ToTitleCase(wzLabel);

            var test = new Inventory(inventoryWidget.inventory_data.MakeInventoryCopy());
            var cap = test.size;
            var used = test.data.inventory.Count;

            inventoryWidget.header_label.overflowMethod = UILabel.Overflow.ResizeFreely;

            var header = objId;

            if (inventoryWidget.inventory_data.is_bag)
            {
                header = GJL.L(inventoryWidget.inventory_data.id);
            }

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
                //if (!Tools.TutorialDone()) return instructionsList.AsEnumerable();
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
                //var message = new List<string>
                //{
                //    $"---------------",
                //    $"Req: {__instance.GetCrafteryWGO().obj_id}, Loc: {__instance.GetCrafteryWGO().GetMyWorldZoneId()}"
                //};
                //foreach (var inv in __result.all)
                //{
                //    message.Add($"---------------");
                //    message.Add($"Inv: {inv.name}, Loc: {inv.data.sub_name}");
                //    message.Add($"---------------");
                //    message.AddRange(inv.data.inventory.Select(item => $"Item: {item.id}, Value: {item.value}"));
                //    message.Add($"---------------\n");
                //}

                //var stringMessage = string.Join("\n", message.ToArray());
                //Log($"{stringMessage}");

                if (!_cfg.SharedInventory) return;
                if (!_zombieWorker)
                {
                    if (Time.time - _timeTen > LogGap)
                    {
                        _timeTen = Time.time;

                        Log(
                            $"[BaseCraftGUI.multi_inventory (Getter)]: {__instance.name}, Craftery: {__instance.GetCrafteryWGO().obj_id}");
                    }
                }
                __result = _mi;
            }
        }

        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(CraftComponent), "CraftReally")]
        public static class CraftComponentPatch
        {
            [HarmonyPrefix]
            public static void Prefix(CraftDefinition craft, bool for_gratitude_points, ref bool start_by_player)
            {
                _gratitudeCraft = for_gratitude_points;
                if (_gratitudeCraft)
                {
                    start_by_player = false;
                }
                // Log($"[Gratitude]: Craft: {craft.id}, CraftGratCost: {craft.gratitude_points_craft_cost?.EvaluateFloat(MainGame.me.player,null)}, ForGrat: {for_gratitude_points}, StartedByPlayer: {start_by_player}");
            }

            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                if (!_cfg.SharedInventory) return codes.AsEnumerable();

                var usedMultiField = AccessTools.Field(typeof(CraftComponent), "used_multi_inventory");
                var otherObj = AccessTools.Field(typeof(CraftComponent), "other_obj");
                var miGetter = typeof(MainPatcher).GetMethod("GetMi");
                var insertIndex = -1;
                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand.ToString().Contains("item_needs") && codes[i - 1].opcode == OpCodes.Ldarg_1)
                    {
                        insertIndex = i;
                        Log($"[CraftReally]: Found insert index! {i}");
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
                    Log($"[CraftReally]: Inserted range into {insertIndex}");
                }
                else
                {
                    Log($"[CraftReally]: Insert range not found!");
                }

                return codes.AsEnumerable();
            }
        }

        [HarmonyBefore("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs")]
        [HarmonyPatch(typeof(CraftDefinition), "takes_item_durability", MethodType.Getter)]
        public static class CraftDefinitionTakesItemDurabilityPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref CraftDefinition __instance, ref bool __result)
            {
                if (__instance == null) return;

                if (_cfg.EnablePenPaperInkStacking)
                {
                    if (__instance.needs.Exists(item => item.id.Equals("pen:ink_pen")) && __instance.dur_needs_item > 0)
                    {
                        __result = false;
                    }
                }

                if (_cfg.EnableChiselStacking)
                {
                    if (__instance.needs.Exists(item => item.id.Contains("chisel")) && __instance.dur_needs_item > 0)
                    {
                        __result = false;
                    }
                }
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
                foreach (var item in __result.output.Where(a => GraveItems.Contains(a.definition.type)))
                {
                    item.definition.stack_count = 1;
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

                if (_cfg.EnableToolAndPrayerStacking || _cfg.EnableGraveItemStacking || _cfg.EnablePenPaperInkStacking || _cfg.EnableChiselStacking)
                {
                    foreach (var item in GameBalance.me.items_data.Where(item => item.stack_count == 1))
                    {
                        if (_cfg.EnableToolAndPrayerStacking)
                        {
                            if (ToolItems.Contains(item.type))
                            {
                                item.stack_count = item.stack_count + _cfg.StackSizeForStackables > 999 ? 999 : _cfg.StackSizeForStackables;
                            }
                        }

                        if (_cfg.EnableGraveItemStacking)
                        {
                            if (GraveItems.Contains(item.type))
                            {
                                item.stack_count = item.stack_count + _cfg.StackSizeForStackables > 999 ? 999 : _cfg.StackSizeForStackables;
                            }
                        }

                        if (_cfg.EnablePenPaperInkStacking)
                        {
                            if (PenPaperInkItems.Any(item.id.Contains))
                            {
                                item.stack_count = item.stack_count + _cfg.StackSizeForStackables > 999 ? 999 : _cfg.StackSizeForStackables;
                            }
                        }

                        if (!_cfg.EnableChiselStacking) continue;

                        if (ChiselItems.Any(item.id.Contains))
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

        [HarmonyPatch(typeof(GraveGUI), "GravePartsFilter", typeof(Item), typeof(ItemDefinition.ItemType))]
        public static class GraveGuiGravePartsFilterPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget.ItemFilterResult __result)
            {
                if (!MainGame.game_started) return;
                if (!_cfg.HideInvalidSelections) return;

                if (__result != InventoryWidget.ItemFilterResult.Active)
                {
                    __result = InventoryWidget.ItemFilterResult.Inactive;
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGUI))]
        public static class InventoryGuiPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(InventoryGUI.CloseBag))]
            public static void CloseBagPostfix()
            {
                _usingBag = false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(InventoryGUI.OpenBag))]
            public static void OpenBagPrefix()
            {
                _usingBag = true;
            }
        }

        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class InventoryPanelGuiDoOpeningPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref MultiInventory multi_inventory, ref List<UIWidget> ____separators, ref List<InventoryWidget> ____widgets, ref List<CustomInventoryWidget> ____custom_widgets)
            {
                var isChestPanel = __instance.name.ToLowerInvariant().Contains(Chest);
                var isVendorPanel = __instance.name.ToLowerInvariant().Contains(Vendor);
                var isPlayerPanel = __instance.name.ToLowerInvariant().Contains(Player) || (__instance.name.ToLowerInvariant().Contains(Multi) && CrossModFields.CurrentWgoInteraction == null);
                var isResourcePanelProbably = !isChestPanel && !isVendorPanel && !isPlayerPanel;

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

                if (isVendorPanel || CrossModFields.IsBarman || CrossModFields.IsTavernCellar || CrossModFields.IsRefugee) return;
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
                var isVendorPanel = __instance.name.ToLowerInvariant().Contains(Vendor);

                if (isVendorPanel) return;

                if (_cfg.DontShowEmptyRowsInInventory)
                {
                    __instance.dont_show_empty_rows = true;
                }

                if (CrossModFields.IsCraft || CrossModFields.IsVendor) return;

                if (_cfg.ShowOnlyPersonalInventory || CrossModFields.IsBarman || CrossModFields.IsTavernCellar || CrossModFields.IsRefugee || CrossModFields.IsChest || CrossModFields.IsWritersTable)
                {
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
                //if (MainGame.me.save.IsInTutorial()) return;
                var isChest = __instance.name.ToLowerInvariant().Contains(Chest);
                var isPlayer = __instance.name.ToLowerInvariant().Contains(Player) || (__instance.name.ToLowerInvariant().Contains(Multi) && CrossModFields.CurrentWgoInteraction == null);

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

                if (_usingBag) return;

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
                // Log($"[InvDataID]: {__instance.inventory_data.id}");
                if (activeCount <= 0)
                {
                    __instance.Hide();
                }

                typeof(InventoryWidget).GetMethod("RecalculateWidgetSize", AccessTools.all)
                    ?.Invoke(__instance, new object[]
                    {
                    });
            }
        }

        [HarmonyPatch(typeof(MixedCraftGUI), "AlchemyItemPickerFilter", typeof(Item), typeof(InventoryWidget))]
        public static class MixedCraftGuiAlchemyItemPickerFilterPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget.ItemFilterResult __result)
            {
                if (!MainGame.game_started) return;
                if (!_cfg.HideInvalidSelections) return;

                if (__result != InventoryWidget.ItemFilterResult.Active)
                {
                    __result = InventoryWidget.ItemFilterResult.Inactive;
                }
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
                //if (!Tools.TutorialDone()) return instructionsList.AsEnumerable();
                if (!_cfg.HideInvalidSelections) return instructionsList.AsEnumerable();
                instructionsList[5].opcode = OpCodes.Ldc_I4_1;
                instructionsList[49].opcode = OpCodes.Ldc_I4_1;

                return instructionsList.AsEnumerable();
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
                //if (!Tools.TutorialDone()) return instructionsList.AsEnumerable();
                if (!_cfg.HideInvalidSelections) return instructionsList.AsEnumerable();
                instructionsList[5].opcode = OpCodes.Ldc_I4_1;

                return instructionsList.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(SoulContainerWidget), "SoulItemsFilter", typeof(Item))]
        public static class SoulContainerWidgetSoulItemsFilterPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref InventoryWidget.ItemFilterResult __result)
            {
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
                if (!MainGame.game_started) return;
                if (!_cfg.HideInvalidSelections) return;

                if (__result != InventoryWidget.ItemFilterResult.Active)
                {
                    __result = InventoryWidget.ItemFilterResult.Inactive;
                }
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        public static class TimeOfDayUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (Input.GetKeyUp(KeyCode.F5))
                {
                    _cfg = Config.GetOptions();

                    if (!CrossModFields.ConfigReloadShown)
                    {
                        Tools.ShowMessage(GetLocalizedString(strings.ConfigMessage), Vector3.zero);
                        CrossModFields.ConfigReloadShown = true;
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
                // Log($"[WGO-Get]: {__instance.obj_id}");

                ////if (!Tools.TutorialDone()) return;
                _zombieWorker = (__instance.has_linked_worker && __instance.linked_worker.obj_id.Contains("zombie")) || __instance.obj_def.id.Contains("zombie");
                
                if (!_cfg.SharedInventory) return;

                if (__instance.vendor != null)
                {
                    if (_zombieWorker) return;
                    if (Time.time - _timeOne > LogGap)
                    {
                        _timeOne = Time.time;
                        Log(
                            $"[WorldGameObject.GetMultiInventory-Postfix]: REJECTED: __instance.vendor = {__instance.vendor.id} ");
                    }

                    return;
                }

                if (!_zombieWorker)
                {
                    if (__instance.obj_def.IsNPC() && !__instance.obj_id.Contains("invisible"))
                    {
                        if (!(Time.time - _timeTwo > LogGap)) return;
                        _timeTwo = Time.time;

                        Log(
                            $"[WorldGameObject.GetMultiInventory-Postfix]: REJECTED: __instance.obj_def.IsNPC {__instance.obj_def.id} ");

                        return;
                    }
                }

                //if (_cfg.CacheEligibleInventories && !_cfg.IncludeRefugeeDepot && __instance.obj_id.Contains("invisible") && _refugeeMi.all.Count > 0)
                //{
                //    __result = _refugeeMi;
                //    Log(
                //        $"[WorldGameObject.GetMultiInventory-Postfix]: Sending refugee inventory to refugee bed: {__instance.obj_id}");
                //    return;
                //}

                if (_cfg.CacheEligibleInventories && _mi.all.Count > 0)
                {
                    if (__instance.is_player)
                    {
                        if (!_zombieWorker)
                        {
                            if (Time.time - _timeThree > LogGap)
                            {
                                _timeThree = Time.time;
                                Log(
                                    $"[WorldGameObject.GetMultiInventory-Postfix]: Sending cached inventory to Player: {__instance.obj_id}");
                            }
                        }

                        var pMi = new MultiInventory();
                        var newInv = _mi.all.Where(a => !a.name.Contains("Toolbelt")).ToList();
                        pMi.SetInventories(newInv);
                        //instance = pMi;
                        __result = pMi;

                        return;
                    }

                    if (__instance.obj_id.Contains("invisible"))
                    {
                        if (Time.time - _timeFour > LogGap)
                        {
                            _timeFour = Time.time;
                            Log(
                                $"[WorldGameObject.GetMultiInventory-Postfix]: Most likely refugee farm. Sending cache: {__instance.obj_id}");
                        }

                        __result = _mi;
                        return;
                    }

                    if (__instance == CrossModFields.PreviousWgoInteraction || __instance.obj_id.StartsWith("mf_") || __instance.obj_id.Contains("compost"))
                    {
                        //Log(
                        //    $"[WorldGameObject.GetMultiInventory-Postfix]: _previousWgo == __instance. Sending cache: {__instance.obj_id}");

                        __result = _mi;
                        return;
                    }
                }

                if (__instance.is_player || __instance == CrossModFields.CurrentWgoInteraction || _zombieWorker || __instance.obj_id.StartsWith("mf_"))
                {
                    CrossModFields.PreviousWgoInteraction = __instance;
                    _mi = new MultiInventory();
                    _refugeeMi = new MultiInventory();
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

                        //if (ZoneExclusions.Contains(worldZone.id)) continue;
                        var worldZoneMulti =
                            worldZone.GetMultiInventory(player_mi: MultiInventory.PlayerMultiInventory.ExcludePlayer,
                                sortWGOS: true);
                        if (worldZoneMulti == null) continue;
                        foreach (var inv in worldZoneMulti.Where(inv => inv != null))// && inv.data.inventory.Count != 0))
                        {
                            if (worldZone.id.ToLowerInvariant().Contains("refugee"))
                            {
                                _refugeeMi.AddInventory(inv);
                            }

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
                        if (Time.time - _timeFive > LogGap)
                        {
                            _timeFive = Time.time;
                            Log(
                                $"[WorldGameObject.GetMultiInventory-Postfix]: Sending non-cached to __instance: {__instance.obj_id}, isPlayer: {__instance.is_player}, _previousWgo: {CrossModFields.PreviousWgoInteraction.obj_id}, Zombie: {_zombieWorker}");
                        }
                    }

                    __result = _mi;
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), "InitNewObject")]
        public static class WorldGameObjectInitPatch
        {
            [HarmonyPostfix]
            public static void Postfix(WorldGameObject __instance)
            {
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
    }
}
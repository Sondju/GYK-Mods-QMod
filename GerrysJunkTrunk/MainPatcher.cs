using GerrysJunkTrunk.lang;
using HarmonyLib;
using Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling.Memory.Experimental;
using Object = UnityEngine.Object;

namespace GerrysJunkTrunk
{
    public class MainPatcher
    {
        private const float FullPriceModifier = 0.90f;
        private const float PityPrice = 0.10f;
        private const int LargeInvSize = 20;
        private const int LargeMaxItemCount = 100;
        private const float PriceModifier = 0.60f;
        private const string ShippingBoxTag = "shipping_box";
        private const string ShippingItem = "shipping";
        private const int SmallInvSize = 10;
        private const int SmallMaxItemCount = 50;
        private static readonly List<WorldGameObject> KnownVendors = new();
        private static readonly Dictionary<string, int> StackSizeBackups = new();
        private static readonly List<WorldGameObject> VendorWgos = new();
        private static Config.Options _cfg;
        private static InternalConfig.Options _internalCfg;
        private static WorldGameObject _myVendor;
        private static WorldGameObject _shippingBox;
        private static WorldGameObject _interactedObject;
        private static bool _shippingBuild;
        private static bool _usingShippingBox;
        private static List<VendorSale> _vendorSales = new();
        private static readonly List<ItemPrice> PriceCache = new();
        private static readonly List<BaseItemCellGUI> AlreadyDone = new();
        private static ObjectCraftDefinition _newItem;
        private const string ShippingBoxId = "mf_wood_builddesk:p:mf_shipping_box_place";

        public static void Patch()
        {
            try
            {
                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.GerrysJunkTrunk");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _internalCfg = InternalConfig.GetOptions();
                _cfg = Config.GetOptions();
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        private static void ShowSummary(string money)
        {
            if (!MainGame.game_started) return;
            Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
            var result = string.Empty;
            foreach (var vendor in _vendorSales)
            {
                var sales = vendor.GetSales().OrderBy(a => a.GetItem().id).ToList();

                foreach (var sale in sales)
                {
                    result += $"{sale.GetItem().GetItemName()} {strings.For} {Trading.FormatMoney(sale.GetPrice())}\n";
                }
            }

            GUIElements.me.dialog.OpenOK($"[37ff00]{strings.Header}[-]", null, $"{result}", true, $"{money}");
        }

        private static void ClearGerryFlag(ref ChestGUI chestGui)
        {
            if (chestGui == null || !_usingShippingBox) return;
            if (StackSizeBackups.Count <= 0) return;
            foreach (var item in chestGui.player_panel.multi_inventory.all[0].data.inventory)
            {
                var found = StackSizeBackups.TryGetValue(item.id, out var value);
                if (!found) continue;

                item.definition.stack_count = value;
            }

            foreach (var item in chestGui.chest_panel.multi_inventory.all[0].data.inventory)
            {
                var found = StackSizeBackups.TryGetValue(item.id, out var value);
                if (!found) continue;

                item.definition.stack_count = value;
            }

            _usingShippingBox = false;
        }

        private static float GetBoxEarnings(WorldGameObject shippingBox)
        {
            RefreshVendors();

            if (VendorWgos.Count <= 0) return 0f;

            var totalSalePrice = 0f;
            var totalCount = 0;
            _vendorSales.Clear();

            var prevItem = string.Empty;
            foreach (var item in shippingBox.data.inventory.Where(item => !string.Equals(item.id, prevItem)))
            {
                totalCount = shippingBox.data.GetTotalCount(item.id);

                prevItem = item.id;
                List<Vendor> vendorList = new();
                List<float> priceList = new();

                foreach (var vendor in VendorWgos)
                {
                    var num = 0f;
                    var myTrader = new Trading(vendor);
                    if (item.definition.base_price <= 0)
                    {
                        if (item.id.EndsWith(":3"))
                        {
                            num += 0.75f * totalCount;
                        }
                        else if (item.id.EndsWith(":2"))
                        {
                            num += 0.60f * totalCount;
                        }
                        else if (item.id.EndsWith(":1"))
                        {
                            num += 0.45f * totalCount;
                        }
                        else
                        {
                            num += 0.25f * totalCount;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < totalCount; i++)
                        {
                            var itemCost = Mathf.Round(myTrader.GetSingleItemCostInPlayerInventory(item, -i) * 100f) / 100f;
                            num += itemCost;
                        }
                    }

                    vendorList.Add(vendor.vendor);
                    priceList.Add(UnlockedFullPrice() ? num * FullPriceModifier : num * PriceModifier);
                }

                var maxSaleIndex = priceList.IndexOf(priceList.Max());
                var newSale = new VendorSale(vendorList[maxSaleIndex]);
                newSale.AddSale(item, totalCount, priceList[maxSaleIndex]);
                _vendorSales.Add(newSale);
                _vendorSales = _vendorSales.OrderBy(a => a.GetVendor().id).ToList();
                totalSalePrice += priceList[maxSaleIndex];
            }

            if (totalSalePrice <= 0)
            {
                var price = PityPrice * totalCount;
                return UnlockedFullPrice() ? price * FullPriceModifier : price * PriceModifier;
            }
            return UnlockedFullPrice() ? totalSalePrice * FullPriceModifier : totalSalePrice * PriceModifier;
        }

        private static float GetItemEarnings(Item selectedItem)
        {
            var itemCache = PriceCache.Find(a =>
                string.Equals(a.GetItem().id, selectedItem.id) && a.GetQty() == selectedItem.value);

            if (itemCache != null)
            {
                if (itemCache.GetPrice() == 0)
                {
                    var price = PityPrice * itemCache.GetQty();
                    return UnlockedFullPrice() ? price * FullPriceModifier : price * PriceModifier;
                }
                return UnlockedFullPrice() ? itemCache.GetPrice() * FullPriceModifier : itemCache.GetPrice() * PriceModifier;
            }

            RefreshVendors();

            if (VendorWgos.Count <= 0) return 0f;

            var totalSalePrice = 0f;
            _vendorSales.Clear();
            var totalCount = selectedItem.value;

            List<Vendor> vendorList = new();
            List<float> priceList = new();

            foreach (var vendor in VendorWgos)
            {
                float num = 0;
                var myTrader = new Trading(vendor);
                if (selectedItem.definition.base_price <= 0)
                {
                    if (selectedItem.id.EndsWith(":3"))
                    {
                        num += 0.75f * totalCount;
                    }
                    else if (selectedItem.id.EndsWith(":2"))
                    {
                        num += 0.60f * totalCount;
                    }
                    else if (selectedItem.id.EndsWith(":1"))
                    {
                        num += 0.45f * totalCount;
                    }
                    else
                    {
                        num += 0.25f * totalCount;
                    }
                }
                else
                {
                    for (var i = 0; i < totalCount; i++)
                    {
                        var itemCost = Mathf.Round(myTrader.GetSingleItemCostInPlayerInventory(selectedItem, -i) * 100f) / 100f;
                        num += itemCost;
                    }
                }

                vendorList.Add(vendor.vendor);
                priceList.Add(num);
            }

            var maxSaleIndex = priceList.IndexOf(priceList.Max());
            var newSale = new VendorSale(vendorList[maxSaleIndex]);
            newSale.AddSale(selectedItem, totalCount, priceList[maxSaleIndex]);
            _vendorSales.Add(newSale);
            _vendorSales = _vendorSales.OrderBy(a => a.GetVendor().id).ToList();
            totalSalePrice += priceList[maxSaleIndex];

            PriceCache.Add(new ItemPrice(selectedItem, totalCount, priceList[maxSaleIndex]));

            if (totalSalePrice <= 0)
            {
                var price = PityPrice * totalCount;
                return UnlockedFullPrice() ? price * FullPriceModifier : price * PriceModifier;
            }
            return UnlockedFullPrice() ? totalSalePrice * FullPriceModifier : totalSalePrice * PriceModifier;
        }

        private static void Log(string message, bool error = false)
        {
            Tools.Log("GerrysJunkTrunk", $"{message}", error);
        }

        private static void RefreshVendors()
        {
            if (KnownVendors.Count == VendorWgos.Count) return;
            foreach (var vendor in KnownVendors.Where(vendor => !VendorWgos.Exists(a => string.Equals(vendor.obj_id, a.obj_id))))
            {
                _myVendor = Object.Instantiate(vendor);
                _myVendor.data.money = 1000000f;
                _myVendor.vendor.cur_money = 1000000f;
                _myVendor.vendor.cur_tier = 3;
                _myVendor.vendor.definition.not_buying.Clear();

                if (!VendorWgos.Exists(a => string.Equals(a.obj_id, _myVendor.obj_id)))
                {
                    VendorWgos.Add(_myVendor);
                }
            }
        }

        private static void ShowIntroMessage()
        {
            GUIElements.me.dialog.OpenOK(strings.Message1, null, $"{strings.Message2}\n{strings.Message3}\n{strings.Message4}\n{strings.Message5}\n{strings.Message6}\n{strings.Message7}", true, strings.Message8);
        }

        private static void StartGerryRoutine(float num)
        {
            Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
            var noSales = num <= 0;
            var money = Trading.FormatMoney(num, true);
            //var gerry = WorldMap.GetNPCByObjID("talking_skull");
            var gerry = WorldMap.SpawnWGO(_shippingBox.transform, "talking_skull", new Vector3(_shippingBox.pos3.x, _shippingBox.pos3.y + 43f, _shippingBox.pos3.z));
            gerry.ReplaceWithObject("talking_skull", true);

            GJTimer.AddTimer(2f, delegate
            {
                gerry.Say(noSales ? strings.Nothing : strings.WorkWork, delegate
                {
                    GJTimer.AddTimer(1f, delegate
                    {
                        gerry.ReplaceWithObject("talking_skull", true);
                        gerry.DestroyMe();
                    });
                }, null, SpeechBubbleGUI.SpeechBubbleType.Talk, SmartSpeechEngine.VoiceID.Skull);
            });

            if (noSales) return;
            GJTimer.AddTimer(8f, delegate
            {
                var gerry2 = WorldMap.SpawnWGO(_shippingBox.transform, "talking_skull", new Vector3(_shippingBox.pos3.x, _shippingBox.pos3.y + 43f, _shippingBox.pos3.z));
                gerry2.ReplaceWithObject("talking_skull", true);

                GJTimer.AddTimer(2f, delegate
                {
                    gerry2.Say($"{money}", delegate
                        {
                            _shippingBox.data.inventory.Clear();
                            if (_cfg.showSoldMessagesOnPlayer)
                            {
                                Sounds.PlaySound("coins_sound", MainGame.me.player_pos, true);
                                var pos = MainGame.me.player_pos;
                                pos.y += 125f;
                                EffectBubblesManager.ShowImmediately(pos, $"{money}",
                                    num > 0 ? EffectBubblesManager.BubbleColor.Green : EffectBubblesManager.BubbleColor.Red,
                                    true, 4f);
                            }
                            else
                            {
                                Sounds.PlaySound("coins_sound", gerry2.pos3, true);
                            }

                            GJTimer.AddTimer(2f, delegate
                            {
                                gerry2.Say(strings.Bye, delegate
                                {
                                    GJTimer.AddTimer(1f, delegate
                                    {
                                        gerry2.ReplaceWithObject("talking_skull", true);
                                        gerry2.DestroyMe();

                                        GJTimer.AddTimer(1f, delegate { ShowSummary(money); });
                                    });
                                }, null, SpeechBubbleGUI.SpeechBubbleType.Talk, SmartSpeechEngine.VoiceID.Skull);
                            });
                        }, null, SpeechBubbleGUI.SpeechBubbleType.Talk,
                        SmartSpeechEngine.VoiceID.Skull);
                });
            });
        }

        private static void TryAdd<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key)) return;
            dictionary.Add(key, value);
        }

        private static bool UnlockedFullPrice()
        {
            return UnlockedShippingBoxExpansion() && MainGame.me.save.unlocked_techs.Exists(a => a.ToLowerInvariant().Equals("Best friend".ToLowerInvariant()));
        }

        private static bool UnlockedShippingBox()
        {
            return MainGame.me.save.unlocked_techs.Exists(a =>
                a.ToLowerInvariant().Equals("Wood processing".ToLowerInvariant()));
        }

        private static bool UnlockedShippingBoxExpansion()
        {
            return UnlockedShippingBox() && MainGame.me.save.unlocked_techs.Exists(a => a.ToLowerInvariant().Equals("Engineer".ToLowerInvariant()));
        }

        private static void UpdateInternalConfig()
        {
            InternalConfig.WriteOptions();
            _internalCfg = InternalConfig.GetOptions();
        }

        private static readonly ItemDefinition.ItemType[] ExcludeItems =
        {
            ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
            ItemDefinition.ItemType.Pickaxe, ItemDefinition.ItemType.FishingRod, ItemDefinition.ItemType.BodyArmor,
            ItemDefinition.ItemType.HeadArmor, ItemDefinition.ItemType.Sword, ItemDefinition.ItemType.Preach,
            ItemDefinition.ItemType.GraveStone, ItemDefinition.ItemType.GraveFence, ItemDefinition.ItemType.GraveCover,
            ItemDefinition.ItemType.GraveStoneReq, ItemDefinition.ItemType.GraveFenceReq, ItemDefinition.ItemType.GraveCoverReq,
        };

        private static void UpdateItemStates(ref ChestGUI __instance)
        {
            foreach (var inventory in __instance.player_panel.multi_inventory.all.Where(i => i.data.inventory.Count > 0))
            {
                //reset status
                foreach (var item in inventory.data.inventory)
                {
                    var itemCellGuiForItem = __instance.player_panel.GetItemCellGuiForItem(item);
                    itemCellGuiForItem.SetInactiveState(false);
                }

                //disable quest item selling
                foreach (var item in inventory.data.inventory.Where(item => item.definition.player_cant_throw_out && !ExcludeItems.Contains(item.definition.type)))
                {
                    var itemCellGuiForItem = __instance.player_panel.GetItemCellGuiForItem(item);
                    itemCellGuiForItem.SetInactiveState();
                }
            }

            //disable items in the chest inventory
            foreach (var inventory in __instance.chest_panel.multi_inventory.all.Where(i => i.data.inventory.Count > 0))
            {
                inventory.is_locked = true;
                foreach (var item in inventory.data.inventory)
                {
                    var itemCellGuiForItem = __instance.chest_panel.GetItemCellGuiForItem(item);
                    if (itemCellGuiForItem != null)
                    {
                        itemCellGuiForItem.SetInactiveState();
                    }
                }
            }
        }

        private static int GetTrunkTier()
        {
            if (UnlockedFullPrice()) return 3;
            return UnlockedShippingBoxExpansion() ? 2 : 1;
        }

        //should never need these, but will stop a 2nd being built
        [HarmonyPatch(typeof(BuildModeLogics), nameof(BuildModeLogics.CanBuild))]
        public static class BuildItemGuiSelectPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result, ref CraftDefinition cd)
            {
                //
                if (_internalCfg.shippingBoxBuilt && _shippingBox != null)
                {
                    if (cd.id.Contains(ShippingItem))
                    {
                        __result = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryPanelGUI), nameof(InventoryPanelGUI.Open))]
        public static class InventoryPanelGuiOpenPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref InventoryPanelGUI __instance)
            {
                //
                AlreadyDone.Clear();
                //AlreadyDone.Add(__instance.selected_item_gui);
            }
        }

        [HarmonyPatch(typeof(BuildModeLogics), nameof(BuildModeLogics.OnBuildCraftSelected))]
        public static class BuildModeLogicsOnBuildCraftSelectedPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref ObjectCraftDefinition cd)
            {
                //
                if (cd.id.Contains(ShippingItem))
                {
                    _shippingBuild = true;
                    var ocd = GameBalance.me.GetData<ObjectCraftDefinition>("mf_wood_builddesk:p:mf_box_stuff_place");
                    cd = ocd;
                }
            }
        }

        [HarmonyPatch(typeof(ChestGUI), "Hide")]
        public static class ChestGuiHide
        {
            [HarmonyPrefix]
            public static void Prefix(ref ChestGUI __instance)
            {
                //
                ClearGerryFlag(ref __instance);
            }
        }

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs", "p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(ChestGUI), "MoveItem", typeof(Item), typeof(int), typeof(bool), typeof(Item), typeof(bool))]
        public static class ChestGuiMoveItemPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref ChestGUI __instance)
            {
                //
                if (__instance == null || !_usingShippingBox) return;

                UpdateItemStates(__instance: ref __instance);
            }
        }

        [HarmonyPatch(typeof(ChestGUI), "OnPressedBack")]
        public static class ChestGuiOnClosePressedPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref ChestGUI __instance)
            {
                //
                ClearGerryFlag(ref __instance);
            }
        }

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs", "p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(ChestGUI), nameof(ChestGUI.Open))]
        public static class ChestGuiOpenPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref ChestGUI __instance)
            {
                //

                if (__instance == null || !_usingShippingBox) return;

                var maxItemCount = SmallMaxItemCount;

                if (UnlockedFullPrice())
                {
                    maxItemCount = LargeMaxItemCount;
                }

                foreach (var item in __instance.player_panel.multi_inventory.all[0].data.inventory.Where(item => item.definition.stack_count > 1))
                {
                    TryAdd(StackSizeBackups, item.id, item.definition.stack_count);

                    item.definition.stack_count = maxItemCount;
                }

                foreach (var item in __instance.chest_panel.multi_inventory.all[0].data.inventory.Where(item => item.definition.stack_count > 1))
                {
                    TryAdd(StackSizeBackups, item.id, item.definition.stack_count);

                    item.definition.stack_count = maxItemCount;
                }

                UpdateItemStates(__instance: ref __instance);
            }
        }

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs", "p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(InventoryPanelGUI), "OnItemOver", typeof(InventoryWidget), typeof(BaseItemCellGUI))]
        public static class BaseItemCellGuiInitTooltipsPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref InventoryPanelGUI __instance, ref BaseItemCellGUI item_gui)
            {
                Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                //
                //if (!_usingShippingBox) return;
                if (!_cfg.showItemPriceTooltips) return;
                if (!UnlockedShippingBox()) return;
                if (__instance == null || item_gui == null) return;

                //Log($"[ItemGUI]: {item_gui.item.id}");

                if (AlreadyDone.Contains(item_gui)) return;
                if (item_gui.id_empty) return;

                if (item_gui.x1 != null && item_gui.x1.tooltip != null && item_gui.x1.tooltip.has_info)
                {
                    item_gui.x1.tooltip.AddData(new BubbleWidgetSeparatorData());
                    item_gui.x1.tooltip.AddData(new BubbleWidgetTextData(
                        $"{strings.GerrysPrice} {Trading.FormatMoney(GetItemEarnings(item_gui.item), true)}",
                        UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
                    AlreadyDone.Add(item_gui);
                }

                if (item_gui.x2 != null && item_gui.x2.tooltip != null && item_gui.x2.tooltip.has_info)
                {
                    item_gui.x2.tooltip.AddData(new BubbleWidgetSeparatorData());
                    item_gui.x2.tooltip.AddData(new BubbleWidgetTextData(
                        $"{strings.GerrysPrice} {Trading.FormatMoney(GetItemEarnings(item_gui.item), true)}",
                        UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
                    AlreadyDone.Add(item_gui);
                }
            }

            [HarmonyFinalizer]
            private static Exception Finalizer()
            {
                return null;
            }
        }

        [HarmonyPatch(typeof(EnvironmentEngine), "OnEndOfDay")]
        public static class EnvironmentEngineOnEndOfDayPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                //
                if (!UnlockedShippingBox()) return;
                Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                if (_internalCfg.shippingBoxBuilt && _shippingBox != null)
                {
                    foreach (var item in _shippingBox.data.inventory)
                    {
                        for (var i = 0; i < item.value; i++)
                        {
                            item.OnTraded();
                        }
                    }

                    var earnings = GetBoxEarnings(_shippingBox);
                    if (earnings > 0)
                    {
                        Stats.PlayerAddMoney(earnings, strings.Header);
                    }

                    MainGame.me.player.data.money += earnings;
                    var money = Trading.FormatMoney(earnings, true);

                    Vector3 position;
                    float time;
                    if (_cfg.showSoldMessagesOnPlayer)
                    {
                        position = MainGame.me.player_pos;
                        position.y += 125f;
                        time = 4f;
                    }
                    else
                    {
                        position = _shippingBox.pos3;
                        position.y += 100f;
                        time = 7f;
                    }

                    if (_cfg.enableGerry)
                    {
                        StartGerryRoutine(earnings);
                    }
                    else
                    {
                        if (_cfg.disableSoldMessageWhenNoSale) return;

                        Sounds.PlaySound("coins_sound", position, true);
                        _shippingBox.data.inventory.Clear();
                        EffectBubblesManager.ShowImmediately(position, $"{money}",
                            earnings > 0 ? EffectBubblesManager.BubbleColor.Green : EffectBubblesManager.BubbleColor.Red,
                            true, time);
                    }

                    if (_cfg.showSummary && !_cfg.enableGerry && earnings > 0)
                    {
                        ShowSummary(money);
                    }
                }
            }
        }


        [HarmonyBefore("p1xel8ted.GraveyardKeeper.QueueEverything")]
        [HarmonyPatch(typeof(GameBalance))]
        public static class GameBalanceLoadGameBalancePatches
        {
            [HarmonyBefore("p1xel8ted.GraveyardKeeper.QueueEverything")]
            [HarmonyPatch(nameof(GameBalance.LoadGameBalance))]
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (GameBalance.me.craft_data.Exists(a => a == _newItem)) return;

                //Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                var newCd = new ObjectCraftDefinition();
                var cd = GameBalance.me.GetData<ObjectCraftDefinition>("mf_wood_builddesk:p:mf_box_stuff_place");
                newCd.craft_in = cd.craft_in;
                newCd.builder_ids = cd.builder_ids;
                newCd.out_obj = "mf_shipping_box_place";
                newCd.needs = cd.needs;
                newCd.needs_from_wgo = cd.needs_from_wgo;
                newCd.output = cd.output;
                newCd.out_items_expressions = cd.out_items_expressions;
                newCd.output_res_wgo = cd.output_res_wgo;
                newCd.output_set_res_wgo = cd.output_set_res_wgo;
                newCd.set_when_cancelled = cd.set_when_cancelled;
                newCd.output_to_wgo = cd.output_to_wgo;
                newCd.output_to_wgo_on_start = cd.output_to_wgo_on_start;
                newCd.tool_actions = cd.tool_actions;
                newCd.condition = cd.condition;
                newCd.end_script = cd.end_script;
                newCd.end_event = cd.end_event;
                newCd.flag = cd.flag;
                newCd.craft_time = cd.craft_time;
                newCd.energy = cd.energy;
                newCd.gratitude_points_craft_cost = cd.gratitude_points_craft_cost;
                newCd.sanity = cd.sanity;
                newCd.hidden = false;
                newCd.needs_unlock = true;
                newCd.icon = cd.icon;
                newCd.craft_type = cd.craft_type;
                newCd.is_auto = cd.is_auto;
                newCd.not_hide_gui = cd.not_hide_gui;
                newCd.can_craft_always = cd.can_craft_always;
                newCd.game_res_to_mirror_name = cd.game_res_to_mirror_name;
                newCd.game_res_to_mirror_max = cd.game_res_to_mirror_max;
                newCd.change_wgo = cd.change_wgo;
                newCd.use_variations = cd.use_variations;
                newCd.variation_index = cd.variation_index;
                newCd.craft_after_finish = cd.craft_after_finish;
                newCd.one_time_craft = true;
                newCd.force_multi_craft = cd.force_multi_craft;
                newCd.disable_multi_craft = cd.disable_multi_craft;
                newCd.sub_type = cd.sub_type;
                newCd.transfer_needs_to_wgo = cd.transfer_needs_to_wgo;
                newCd.set_out_wgo_params_on_start = cd.set_out_wgo_params_on_start;
                newCd.itempars_add = cd.itempars_add;
                newCd.itempars_set = cd.itempars_set;
                newCd.item_output = cd.item_output;
                newCd.item_needs = cd.item_needs;
                newCd.item_needs_leave = cd.item_needs_leave;
                newCd.dur_needs_item = cd.dur_needs_item;
                newCd.dur_needs_item_index = cd.dur_needs_item_index;
                newCd.difficulty = cd.difficulty;
                newCd.linked_perks = cd.linked_perks;
                newCd.linked_buffs = cd.linked_buffs;
                newCd.custom_name = strings.Header;
                newCd.tab_id = cd.tab_id;
                newCd.buff = cd.buff;
                newCd.needs_quality = cd.needs_quality;
                newCd.k_money = cd.k_money;
                newCd.k_faith = cd.k_faith;
                newCd.linked_sub_id = cd.linked_sub_id;
                newCd.dont_close_window_on_craft = cd.dont_close_window_on_craft;
                newCd.dur_parameter = cd.dur_parameter;
                newCd.dont_show_in_hint = cd.dont_show_in_hint;
                newCd.ach_key = cd.ach_key;
                newCd.craft_time_is_zero = cd.craft_time_is_zero;
                newCd.puff_when_replaced = cd.puff_when_replaced;
                newCd.is_item_crating_craft = cd.is_item_crating_craft;
                newCd.store_last_craft_slot = cd.store_last_craft_slot;
                newCd.hide_quality_icon = cd.hide_quality_icon;
                newCd.enqueue_type = cd.enqueue_type;
                newCd.id = ShippingBoxId;

                _newItem = newCd;

                GameBalance.me.craft_data.Add(_newItem);
                GameBalance.me.craft_obj_data.Add(_newItem);
                GameBalance.me.AddDataUniversal(_newItem);
                GameBalance.me.AddData(_newItem);
            }
        }

        //[HarmonyPatch]
        //public static class GameBalanceGetDataPatch
        //{
        //    [HarmonyTargetMethod]
        //    public static MethodBase TargetMethod()
        //    {
        //        return AccessTools.Method(typeof(GameBalanceBase), "GetData", generics: new []{typeof(ObjectCraftDefinition) }).MakeGenericMethod(typeof(string));
        //    }

        //    [HarmonyPrefix]
        //    public static void Prefix(ref string id)
        //    {
        //        //blah blah
        //    }
        //}

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class InventoryWidgetDoOpeningPatch
        {
            [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref List<InventoryWidget> ____widgets)
            {
                //
                if (!UnlockedShippingBox()) return;
                if (__instance == null) return;
                Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                var isChest = __instance.name.ToLowerInvariant().Contains("chest");
                var isPlayer = __instance.name.ToLowerInvariant().Contains("player");
                if (_usingShippingBox && isChest && !isPlayer)
                {
                    foreach (var inventoryWidget in ____widgets)
                    {
                        var vendorCount = KnownVendors.Count;
                        var tier = GetTrunkTier();
                        var header = vendorCount switch
                        {
                            > 1 => $"{strings.Header} (T{tier}) - {vendorCount} {strings.Vendors}",
                            1 => $"{strings.Header} (T{tier}) - {vendorCount} {strings.Vendor}",
                            _ => $"{strings.Header} (T{tier})"
                        };
                        inventoryWidget.header_label.text = _cfg.showKnownVendorCount ? header : $"{strings.Header} (T{tier})";
                        inventoryWidget.dont_show_empty_rows = true;
                        inventoryWidget.SetInactiveStateToEmptyCells();
                    }
                }
            }
        }

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(InventoryPanelGUI), nameof(InventoryPanelGUI.Redraw))]
        public static class InventoryWidgetRedrawPatch
        {
            [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref List<InventoryWidget> ____widgets)
            {
                //
                if (!UnlockedShippingBox()) return;
                if (__instance == null) return;
                Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                var isChest = __instance.name.ToLowerInvariant().Contains("chest");
                var isPlayer = __instance.name.ToLowerInvariant().Contains("player");
                if (_usingShippingBox && isChest && !isPlayer)
                {
                    foreach (var inventoryWidget in ____widgets)
                    {
                        var vendorCount = KnownVendors.Count;
                        var tier = GetTrunkTier();
                        var header = vendorCount switch
                        {
                            > 1 => $"{strings.Header} (T{tier}) - {vendorCount} {strings.Vendors}",
                            1 => $"{strings.Header} (T{tier}) - {vendorCount} {strings.Vendor}",
                            _ => $"{strings.Header} (T{tier})"
                        };
                        inventoryWidget.header_label.text = _cfg.showKnownVendorCount ? header : $"{strings.Header} (T{tier})";
                        inventoryWidget.dont_show_empty_rows = true;
                        inventoryWidget.SetInactiveStateToEmptyCells();
                    }

                    __instance.money_label.text = Trading.FormatMoney(GetBoxEarnings(_shippingBox), true);
                }
            }
        }

        private static void CheckShippingBox()
        {
            if (UnlockedShippingBox())
            {
                MainGame.me.save.UnlockCraft(ShippingBoxId);
                Log($"Tech requirements met, unlocking shipping box craft!");
            }
            else
            {
                MainGame.me.save.LockCraft(ShippingBoxId);
                Log($"Tech requirements not met, locking shipping box craft!");
            }
        }

        private static int _techCount;
        private static int _oldTechCount;

        [HarmonyPatch(typeof(MainGame), nameof(MainGame.Update))]
        public static class MainGameUpdatePatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!MainGame.game_started) return;

                _techCount = MainGame.me.save.unlocked_techs.Count;
                if (_techCount > _oldTechCount)
                {
                    _oldTechCount = _techCount;
                    CheckShippingBox();
                }

                if (_internalCfg.showIntroMessage)
                {
                    ShowIntroMessage();
                    _internalCfg.showIntroMessage = false;
                    UpdateInternalConfig();
                }

                if (!UnlockedShippingBox()) return;
                var sbCraft = GameBalance.me.GetData<ObjectCraftDefinition>(ShippingBoxId);
                if (_internalCfg.shippingBoxBuilt && _shippingBox == null)
                {
                    _shippingBox = Object.FindObjectsOfType<WorldGameObject>(true)
                        .FirstOrDefault(x => string.Equals(x.custom_tag, ShippingBoxTag));
                    if (_shippingBox == null)
                    {
                        Log("No Shipping Box Found!");
                        _internalCfg.shippingBoxBuilt = false;
                        sbCraft.hidden = false;
                    }
                    else
                    {
                        Log($"Found Shipping Box at {_shippingBox.pos3}");
                        _internalCfg.shippingBoxBuilt = true;
                        _shippingBox.data.drop_zone_id = ShippingBoxTag;

                        var invSize = SmallInvSize;
                        if (UnlockedShippingBoxExpansion())
                        {
                            invSize = LargeInvSize;
                        }
                        _shippingBox.data.SetInventorySize(invSize);

                       
                        sbCraft.hidden = true;
                    }

                    UpdateInternalConfig();
                }
            }
        }

        [HarmonyPatch(typeof(TechTreeGUIItem), "InitGamepadTooltip")]
        public static class TechTreeGuiItemInitGamepadTooltip
        {
            [HarmonyPostfix]
            public static void Postfix(ref TechTreeGUIItem __instance)
            {
                //
                if (__instance == null) return;
                {
                    Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                    var component = __instance.GetComponent<Tooltip>();
                    if (__instance.tech_id.ToLowerInvariant().Contains("Wood processing".ToLowerInvariant()))
                    {
                        component.AddData(new BubbleWidgetSeparatorData());
                        component.AddData(new BubbleWidgetTextData(strings.Stage1Header, UITextStyles.TextStyle.HintTitle));
                        component.AddData(new BubbleWidgetTextData(strings.Stage1Des, UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
                    }

                    if (__instance.tech_id.ToLowerInvariant().Contains("Engineer".ToLowerInvariant()))
                    {
                        component.AddData(new BubbleWidgetSeparatorData());
                        component.AddData(new BubbleWidgetTextData(strings.Stage2Header, UITextStyles.TextStyle.HintTitle));
                        component.AddData(new BubbleWidgetTextData(strings.Stage2Des, UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
                    }

                    if (__instance.tech_id.ToLowerInvariant().Contains("Best friend".ToLowerInvariant()))
                    {
                        component.AddData(new BubbleWidgetSeparatorData());
                        component.AddData(new BubbleWidgetTextData(strings.Stage3Header, UITextStyles.TextStyle.HintTitle));
                        component.AddData(new BubbleWidgetTextData(strings.Stage3Des, UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(TechUnlock), nameof(TechUnlock.GetTooltip), typeof(Tooltip))]
        public static class TechUnlockGetTooltipPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref TechUnlock __instance, ref Tooltip tooltip)
            {
                //
                if (__instance != null)
                {
                    Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                    if (LazyInput.gamepad_active) return;
                    var name = __instance.GetData().name;

                    if (name.ToLowerInvariant().Contains("Wooden plank".ToLowerInvariant()))
                    {
                        tooltip.AddData(new BubbleWidgetBlankSeparatorData());
                        tooltip.AddData(new BubbleWidgetTextData(strings.Stage1Header, UITextStyles.TextStyle.HintTitle, NGUIText.Alignment.Left));
                        tooltip.AddData(new BubbleWidgetTextData(strings.Stage1Des, UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
                    }

                    if (name.ToLowerInvariant().Contains("Engineer".ToLowerInvariant()))
                    {
                        tooltip.AddData(new BubbleWidgetBlankSeparatorData());
                        tooltip.AddData(new BubbleWidgetTextData(strings.Stage2Header, UITextStyles.TextStyle.HintTitle, NGUIText.Alignment.Left));
                        tooltip.AddData(new BubbleWidgetTextData(strings.Stage2Des, UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
                    }

                    if (name.ToLowerInvariant().Contains("Jeweler".ToLowerInvariant()))
                    {
                        tooltip.AddData(new BubbleWidgetBlankSeparatorData());
                        tooltip.AddData(new BubbleWidgetTextData(strings.Stage3Header, UITextStyles.TextStyle.HintTitle, NGUIText.Alignment.Left));
                        tooltip.AddData(new BubbleWidgetTextData(strings.Stage3Des, UITextStyles.TextStyle.TinyDescription, NGUIText.Alignment.Left));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Vendor), nameof(Vendor.CanTradeItem))]
        public static class VendorCanTradeItemPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref Vendor __instance, ref bool __result)
            {
                //
                if (!UnlockedShippingBox()) return;
                if (__instance == null || _myVendor == null || _myVendor.vendor == null) return;
                if (__instance.Equals(_myVendor.vendor))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.DestroyMe))]
        public static class WorldGameObjectDestroyMePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref WorldGameObject __instance)
            {
                //
                if (!UnlockedShippingBox()) return;
                if (__instance == null) return;
                if (string.Equals(__instance.custom_tag, ShippingBoxTag))
                {
                    Log($"Removed Shipping Box!");
                    _shippingBox = null;
                    _internalCfg.shippingBoxBuilt = false;
                    var sbCraft = GameBalance.me.GetData<ObjectCraftDefinition>(ShippingBoxId);
                    sbCraft.hidden = false;

                    UpdateInternalConfig();
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref WorldGameObject __instance, ref WorldGameObject other_obj)
            {
                //
                if (!UnlockedShippingBox()) return;
                if (__instance == null) return;
                _interactedObject = __instance;
                if (string.Equals(__instance.custom_tag, ShippingBoxTag))
                {
                    Log($"Found Shipping Box! {__instance.data.drop_zone_id}, Other: {other_obj.obj_id}");
                    _internalCfg.shippingBoxBuilt = true;
                    UpdateInternalConfig();
                    _usingShippingBox = true;
                    __instance.data.drop_zone_id = ShippingBoxTag;
                    __instance.custom_tag = ShippingBoxTag;
                    var invSize = SmallInvSize;
                    if (UnlockedShippingBoxExpansion())
                    {
                        invSize = LargeInvSize;
                    }
                    __instance.data.SetInventorySize(invSize);
                    __instance.data.money = GetBoxEarnings(__instance);
                    _shippingBox = __instance;
                }
            }

            [HarmonyFinalizer]
            private static Exception Finalizer()
            {
                return null;
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.ReplaceWithObject))]
        public static class WorldGameObjectReplaceWithObjectPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref WorldGameObject __instance, ref string new_obj_id)
            {
                //
                if (!UnlockedShippingBox()) return;
                if (__instance == null) return;
                if (_internalCfg.shippingBoxBuilt && _shippingBox != null) return;
                if (string.Equals(new_obj_id, "mf_box_stuff") && _shippingBuild)
                {
                    Log($"Built Shipping Box!");
                    var sbCraft = GameBalance.me.GetData<ObjectCraftDefinition>(ShippingBoxId);
                    sbCraft.hidden = true;
                    __instance.custom_tag = ShippingBoxTag;

                    _shippingBuild = false;
                    var invSize = SmallInvSize;
                    if (UnlockedShippingBoxExpansion())
                    {
                        invSize = LargeInvSize;
                    }
                    __instance.data.SetInventorySize(invSize);
                    __instance.data.drop_zone_id = ShippingBoxTag;
                    _shippingBox = __instance;

                    _internalCfg.shippingBoxBuilt = true;
                    UpdateInternalConfig();
                }
            }
        }

        [HarmonyPatch(typeof(WorldMap), nameof(WorldMap.RescanWGOsList))]
        public static class WorldMapAddVendorPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref List<WorldGameObject> ____npcs)
            {
                //
                if (____npcs == null) return;
                foreach (var npc in ____npcs.Where(npc => npc.vendor != null))
                {
                    var known =
                        MainGame.me.save.known_npcs.npcs.Exists(a => string.Equals(a.npc_id, npc.vendor.id));
                    if (known)
                    {
                        KnownVendors.Add(npc);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WorldZone), nameof(WorldZone.GetZoneWGOs))]
        public static class WorldZoneGetZoneWgOsPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref List<WorldGameObject> __result)
            {
                if (__result == null) return;
                if (_interactedObject != null && _interactedObject.obj_def.interaction_type == ObjectDefinition.InteractionType.Builder) return;
                foreach (var wgo in __result.Where(a => string.Equals(a.custom_tag, ShippingBoxTag) || string.Equals(a.data.drop_zone_id, ShippingBoxTag)))
                {
                    __result.Remove(wgo);
                    Log($"[WorldZone.GetZoneWGOs] Removed Shipping Box From WorldMap Objects");
                }

                _interactedObject = null;
            }
        }
    }
}
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using GerrysJunkTrunk.lang;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GerrysJunkTrunk
{
    public class MainPatcher
    {
        private static bool _shippingBuild;
        private static WorldGameObject _shippingBox;
        private static InternalConfig.Options _internalCfg;
        private static Config.Options _cfg;
        private static bool _usingShippingBox;
        private static WorldGameObject _myVendor;
        private const string ShippingItem = "shipping";
        private const string ShippingBoxTag = "shipping_box";
        private static readonly Dictionary<string, int> StackSizeBackups = new();
        private static string Lang { get; set; }

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.GerrysJunkTrunk");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _internalCfg = InternalConfig.GetOptions();
            _cfg = Config.GetOptions();
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

        private static void ShowIntroMessage()
        {
            GUIElements.me.dialog.OpenOK(strings.Message1, null, $"{strings.Message2}\n{strings.Message3}\n{strings.Message4}\n{strings.Message5}\n{strings.Message6}\n{strings.Message7}", true, strings.Message8);
        }

        private static bool UnlockedFullPrice()
        {
            return MainGame.me.save.unlocked_techs.Exists(a => a.ToLowerInvariant().Equals("Best friend".ToLowerInvariant()));
        }

        private static bool UnlockedShippingBox()
        {
            return MainGame.me.save.unlocked_techs.Exists(a => a.ToLowerInvariant().Equals("Wood processing".ToLowerInvariant()));
        }

        private static bool UnlockedShippingBoxExpansion()
        {
            return MainGame.me.save.unlocked_techs.Exists(a => a.ToLowerInvariant().Equals("Engineer".ToLowerInvariant()));
        }

        [HarmonyPatch(typeof(MainGame), nameof(MainGame.Update))]
        public static class MainGameUpdatePatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (!MainGame.game_started) return;

                if (_internalCfg.ShowIntroMessage)
                {
                    ShowIntroMessage();
                    _internalCfg.ShowIntroMessage = false;
                    UpdateInternalConfig();
                }

                if (!UnlockedShippingBox()) return;
                if (_internalCfg.ShippingBoxBuilt && _shippingBox == null)
                {
                    _shippingBox = Object.FindObjectsOfType<WorldGameObject>(true)
                        .FirstOrDefault(x => string.Equals(x.custom_tag, ShippingBoxTag));
                    if (_shippingBox == null)
                    {
                        Debug.LogError("[SB]: No Shipping Box Found!");
                        _internalCfg.ShippingBoxBuilt = false;
                    }
                    else
                    {
                        Debug.LogError($"[SB]: Found Shipping Box at {_shippingBox.pos3}");
                        _internalCfg.ShippingBoxBuilt = true;
                        _shippingBox.data.drop_zone_id = ShippingBoxTag;
                        var invSize = 10;
                        if (UnlockedShippingBoxExpansion())
                        {
                            invSize = 20;
                        }
                        _shippingBox.data.SetInventorySize(invSize);
                    }

                    UpdateInternalConfig();
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.DestroyMe))]
        public static class WorldGameObjectDestroyMePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref WorldGameObject __instance)
            {
                if (string.Equals(__instance.custom_tag, ShippingBoxTag))
                {
                    Debug.LogError($"[SB]: Removed Shipping Box!");
                    _shippingBox = null;
                    _internalCfg.ShippingBoxBuilt = false;
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
                if (__instance == null) return;
                {
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
                if (__instance != null)
                {
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

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs", "p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(ChestGUI), "MoveItem", typeof(Item), typeof(int), typeof(bool), typeof(Item), typeof(bool))]
        public static class ChestGuiMoveItemPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref ChestGUI __instance)
            {
                if (__instance == null || !_usingShippingBox) return;

                UpdateItemStates(ref __instance);
            }
        }

        private static void UpdateItemStates(ref ChestGUI __instance)
        {
            foreach (var inventory in __instance.player_panel.multi_inventory.all.Where(i => i.data.inventory.Count > 0))
            {
                foreach (var item in inventory.data.inventory)
                {
                    var itemCellGuiForItem = __instance.player_panel.GetItemCellGuiForItem(item);
                    itemCellGuiForItem.SetInactiveState(false);
                }

                foreach (var item in inventory.data.inventory.Where(item => item.definition.player_cant_throw_out))
                {
                    var itemCellGuiForItem = __instance.player_panel.GetItemCellGuiForItem(item);
                    itemCellGuiForItem.SetInactiveState();
                }
            }

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

        private static bool TryAdd<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key)) return false;
            dictionary.Add(key, value);
            return true;
        }

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs", "p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(ChestGUI), nameof(ChestGUI.Open))]
        public static class ChestGuiOpenPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref ChestGUI __instance)
            {
                if (__instance == null || !_usingShippingBox) return;

                var maxItemCount = 50;

                if (UnlockedFullPrice())
                {
                    maxItemCount = 100;
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

                UpdateItemStates(ref __instance);
            }
        }

        [HarmonyPatch(typeof(ChestGUI), "OnPressedBack")]
        public static class ChestGuiOnClosePressedPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref ChestGUI __instance)
            {
                if (__instance == null || !_usingShippingBox) return;
                if (StackSizeBackups.Count <= 0) return;
                foreach (var item in __instance.player_panel.multi_inventory.all[0].data.inventory)
                {
                    var found = StackSizeBackups.TryGetValue(item.id, out var value);
                    if (!found) continue;
                   // Debug.LogError($"[SB]: Restore item stack size: Item: {item.id}, StackSize: {value}");
                    item.definition.stack_count = value;
                }

                foreach (var item in __instance.chest_panel.multi_inventory.all[0].data.inventory)
                {
                    var found = StackSizeBackups.TryGetValue(item.id, out var value);
                    if (!found) continue;
                   // Debug.LogError($"[SB]: Restore item stack size: Item: {item.id}, StackSize: {value}");
                    item.definition.stack_count = value;
                }

                _usingShippingBox = false;
            }
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref WorldGameObject __instance, ref WorldGameObject other_obj)
            {
                if (string.Equals(__instance.custom_tag, ShippingBoxTag))
                {
                    Debug.LogError($"[SB]: Prefix Found Shipping Box! {__instance.data.drop_zone_id}, Other: {other_obj.obj_id}");
                    _internalCfg.ShippingBoxBuilt = true;
                    UpdateInternalConfig();
                    _usingShippingBox = true;
                    __instance.data.drop_zone_id = ShippingBoxTag;
                    var invSize = 10;
                    if (UnlockedShippingBoxExpansion())
                    {
                        invSize = 20;
                    }
                    __instance.data.SetInventorySize(invSize);
                    __instance.data.money = GetEarnings(__instance);
                    _shippingBox = __instance;
                }
            }

            [HarmonyFinalizer]
            private static Exception Finalizer()
            {
                return null;
            }
        }

        private static void UpdateInternalConfig()
        {
            InternalConfig.WriteOptions();
            _internalCfg = InternalConfig.GetOptions();
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.ReplaceWithObject))]
        public static class WorldGameObjectReplaceWithObjectPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref WorldGameObject __instance, ref string new_obj_id)
            {
                if (string.Equals(new_obj_id, "mf_box_stuff") && _shippingBuild)
                {
                    Debug.LogError($"[SB]: Shipping Build: Built Shipping Box!");
                    __instance.custom_tag = ShippingBoxTag;

                    // _shippingBoxBuilt = true;
                    var invSize = 10;
                    if (UnlockedShippingBoxExpansion())
                    {
                        invSize = 20;
                    }
                    __instance.data.SetInventorySize(invSize);
                    __instance.data.drop_zone_id = ShippingBoxTag;
                    _shippingBox = __instance;
                    _internalCfg.ShippingBoxBuilt = true;
                    UpdateInternalConfig();
                }
            }
        }

        //should never need these, but will stop a 2nd being built
        [HarmonyPatch(typeof(BuildModeLogics), nameof(BuildModeLogics.CanBuild))]
        public static class BuildItemGuiSelectPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result, ref CraftDefinition cd)
            {
                if (_internalCfg.ShippingBoxBuilt && _shippingBox != null)
                {
                    if (cd.id.Contains(ShippingItem))
                    {
                        __result = false;
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
                if (__instance == null || _myVendor == null || _myVendor.vendor == null) return;
                if (__instance.Equals(_myVendor.vendor))
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(BuildModeLogics), nameof(BuildModeLogics.OnBuildCraftSelected))]
        public static class BuildModeLogicsOnBuildCraftSelectedPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref ObjectCraftDefinition cd)
            {
                if (cd.id.Contains(ShippingItem))
                {
                    _shippingBuild = true;
                    var ocd = GameBalance.me.GetData<ObjectCraftDefinition>("mf_wood_builddesk:p:mf_box_stuff_place");
                    cd = ocd;
                }
            }
        }

        private static float GetEarnings(WorldGameObject shippingBox)
        {
            var vendor = WorldMap.GetNPCByObjID("npc_tavern owner");
            if (vendor == null)
            {
                return 0;
            }
            _myVendor = Object.Instantiate(vendor);
            _myVendor.data.money = 1000000f;
            _myVendor.vendor.cur_money = 1000000f;
            _myVendor.vendor.cur_tier = 3;
            _myVendor.vendor.definition.not_buying.Clear();

            var myTrader = new Trading(_myVendor);
            var num = 0f;

            foreach (var item in shippingBox.data.inventory)
            {
                var totalCount = shippingBox.data.GetTotalCount(item.id);
                for (var i = 0; i < totalCount; i++)
                {
                    num += Mathf.Round(myTrader.GetSingleItemCostInPlayerInventory(item, -i) * 100f) / 100f;
                }
            }

            return UnlockedFullPrice() ? num : num * 0.60f;
        }

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(InventoryPanelGUI), nameof(InventoryPanelGUI.Redraw))]
        public static class InventoryWidgetRedrawPatch
        {
            [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref List<InventoryWidget> ____widgets)
            {
                var isChest = __instance.name.ToLowerInvariant().Contains("chest");
                var isPlayer = __instance.name.ToLowerInvariant().Contains("player");
                if (_usingShippingBox && isChest && !isPlayer)
                {
                    foreach (var inventoryWidget in ____widgets)
                    {
                        inventoryWidget.header_label.text = strings.Header;
                        inventoryWidget.dont_show_empty_rows = true;
                        inventoryWidget.SetInactiveStateToEmptyCells();
                    }

                    __instance.money_label.text = Trading.FormatMoney(GetEarnings(_shippingBox), true);
                }
            }
        }

        [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class InventoryWidgetDoOpeningPatch
        {
            [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
            [HarmonyPostfix]
            public static void Postfix(ref InventoryPanelGUI __instance, ref List<InventoryWidget> ____widgets)
            {
                var isChest = __instance.name.ToLowerInvariant().Contains("chest");
                var isPlayer = __instance.name.ToLowerInvariant().Contains("player");
                if (_usingShippingBox && isChest && !isPlayer)
                {
                    foreach (var inventoryWidget in ____widgets)
                    {
                        inventoryWidget.header_label.text = strings.Header;
                        inventoryWidget.dont_show_empty_rows = true;
                        inventoryWidget.SetInactiveStateToEmptyCells();
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EnvironmentEngine), "OnEndOfDay")]
        public static class EnvironmentEngineOnEndOfDayPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (_internalCfg.ShippingBoxBuilt && _shippingBox != null)
                {
                    foreach (var item in _shippingBox.data.inventory)
                    {
                        for (var i = 0; i < item.value; i++)
                        {
                            item.OnTraded();
                        }
                    }

                    var num = GetEarnings(_shippingBox);
                    Stats.PlayerAddMoney(num,  strings.Header);
                    MainGame.me.player.data.money += num;
                    var money = Trading.FormatMoney(num, true);

                    Vector3 position;
                    float time;
                    if (_cfg.ShowSoldMessagesOnPlayer)
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

                    if (_cfg.DisableSoldMessageWhenNoSale) return;

                    if (_cfg.EnableGerry)
                    {
                        _shippingBox.Say(strings.WorkWork, delegate
                        {
                            Sounds.PlaySound("coins_sound", position, true);
                            _shippingBox.data.inventory.Clear();
                            _shippingBox.Say($"{money}", null, null, SpeechBubbleGUI.SpeechBubbleType.Talk,
                                SmartSpeechEngine.VoiceID.Skull);
                        }, null, SpeechBubbleGUI.SpeechBubbleType.Talk, SmartSpeechEngine.VoiceID.Skull, false);
                    }
                    else
                    {
                        Sounds.PlaySound("coins_sound", position, true);
                        _shippingBox.data.inventory.Clear();
                        EffectBubblesManager.ShowImmediately(position, $"{money}",
                            num > 0 ? EffectBubblesManager.BubbleColor.Green : EffectBubblesManager.BubbleColor.Red,
                            true, time);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(BaseCraftGUI), "CommonOpen")]
        public static class GameBalanceLoadGameBalancePatch
        {
            [HarmonyPostfix]
            public static void BaseCraftGUICommonOpenPostfix(ref BaseCraftGUI __instance, ref CraftComponent ___craft_component,
                ref CraftsInventory ___crafts_inventory, ref List<CraftDefinition> ___crafts)
            {
                var newCd = new ObjectCraftDefinition();
                var cd = GameBalance.me.GetData<ObjectCraftDefinition>("mf_wood_builddesk:p:mf_box_stuff_place");
                newCd.craft_in = cd.craft_in;
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
                newCd.needs_unlock = false;
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
                newCd.one_time_craft = cd.one_time_craft;
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
                newCd.id = "mf_wood_builddesk:p:mf_shipping_box_place";
                var wgo = GUIElements.me.craft.GetCrafteryWGO();
                if (wgo == null) return;
                if (wgo.obj_id.Contains("zombie")) return;
                if (!wgo.obj_id.Contains("mf_wood_builddesk")) return;
                if (!UnlockedShippingBox()) return;
                if (_internalCfg.ShippingBoxBuilt || _shippingBox != null) return;

                ___crafts.Add(newCd);
                ___crafts_inventory?.AddCraft(newCd.id);
            }
        }
    }
}
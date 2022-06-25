using HarmonyLib;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace INeedSticks;

[HarmonyAfter("p1xel8ted.GraveyardKeeper.QueueEverything")]
public class MainPatcher
{
    private static bool _craftWoodenStick;
    private static CraftDefinition _newItem;
    private static bool _queueEverything;

    //creates and adds our new object on ui open. Will only add when user has unlocked circular saw, and is interacting with it
    [HarmonyPatch(typeof(BaseCraftGUI), "CommonOpen")]
    [HarmonyPostfix]
    public static void BaseCraftGUICommonOpenPostfix(ref BaseCraftGUI __instance, ref CraftComponent ___craft_component,
        ref CraftsInventory ___crafts_inventory, ref List<CraftDefinition> ___crafts)
    {
        var newCd = new CraftDefinition();
        var cd = GameBalance.me.GetData<CraftDefinition>("wood1_2");
        var redOutputValue = _queueEverything ? 2 : 5;
        var output = new List<Item>
            {
                new("stick", 12),
                new("r", redOutputValue)
            };
        newCd.craft_in = cd.craft_in;
        newCd.needs = cd.needs;
        newCd.needs_from_wgo = cd.needs_from_wgo;
        newCd.output = output;
        newCd.output[0].min_value =
            SmartExpression
                .ParseExpression(
                    "6"); //don't think this actually does anything as I had to manually patch anyway
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
        newCd.craft_time =
            SmartExpression.ParseExpression(
                (cd.craft_time.EvaluateFloat() * 2).ToString(CultureInfo.InvariantCulture));
        newCd.energy =
            SmartExpression.ParseExpression(
                (cd.energy.EvaluateFloat() * 2).ToString(CultureInfo.InvariantCulture));
        newCd.gratitude_points_craft_cost = SmartExpression.ParseExpression(
            (cd.gratitude_points_craft_cost.EvaluateFloat() * 2).ToString(CultureInfo.InvariantCulture));
        newCd.sanity =
            SmartExpression.ParseExpression(
                (cd.sanity.EvaluateFloat() * 2).ToString(CultureInfo.InvariantCulture));
        newCd.hidden = false;
        newCd.needs_unlock = false;
        newCd.icon = "i_stick";
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
        newCd.custom_name = "Wooden stick";
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
        newCd.enqueue_type = CraftDefinition.EnqueueType.CanEnqueue;
        newCd.id = "wooden_stick";
        _newItem = newCd;

        var wgo = GUIElements.me.craft.GetCrafteryWGO();
        if (wgo == null) return;
        if (wgo.obj_id.Contains("zombie")) return;
        if (!wgo.obj_id.Contains("mf_saw") &&
            !MainGame.me.save.unlocked_techs.Contains("Circular")) return;
        ___crafts.Add(_newItem);
        ___crafts_inventory?.AddCraft(_newItem.id);
    }

    [HarmonyPatch(typeof(CraftItemGUI), "OnCraftPressed")]
    [HarmonyPrefix]
    public static void CraftItemGUIOnCraftPressedPrefix(ref CraftItemGUI __instance)
    {
        _craftWoodenStick = false || __instance.current_craft.id.Contains("wooden_stick");
    }

    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.INeedSticks");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            if (Harmony.HasAnyPatches("p1xel8ted.GraveyardKeeper.QueueEverything")) _queueEverything = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[INeedSticks]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }

    //setting minimum had no effect, this method updates the counter on the craft screen
    [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.GetCraftAmountCounter))]
    [HarmonyPostfix]
    public static void WorldGameObjectGetCraftAmountCounterPostfix(ref CraftDefinition craft_definition, ref string __result, ref int amount)
    {
        if (craft_definition.id.Contains("wooden_stick")) __result = (12 * amount).ToString();
    }

    [HarmonyPatch(typeof(CraftComponent))]
    public static class CraftComponentCraftPatch
    {
        //adds our new object as an available craft as the game checks when starting the craft
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CraftComponent.Craft))]
        public static void CraftComponentCraftPrefix(ref CraftComponent __instance)
        {
            if (_newItem == null) return;
            __instance?.crafts.Add(_newItem);
        }

        //this is required as it seems impossible to add things to GameData that actually take effect, this stops it coming back null when it doesn't find "wooden_stick" in game data
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftComponent.CraftQueueItem),"craft", MethodType.Getter)]
        public static void CraftComponentCraftQueueItemPrefix(ref CraftDefinition ____craft)
        {
            var wgo = GUIElements.me.craft.GetCrafteryWGO();
            if (wgo == null) return;
            if (wgo.obj_id.Contains("zombie")) return;
            if (!wgo.obj_id.Contains("mf_saw") &&
                !MainGame.me.save.unlocked_techs.Contains("Circular")) return;
            if (_craftWoodenStick) ____craft ??= _newItem;
        }

        //again, the output of the new object seems to be ignored despite being processed, this manually drops the craft
        [HarmonyPrefix]
        [HarmonyPatch("ProcessFinishedCraft")]
        public static void CraftComponentProcessFinishedCraftPrefix(ref CraftComponent __instance)
        {
            if (!__instance.wgo.obj_id.Contains("mf_saw")) return;
            if (!__instance.current_craft.id.Contains("wooden_stick")) return;
            var itemList = __instance.current_craft.output;
            Debug.Log($"Instance Craft: {__instance.current_craft.id}");
            foreach (var unused in itemList.Where(item => item.id.Contains("stick")))
                if (__instance.wgo.is_current_craft_gratitude)
                {
                    __instance.wgo.PutToAllPossibleInventories(itemList, out var items);
                    __instance.wgo.DropItems(items);
                }
                else
                {
                    __instance.wgo.DropItems(itemList);
                }
        }
    }
}
using HarmonyLib;
using QueueEverything.lang;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace QueueEverything;

[HarmonyAfter("p1xel8ted.GraveyardKeeper.exhaust-less", "com.glibfire.graveyardkeeper.fastercraft.mod", "com.graveyardkeeper.urbanvibes.maxbutton")]
[HarmonyBefore("p1xel8ted.GraveyardKeeper.INeedSticks")]
public static class MainPatcher
{
    private static readonly Dictionary<string, SmartExpression> Crafts = new();
    private static readonly Dictionary<string, int> FireCrafts = new();

    private static readonly string[] UnSafeCraftObjects =
    {
        "mf_crematorium_corp", "garden_builddesk", "tree_garden_builddesk", "mf_crematorium", "grave_ground",
        "tile_church_semicircle_2floors"
    };

    private static bool _alreadyRun;
    private static Config.Options _cfg;
    private static int _craftAmount = 1;
    private static bool _exhaustless;
    private static bool _fasterCraft;
    private static WorldGameObject _previousWorldGameObject;
    private static float _timeAdjustment;
    private static string Lang { get; set; }

    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.QueueEverything");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _cfg = Config.GetOptions();

            _fasterCraft = false;
            _exhaustless = false;
            _alreadyRun = false;

            Lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);

            if (Harmony.HasAnyPatches("com.glibfire.graveyardkeeper.fastercraft.mod")) _fasterCraft = true;

            if (Harmony.HasAnyPatches("p1xel8ted.GraveyardKeeper.exhaust-less")) _exhaustless = true;

            if (_fasterCraft) LoadFasterCraftConfig();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QueueEverything]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }

    [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.FillCraftsList))]
    public static class CraftComponentFillCraftsListPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!_cfg.MakeEverythingAuto) return;
            try
            {
                var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();
                GameBalance.me.craft_data.ForEach(craft =>
                {
                    if (craft.id.Contains("zombie") | craft.id.Contains("grow_desk_planting") | craft.id.Contains("refugee") |
                        craft.id.Contains("grow_vineyard_planting")) return;
                    if (!craft.is_auto)
                    {
                        var ct = craft.energy.EvaluateFloat(crafteryWgo, MainGame.me.player);
                        ct /= 2; //don't know why its getting doubled
                        Debug.LogError($"[QueueEverything]: Original CT {ct}");
                        if (_fasterCraft)
                        {
                            if (_timeAdjustment < 0)
                            {
                                ct /= _timeAdjustment;
                            }
                            else
                            {
                                ct *= _timeAdjustment;
                            }
                            craft.craft_time = SmartExpression.ParseExpression(ct.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            craft.craft_time = SmartExpression.ParseExpression(ct.ToString(CultureInfo.InvariantCulture));
                        }
                        craft.energy = SmartExpression.ParseExpression("0");
                        craft.is_auto = true;
                        craft.needs.ForEach(need =>
                        {
                            need.value *= 2;
                        });
                        craft.output.ForEach(output =>
                        {
                            Debug.LogError($"[QueueEverything]: Output: {output.id}, Value: {output.value}");
                            if (output.id is "r" or "g" or "b")
                            {
                                output.value /= 2;
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QueueEverything] {ex.Message}, {ex.Source}, {ex.StackTrace}");
            }
        }
    }

    public static void ShowMessage(string msg)
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        MainGame.me.player.Say(Lang.StartsWith("en") ? msg : strings.Message, null, false,
            SpeechBubbleGUI.SpeechBubbleType.Think,
            SmartSpeechEngine.VoiceID.None, true);
    }

    private static void LoadFasterCraftConfig()
    {
        try
        {
            const string path = "./QMods/FasterCraft/config.txt";
            var streamReader = new StreamReader(path);
            var text = streamReader.ReadLine();
            var array = text?.Split('=');
            _timeAdjustment = (float)Convert.ToDouble(array?[1], CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            Debug.LogError("Issue reading FasterCraft Config, disabling integration.");
            Debug.Log("Issue reading FasterCraft Config, disabling integration.");
            _fasterCraft = false;
        }
    }

    [HarmonyPatch(typeof(CraftDefinition), nameof(CraftDefinition.CanCraftMultiple))]
    public static class CraftDefinitionCanCraftMultiplePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool __result, WorldGameObject __state)
        {
            if (!UnSafeCraftObjects.Contains(__state.obj_id)) __result = true;
        }

        [HarmonyPrefix]
        public static void Prefix(out WorldGameObject __state)
        {
            __state = GUIElements.me.craft.GetCrafteryWGO();
        }
    }

    [HarmonyPatch(typeof(CraftDefinition), nameof(CraftDefinition.GetSpendTxt))]
    public static class CraftDefinitionGetSpendTxtPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref CraftDefinition __instance, ref WorldGameObject wgo, ref string __result,
            int multiplier = 1)
        {
            var text = "";
            int num;
            if (GlobalCraftControlGUI.is_global_control_active)
                num = __instance.gratitude_points_craft_cost is not { has_expression: true }
                    ? 0
                    : Mathf.RoundToInt(__instance.gratitude_points_craft_cost.EvaluateFloat(wgo));
            else
                num = __instance.energy is not { has_expression: true }
                    ? 0
                    : Mathf.RoundToInt(__instance.energy.EvaluateFloat(wgo));

            if (num != 0)
            {
                var num2 = 1f;
                bool flag;
                if (wgo == null)
                {
                    flag = false;
                }
                else
                {
                    var objDef = wgo.obj_def;
                    flag = objDef?.tool_actions != null;
                }

                if (flag)
                    foreach (var equippedTool in from itemType in wgo.obj_def.tool_actions.action_tools
                                                 where itemType != ItemDefinition.ItemType.Hand
                                                 select MainGame.me.player.GetEquippedTool(itemType))
                    {
                        bool flag2;
                        if (equippedTool == null)
                        {
                            flag2 = false;
                        }
                        else
                        {
                            var definition = equippedTool.definition;
                            flag2 = definition?.tool_energy_k != null;
                        }

                        if (!flag2 || !equippedTool.definition.tool_energy_k.has_expression) continue;
                        var num3 = equippedTool.definition.tool_energy_k.EvaluateFloat(wgo, MainGame.me.player);
                        if (num3 < num2) num2 = num3;
                    }

                if (!num2.EqualsTo(1f, 0.01f)) num = Mathf.RoundToInt(num * num2);

                if (GlobalCraftControlGUI.is_global_control_active)
                {
                    var gratitudePoints = MainGame.me.player.gratitude_points;
                    var smartExpression = __instance.gratitude_points_craft_cost;
                    if (num > 0) num *= _craftAmount;

                    if (_exhaustless)
                    {
                        var adjustedNum = (float)Math.Round(num / 2f, 2);
                        if (gratitudePoints < (smartExpression?.EvaluateFloat(MainGame.me.player) ?? 0f))
                        {
                            if (adjustedNum % 1 == 0)
                                text = text + "(gratitude_points)[c][ff1111]" + adjustedNum.ToString("0") + "[/c]";
                            else
                                text = text + "(gratitude_points)[c][ff1111]" + adjustedNum.ToString("0.0") +
                                       "[/c]";
                        }
                        else
                        {
                            if (adjustedNum % 1 == 0)
                                text = text + "[c](gratitude_points)[/c]" + adjustedNum.ToString("0");
                            else
                                text = text + "[c](gratitude_points)[/c]" + adjustedNum.ToString("0.0");
                        }
                    }
                    else
                    {
                        if (gratitudePoints < (smartExpression?.EvaluateFloat(MainGame.me.player) ?? 0f))
                            text = text + "(gratitude_points)[c][ff1111]" + num + "[/c]";
                        else
                            text = text + "[c](gratitude_points)[/c]" + num;
                    }
                }
                else
                {
                    if (num > 0) num *= _craftAmount;

                    if (_exhaustless)
                    {
                        var adjustedNum = (float)Math.Round(num / 2f, 2);
                        if (adjustedNum % 1 == 0)
                            text = text + "[c](en)[/c]" + adjustedNum.ToString("0");
                        else
                            text = text + "[c](en)[/c]" + adjustedNum.ToString("0.0");
                    }
                    else
                    {
                        text = text + "[c](en)[/c]" + num;
                    }
                }
            }

            if (__instance.is_auto)
            {
                float num4 = 0; // = __instance.craft_time is not {has_expression: true}
                //    ? 0
                //    : __instance.craft_time.EvaluateFloat(wgo);

                var found = Crafts.TryGetValue(__instance.id, out var value);
                if (found)
                    num4 = value is not { has_expression: true }
                        ? 0
                        : value.EvaluateFloat(wgo);

                if (num4 != 0)
                {
                    if (_fasterCraft)
                    {
                        if (_timeAdjustment < 0)
                            num4 *= _timeAdjustment;
                        else
                            num4 /= _timeAdjustment;
                    }

                    if (num4 > 0) num4 *= _craftAmount;

                    var timeSpan = TimeSpan.FromSeconds(Mathf.RoundToInt(num4));
                    text = text.ConcatWithSeparator(timeSpan.Hours >= 1
                        ? $"[c](time)[/c]{timeSpan.Hours:0}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
                        : $"[c](time)[/c]{timeSpan.Minutes:0}:{timeSpan.Seconds:00}");
                }
            }

            //stops the fire price being the same on everything as the first one you visited
            var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();
            if (_previousWorldGameObject == null) _previousWorldGameObject = crafteryWgo;

            if (!string.Equals(crafteryWgo.obj_id, _previousWorldGameObject.obj_id))
            {
                FireCrafts.Clear();
                _previousWorldGameObject = crafteryWgo;
            }

            foreach (var item in __instance.needs_from_wgo.Where(item => item.id == "fire"))
            {
                var found = FireCrafts.TryGetValue(__instance.id, out var value);
                if (!found)
                    FireCrafts.Add(__instance.id, item.value);
                else
                    item.value = (int)Math.Round((double)value / 2, MidpointRounding.AwayFromZero);

                var amount = item.value * multiplier;
                var text2 = amount % 1 == 0
                    ? $"[c](fire2)[/c]{item.value * multiplier:0}"
                    : $"[c](fire2)[/c]{item.value * multiplier:0.0}";
                if (!wgo.data.IsEnoughItems(item, "", 0, multiplier)) text2 = "[ff1111]" + text2 + "[/c]";

                text = text.ConcatWithSeparator(text2);
            }

            bool flag3;
            if (wgo == null)
            {
                flag3 = false;
            }
            else
            {
                var objDef2 = wgo.obj_def;
                flag3 = objDef2?.tool_actions != null;
            }

            if (flag3)
                for (var j = 0; j < wgo.obj_def.tool_actions.action_tools.Count; j++)
                {
                    var itemType2 = wgo.obj_def.tool_actions.action_tools[j];
                    if (itemType2 == ItemDefinition.ItemType.Hand) continue;
                    var text3 = itemType2.ToString().ToLower();
                    var num5 = wgo.obj_def.tool_actions.action_k[j];
                    var num6 = Mathf.FloorToInt(100f * num5);
                    var equippedTool2 = MainGame.me.player.GetEquippedTool(itemType2);
                    if (equippedTool2 == null)
                    {
                        text = text + "\n[c][ff1111](" + text3 + "_s)[-][/c]";
                    }
                    else
                    {
                        num6 = Mathf.FloorToInt(num6 * equippedTool2.definition.efficiency);
                        text += $"\n[c]({text3}_s)[/c]\n{num6}%";
                    }
                }

            __result = text;
        }

        [HarmonyPrefix]
        public static bool Prefix()
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(CraftGUI), nameof(CraftGUI.ExpandItem))]
    public static class CraftGuiExpandItemPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref CraftGUI __instance, ref CraftItemGUI craft_item_gui)
        {
            if (_cfg.AutoSelectCraftButtonWithController && LazyInput.gamepad_active)
                foreach (var uiButton in craft_item_gui.GetComponentsInChildren<UIButton>())
                {
                    if (!uiButton.name.Contains("craft")) continue;
                    __instance.gamepad_controller.SetFocusedItem(uiButton.GetComponent<GamepadNavigationItem>());
                    uiButton.gameObject.SetActive(true);
                }
        }
    }

    [HarmonyPatch(typeof(CraftGUI), nameof(CraftGUI.Open))]
    public static class CraftGuiOpenPatch
    {
        private static string previousObjId;

        [HarmonyPostfix]
        public static void Postfix()
        {
            _alreadyRun = true;
            var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();
            Debug.LogError(
                $"[QueueEverything] CraftGUI Open CrafteryWGO: {crafteryWgo.obj_id}, {crafteryWgo.name}");
            if (string.Equals(crafteryWgo.obj_id, previousObjId)) return;
            previousObjId = crafteryWgo.obj_id;
            File.AppendAllText("./QMods/QueueEverything/interacted-objects.txt", crafteryWgo.obj_id + "\n");
        }

        [HarmonyPrefix]
        public static void Prefix()
        {
            _craftAmount = 1;
            _alreadyRun = false;
        }
    }

    [HarmonyPatch(typeof(CraftGUI), "SwitchTab")]
    public static class CraftGuiSwitchTabPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            _alreadyRun = true;
        }

        [HarmonyPrefix]
        public static void Prefix()
        {
            _craftAmount = 1;
            _alreadyRun = false;
        }
    }

    [HarmonyPatch(typeof(CraftItemGUI), nameof(CraftItemGUI.Draw))]
    public static class CraftItemGuiDrawPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref CraftDefinition craft_definition, ref int ____amount)
        {
            var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();

            ____amount = 1;
            _craftAmount = 1;

            if (_previousWorldGameObject == null) _previousWorldGameObject = crafteryWgo;

            if (!string.Equals(crafteryWgo.obj_id, _previousWorldGameObject.obj_id))
            {
                FireCrafts.Clear();
                _previousWorldGameObject = crafteryWgo;
            }

            foreach (var item in craft_definition.needs_from_wgo.Where(item => item.id == "fire"))
            {
                var foundFire = FireCrafts.TryGetValue(craft_definition.id, out var fireValue);
                if (!foundFire)
                {
                    FireCrafts.Add(craft_definition.id, item.value);
                }
                else
                {
                    if (_cfg.HalfFireRequirements)
                        item.value = (int)Math.Round((double)fireValue / 2, MidpointRounding.AwayFromZero);
                    else
                        item.value = fireValue;
                }
            }

            var found = Crafts.TryGetValue(craft_definition.id, out _);
            if (!found) Crafts.Add(craft_definition.id, craft_definition.craft_time);

            Crafts.TryGetValue(craft_definition.id, out var value);

            if (_fasterCraft)
            {
                var ct = value?.EvaluateFloat(crafteryWgo);
              
                    if (_timeAdjustment < 0)
                    {
                        ct *= _timeAdjustment;
                        craft_definition.craft_time = SmartExpression.ParseExpression(ct.ToString());
                    }
                    else
                    {
                        ct /= _timeAdjustment;
                        craft_definition.craft_time = SmartExpression.ParseExpression(ct.ToString());
                    }

            }
            else
            {

                    craft_definition.craft_time = value;
                
            }
        }
    }

    [HarmonyPatch(typeof(CraftItemGUI), "OnCraftPressed")]
    public static class CraftItemGuiOnCraftPressedPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref CraftItemGUI __instance, ref int ____amount)
        {
            var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();

            if (UnSafeCraftObjects.Contains(crafteryWgo.obj_id)) return;

            var found = Crafts.TryGetValue(__instance.craft_definition.id, out var value);
            float originalTimeFloat;
            if (found)
            {
                originalTimeFloat = value.EvaluateFloat(crafteryWgo);
            }
            else
            {
                Crafts.Add(__instance.craft_definition.id, __instance.craft_definition.craft_time);
                originalTimeFloat =
                    __instance.craft_definition.craft_time.EvaluateFloat(crafteryWgo);
            }

            __instance.craft_definition.craft_time =
                SmartExpression.ParseExpression(originalTimeFloat.ToString(CultureInfo.InvariantCulture));

            var time = originalTimeFloat;

            if (_fasterCraft)
            {
                if (_timeAdjustment < 0)
                    time *= _timeAdjustment;
                else
                    time /= _timeAdjustment;
            }

            // _craftAmount = ____amount;
            time *= ____amount;
            Debug.LogError($"[QueueEverything]: Item: {__instance.craft_definition.id}, CraftTime: {time}");
            
            if (time >= 300)
            {
                var lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);

                var endTime = time / 60;
                var message = endTime % 1 == 0
                    ? $"Hmmm guess I'll come back in {time / 60:0} minutes..."
                    : $"Hmmm guess I'll come back in roughly {time / 60:0} minutes...";
                MainGame.me.player.Say(
                    !lang.Contains("en")
                        ? strings.Message
                        : message, null, false,
                    SpeechBubbleGUI.SpeechBubbleType.Think,
                    SmartSpeechEngine.VoiceID.None, true);
            }
        }
    }

    [HarmonyPatch(typeof(CraftItemGUI), nameof(CraftItemGUI.Redraw))]
    public static class CraftItemGuiRedrawPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref CraftItemGUI __instance, ref List<string> ____multiquality_ids,
            ref int ____amount)
        {
            _craftAmount = ____amount;

            if (_alreadyRun) return;
            List<int> craftable = new();
            List<int> notCraftable = new();
            craftable.Clear();
            notCraftable.Clear();
            var isMultiQualCraft = false;
            var bCraftable = 0;
            var sCraftable = 0;
            var gCraftable = 0;
            var multiInventory = !GlobalCraftControlGUI.is_global_control_active
                ? MainGame.me.player.GetMultiInventoryForInteraction()
                : GUIElements.me.craft.multi_inventory;
            //const string path = "./qmods/multis.txt";
            //var message = crafteryWgo.obj_id+"\n---------------------\n" + "Item: " + __instance.current_craft.id + ", Craft Def: " + __instance.craft_definition.id + "\n";
            for (var i = 0; i < ____multiquality_ids.Count; i++)
                if (string.IsNullOrWhiteSpace(____multiquality_ids[i]))
                {
                    var itemCount = multiInventory.GetTotalCount(__instance.current_craft.needs[i].id);
                    var itemNeed = __instance.current_craft.needs[i].value;
                    var itemCraftable = itemCount / itemNeed;
                    if (itemCraftable != 0)
                        craftable.Add(itemCraftable);
                    else
                        notCraftable.Add(itemCraftable);
                    //message += "Item: " + __instance.current_craft.needs[i].id + ", Icon: "+ __instance.current_craft.needs[i].GetIcon() + ", Stock: " + itemCount +
                    //           ", Craftable: " + itemCraftable + "\n";
                    //message += " - Required for craft: " + itemNeed + "\n";
                }
                else
                {
                    isMultiQualCraft = true;
                    var itemValueNeeded = __instance.current_craft.needs[i].value;
                    var bStarItem = multiInventory.GetTotalCount(__instance.current_craft.needs[i].id + ":1");
                    var sStarItem = multiInventory.GetTotalCount(__instance.current_craft.needs[i].id + ":2");
                    var gStarItem = multiInventory.GetTotalCount(__instance.current_craft.needs[i].id + ":3");
                    bCraftable = bStarItem / itemValueNeeded;
                    sCraftable = sStarItem / itemValueNeeded;
                    gCraftable = gStarItem / itemValueNeeded;
                    if (bCraftable != 0) craftable.Add(bCraftable);
                    if (sCraftable != 0) craftable.Add(sCraftable);
                    if (gCraftable != 0) craftable.Add(gCraftable);

                    //message += "MQ Item " + i + ": " + ____multiquality_ids[i] + "\n - Gold Stock: " + gStarItem +
                    //           ", Craftable: " + gStarItem / itemValueNeeded + "\n";
                    //message += " - Silver Stock: " + sStarItem + ", Craftable: " + sStarItem / itemValueNeeded +
                    //           "\n";
                    //message += " - Bronze Stock: " + bStarItem + ", Craftable: " + bCraftable + "\n";
                    //message += " - Required for craft: " + __instance.current_craft.needs[i].value + "\n";
                    if (bCraftable + sCraftable + gCraftable > 0)
                        if (_cfg.AutoSelectHighestQualRecipe)
                        {
                            if (gCraftable > 0)
                                ____multiquality_ids[i] = __instance.current_craft.needs[i].id + ":3";
                            else if (sCraftable > 0)
                                ____multiquality_ids[i] = __instance.current_craft.needs[i].id + ":2";
                            else
                                ____multiquality_ids[i] = __instance.current_craft.needs[i].id + ":1";
                        }
                }

            var m1 = 0;
            if (craftable.Count > 0) m1 = craftable.Min();

            var multiMin = Mathf.Max(bCraftable, sCraftable, gCraftable);
            var min = multiMin <= 0 ? m1 : Math.Min(m1, multiMin);

            if (_cfg.AutoMaxMultiQualCrafts)
                if (isMultiQualCraft && multiMin != 0)
                {
                    _craftAmount = min;
                    ____amount = min;
                }

            if (_cfg.AutoMaxNormalCrafts)
                if (!isMultiQualCraft)
                    if (notCraftable.Count <= 0)
                    {
                        _craftAmount = min;
                        ____amount = min;
                    }

            //message += "Max Craftable: " + min + "\n";
            //message += "Ingredient list count: " + craftable.Count + "\n";
            //message += "---------------------\n";
            //File.AppendAllText(path, message);
        }
    }

    [HarmonyPatch(typeof(InGameMenuGUI), nameof(InGameMenuGUI.OnClosePressed))]
    public static class InGameMenuGuiOnClosePressedPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        }
    }
}
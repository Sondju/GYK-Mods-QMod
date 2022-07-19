using FlowCanvas.Nodes;
using HarmonyLib;
using Helper;
using QueueEverything.lang;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace QueueEverything;

[HarmonyAfter("p1xel8ted.GraveyardKeeper.exhaust-less", "com.glibfire.graveyardkeeper.fastercraft.mod", "com.graveyardkeeper.urbanvibes.maxbutton")]
[HarmonyBefore("p1xel8ted.GraveyardKeeper.INeedSticks")]
public static class MainPatcher
{
    private static readonly Dictionary<string, SmartExpression> Crafts = new();
    private static readonly Dictionary<string, int> FireCrafts = new();
    private static bool _unsafeInteraction;

    //unsafe crafting objects as a whole
    private static readonly string[] UnSafeCraftObjects =
    {
        "mf_crematorium_corp", "garden_builddesk", "tree_garden_builddesk", "mf_crematorium", "grave_ground",
        "tile_church_semicircle_2floors", "mf_grindstone_1", "zombie_garden_desk_1", "zombie_garden_desk_2", "zombie_garden_desk_3",
        "zombie_vineyard_desk_1", "zombie_vineyard_desk_2", "zombie_vineyard_desk_3", "graveyard_builddesk", "blockage_H_low", "blockage_V_low",
        "blockage_H_high", "blockage_V_high", "wood_obstacle_v", "refugee_camp_garden_bed", "refugee_camp_garden_bed_1", "refugee_camp_garden_bed_2",
        "refugee_camp_garden_bed_3", "carrot_box", "elevator_top", "zombie_crafting_table", "mf_balsamation","mf_balsamation_1","mf_balsamation_2",
        "mf_balsamation_3"
    };

    //individual zones
    private static readonly string[] UnSafeCraftZones =
    {
        "church"
    };

    //individual craft definitions
    private static readonly string[] UnSafeCraftDefPartials =
    {
        "soul_workbench_craft", "remove", "zombie", "refugee"
    };

    private static readonly string[] UnSafePartials =
    {
        "blockage", "obstacle", "builddesk", "fix", "broken", "elevator", "zombie", "refugee"
    };

    private static readonly CraftDefinition.CraftType[] UnSafeCraftTypes =
    {
       // CraftDefinition.CraftType.PrayCraft, CraftDefinition.CraftType.Fixing
    };

    private static readonly string[] UnSafeItems =
    {
        "zombie","grow_desk_planting","refugee","grow_vineyard_planting", "axe", "hammer", "shovel", "sword", "mf_balsamation"
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

            _fasterCraft = Harmony.HasAnyPatches("com.glibfire.graveyardkeeper.fastercraft.mod");
            _exhaustless = Tools.IsModLoaded("Exhaustless") || Harmony.HasAnyPatches("p1xel8ted.GraveyardKeeper.exhaust-less");

            if (_fasterCraft) LoadFasterCraftConfig();
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("QueueEverything", $"{message}", error);
    }

    private static bool IsUnsafeDefinition(CraftDefinition _craftDefinition)
    {
        return UnSafeCraftDefPartials.Any(_craftDefinition.id.Contains) || _craftDefinition.one_time_craft;
    }

    [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
    public static class WorldGameObjectInteractPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref WorldGameObject __instance)
        {
            if (UnSafeCraftZones.Contains(__instance.GetMyWorldZoneId()) || UnSafePartials.Any(__instance.obj_id.Contains) || UnSafeCraftObjects.Contains(__instance.obj_id))
            {
                _unsafeInteraction = true;
                // Log($"Object: {__instance.obj_id}, Zone: {__instance.GetMyWorldZoneId()}, Custom Tag: {__instance.custom_tag}");
            }
            else
            {
                _unsafeInteraction = false;
                // Log($"UNKNOWN/SAFE?: Object: {__instance.obj_id}, Zone: {__instance.GetMyWorldZoneId()}, Custom Tag: {__instance.custom_tag}");
            }
        }

        //[HarmonyPostfix]
        //public static void Postfix(ref WorldGameObject __instance, WorldGameObject other_obj)
        //{
        //    var craftery = GUIElements.me.craft.GetCrafteryWGO();
        //    if (craftery == null || craftery.has_linked_worker) return;
        //    var dockPoint = __instance.RefindDockPointsAndGet();
        //    if (other_obj.is_player)
        //    {

        //        var data = GameBalance.me.GetData<WorkerDefinition>("worker_zombie_1");
        //        var item = MainGame.me.save.GenerateBody(1, 1, -1, -1);
        //        var worker_wgo = WorldMap.SpawnWGO(MainGame.me.world_root, data.worker_wgo, new Vector3?(dockPoint[0].transform.position));
        //        var worker = MainGame.me.save.workers.CreateNewWorker(worker_wgo, data.id, item);
        //        worker.ForcingWorkerK(true, 1f);
        //        //worker.UpdateWorkerLevel();
        //        craftery.linked_worker = worker_wgo;
        //        worker_wgo.enabled = false;

        //    } 
        //    Log($"Object: {__instance.obj_id}, Other: {other_obj.obj_id}, Craftery: {GUIElements.me.craft.GetCrafteryWGO().obj_id}, Worker Attached: {craftery.has_linked_worker}");
        //}
    }

    [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.FillCraftsList))]
    public static class CraftComponentFillCraftsListPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (_unsafeInteraction) return;
            if (!_cfg.MakeEverythingAuto) return;
            try
            {
                var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();
                foreach (var craft in GameBalance.me.craft_data)
                {
                    if (craft.is_auto) continue;

                    var zombieCraft = craft.craft_in.Any(craftIn => craftIn.Contains("zombie"));
                    var refugeeCraft = craft.craft_in.Any(craftIn => craftIn.Contains("refugee"));
                    var graveCraft = false;
                    var bodyCraft = false;
                    var cookingTableCraft = false;
                    if (!_cfg.MakeHandTasksAuto)
                    {
                        graveCraft = craft.craft_in.Any(craftIn => craftIn.Contains("grave"));
                        bodyCraft = craft.craft_in.Any(craftIn => craftIn.Contains("mf_preparation"));
                        cookingTableCraft = craft.craft_in.Any(craftIn => craftIn.Contains("cooking_table") && !refugeeCraft);
                    }

                    if (UnSafeItems.Any(craft.id.Contains) || refugeeCraft || cookingTableCraft || zombieCraft || graveCraft || bodyCraft ||
                       UnSafeCraftTypes.Contains(craft.craft_type) || craft.craft_time_is_zero || craft.one_time_craft) continue;

                    var ct = craft.energy.EvaluateFloat(crafteryWgo, MainGame.me.player);

                    //ct *= 1.5f;
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
                    //craft.needs.ForEach(need =>
                    //{
                    //    //int value = need.value * 10 / 100;
                    //    need.value += (int) Math.Ceiling((decimal) need.value * 25 / 100);
                    //});
                    craft.output.ForEach(output =>
                    {
                        if (output.id is not ("r" or "g" or "b")) return;
                        output.value /= 2;
                        if (output.value < 1)
                        {
                            output.value = 1;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
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
            Log("Issue reading FasterCraft Config, disabling integration.");
            _fasterCraft = false;
        }
    }

    [HarmonyPatch(typeof(CraftDefinition), nameof(CraftDefinition.CanCraftMultiple))]
    public static class CraftDefinitionCanCraftMultiplePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref CraftDefinition __instance, ref bool __result)
        {
            if (_unsafeInteraction || IsUnsafeDefinition(__instance))
            {
                //Log($"Unsafe Craft: {__instance.id}");
                return;
            }
            __result = true;
        }
    }

    [HarmonyPatch(typeof(CraftDefinition), nameof(CraftDefinition.GetSpendTxt))]
    public static class CraftDefinitionGetSpendTxtPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref CraftDefinition __instance, ref WorldGameObject wgo, ref string __result,
            int multiplier = 1)
        {
            if (_unsafeInteraction) return;
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
                float num4 = 0;

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
                {
                    FireCrafts.Add(__instance.id, item.value);
                }
                else
                {
                    if (_cfg.HalfFireRequirements)
                    {
                        item.value = (int)Math.Round((double)value / 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        item.value = value;
                    }
                }

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
            if (_unsafeInteraction) return;
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
            if (_unsafeInteraction) return;
            _alreadyRun = true;
            var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();

            if (string.Equals(crafteryWgo.obj_id, previousObjId)) return;
            previousObjId = crafteryWgo.obj_id;
            File.AppendAllText("./QMods/QueueEverything/interacted-objects.txt", crafteryWgo.obj_id + "\n");
        }

        [HarmonyPrefix]
        public static void Prefix()
        {
            if (_unsafeInteraction) return;
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
            if (_unsafeInteraction) return;
            _alreadyRun = true;
        }

        [HarmonyPrefix]
        public static void Prefix()
        {
            if (_unsafeInteraction) return;
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
            if (_unsafeInteraction || IsUnsafeDefinition(craft_definition)) return;
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
        public static void Prefix(ref CraftItemGUI __instance, ref int ____amount, ref WorldGameObject __state)
        {
            // Log($"Craft: {__instance.craft_definition.id}, One time: {__instance.craft_definition.one_time_craft}");
            if (_unsafeInteraction || IsUnsafeDefinition(__instance.craft_definition)) return;

            var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();
            __state = crafteryWgo;
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

            time *= ____amount;

            if (time >= 300 && !_cfg.DisableComeBackLaterThoughts)
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

        [HarmonyPostfix]
        public static void Postfix(WorldGameObject __state)
        {
            if (!_cfg.MakeEverythingAuto) return;
            if (__state == null) return;
            if (__state.linked_worker != null) return;
            if (__state.has_linked_worker) return;
            currentlyCrafting.Add(__state);
            __state.OnWorkAction();

        }
    }

    private static readonly List<WorldGameObject> currentlyCrafting = new();
    private static bool _craftsStarted;

    [HarmonyPatch(typeof(MainGame), nameof(MainGame.Update))]
    public static class MainGameUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!MainGame.game_started) return;
            if (!_cfg.MakeEverythingAuto) return;
            if (_craftsStarted) return;

            foreach (var wgo in MainGame.me.world.GetComponentsInChildren<WorldGameObject>(true))
            {
                if (wgo.components.craft.is_crafting && !wgo.has_linked_worker)
                {
                    currentlyCrafting.Add(wgo);
                }
            }

            _craftsStarted = true;
        }
    }

    [HarmonyPatch(typeof(CraftComponent), "CraftReally")]
    public static class CraftComponentCraftReally
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!MainGame.game_started) return;
            if (!_cfg.MakeEverythingAuto) return;

            foreach (var wgo in currentlyCrafting.Where(wgo => wgo.components.craft.is_crafting && !wgo.has_linked_worker))
            {
                wgo.OnWorkAction();
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
            if (_unsafeInteraction) return;
            if (IsUnsafeDefinition(__instance.craft_definition))
            {
                _craftAmount = 1;
                ____amount = 1;
                return;
            }

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
            {
                if (isMultiQualCraft && multiMin != 0)
                {
                    _craftAmount = min;
                    ____amount = min;
                }
            }

            if (_cfg.AutoMaxNormalCrafts)
            {
                if (isMultiQualCraft) return;
                if (notCraftable.Count > 0) return;
                _craftAmount = min;
                ____amount = min;
            }
            //message += "Max Craftable: " + min + "\n";
            //message += "Ingredient list count: " + craftable.Count + "\n";
            //message += "---------------------\n";
            //File.AppendAllText(path, message);
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
}
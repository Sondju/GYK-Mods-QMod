using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using QueueEverything.lang;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace QueueEverything
{
   public class MainPatcher
    {
        public static Dictionary<string, SmartExpression> Crafts = new();
        public static bool FasterCraft, Exhaustless;
        public static float TimeAdjustment;
        //public static bool Skip;
        public static Dictionary<string, string> Objects = new();
        public static int CraftAmount = 1;
        public static SmartExpression CraftTimeBackup;

        //public static string[] SafeObjects =
        //{
        //    "oven", "barrel_brew", "brewing_stand", "mf_barrel_mid", "mf_distcube_1", "mf_distcube_2", "mf_distcube_3",
        //    "mf_furnace_1", "mf_furnace_2", "mf_furnace_3"
        //};

        //public static string[] UnSafeObjects =
        //{
        //    "mf_hammer_1",
        //    "mf_hammer_2",
        //    "mf_hammer_3",
        //    "mf_vine_press",
        //    "cellar_builddesk",
        //    "desk_1",
        //    "desk_2",
        //    "desk_3",
        //    "mf_alchemy_craft_03",
        //    "mf_alchemy_craft_02",
        //    "mf_alchemy_craft_01",
        //    "mf_printing_press_2",
        //    "mf_printing_press_1",
        //    "mf_printing_press_3",
        //    "alchemy_table_zombie",
        //    "alchemy_workbench_zombie",
        //    "morgue_builddesk",
        //    "soul_workbench",
        //    "mf_crematorium",
        //    "table_book_constr",
        //    "church_builddesk",
        //    "graveyard_builddesk",
        //    "cremation_builddesk",
        //    "carrot_box",
        //    "tree_garden_builddesk",
        //    "elevator_top",
        //    "garden_builddesk",
        //    "mf_saw_1",
        //    "mf_potter_wheel_1",
        //    "mf_potter_wheel_2",
        //    "mf_potter_wheel_3",
        //    "mf_workbench_1",
        //    "mf_workbench_2",
        //    "mf_workbench_3",
        //    "mf_anvil_1",
        //    "mf_anvil_2",
        //    "mf_anvil_3",
        //    "mf_jewelry",
        //    "mf_paper_press",
        //    "mf_wood_builddesk",
        //    "cooking_table_1",
        //    "cooking_table_2",
        //    "cooking_table_3",
        //    "keeper_room_builddesk",
        //};

        public static void ShowMessage(string msg)
        {
            var lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
            MainGame.me.player.Say(lang.StartsWith("en") ? msg : strings.Message, null, false,
                SpeechBubbleGUI.SpeechBubbleType.Think,
                SmartSpeechEngine.VoiceID.None, true);
        }

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.QueueEverything");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);

            FasterCraft = false;
            Exhaustless = false;

            if (Harmony.HasAnyPatches("com.glibfire.graveyardkeeper.fastercraft.mod"))
            {
                FasterCraft = true;
            }
            if (Harmony.HasAnyPatches("p1xel8ted.GraveyardKeeper.exhaust-less"))
            {
                Exhaustless = true;
            }

            if (FasterCraft)
            {
                LoadFasterCraftConfig();
            }

        }

        [HarmonyPatch(typeof(CraftDefinition))]
        [HarmonyPatch(nameof(CraftDefinition.CanCraftMultiple))]
        public static class ModPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }

        public static void LoadFasterCraftConfig()
        {
            try
            {
                const string path = "./QMods/FasterCraft/config.txt";
                var streamReader = new StreamReader(path);
                var text = streamReader.ReadLine();
                var array = text?.Split('=');
                TimeAdjustment = (float) Convert.ToDouble(array?[1]);
            }
            catch (Exception)
            {
                Debug.LogError("Issue reading FasterCraft Config, disabling integration.");
                Debug.Log("Issue reading FasterCraft Config, disabling integration.");
                FasterCraft = false;
            }
        }

        [HarmonyPatch(typeof(CraftItemGUI))]
        [HarmonyPatch(nameof(CraftItemGUI.OnAmountMinus))]
        public static class OnAmountMinusPatch
        {
            [HarmonyPrefix]
            public static void Prefix(int ____amount)
            {
                CraftAmount = ____amount;
                CraftAmount--;
            }
        }

        [HarmonyPatch(typeof(CraftItemGUI))]
        [HarmonyPatch(nameof(CraftItemGUI.OnAmountPlus))]
        public static class OnAmountPlusPatch
        {
            [HarmonyPrefix]
            public static void Prefix(int ____amount)
            {
                CraftAmount = ____amount;
                CraftAmount++;
            }
        }

        [HarmonyPatch(typeof(CraftDefinition))]
        [HarmonyPatch(nameof(CraftDefinition.GetSpendTxt))]
        public static class GetSpendTextPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                return false;
            }

            [HarmonyPostfix]
            public static void Postfix(ref CraftDefinition __instance, ref WorldGameObject wgo, ref string __result,
                int multiplier = 1)
            {
                var text = "";
                int num;
                if (GlobalCraftControlGUI.is_global_control_active)
                {
                    num = (__instance.gratitude_points_craft_cost is not {has_expression: true}
                        ? 0
                        : Mathf.RoundToInt(__instance.gratitude_points_craft_cost.EvaluateFloat(wgo)));
                }
                else
                {
                    num = (__instance.energy is not {has_expression: true}
                        ? 0
                        : Mathf.RoundToInt(__instance.energy.EvaluateFloat(wgo)));
                }

                if (num != 0)
                {
                    var num2 = 1f;
                    bool flag;
                    if (wgo == null)
                    {
                        flag = (false);
                    }
                    else
                    {
                        var objDef = wgo.obj_def;
                        flag = (objDef?.tool_actions != null);
                    }

                    if (flag)
                    {
                        foreach (var equippedTool in from itemType in wgo.obj_def.tool_actions.action_tools
                                 where itemType != ItemDefinition.ItemType.Hand
                                 select MainGame.me.player.GetEquippedTool(itemType))
                        {
                            bool flag2;
                            if (equippedTool == null)
                            {
                                flag2 = (false);
                            }
                            else
                            {
                                var definition = equippedTool.definition;
                                flag2 = (definition?.tool_energy_k != null);
                            }

                            if (!flag2 || !equippedTool.definition.tool_energy_k.has_expression) continue;
                            var num3 = equippedTool.definition.tool_energy_k.EvaluateFloat(wgo, MainGame.me.player);
                            if (num3 < num2)
                            {
                                num2 = num3;
                            }
                        }
                    }

                    if (!num2.EqualsTo(1f, 0.01f))
                    {
                        num = Mathf.RoundToInt(num * num2);
                    }

                    if (GlobalCraftControlGUI.is_global_control_active)
                    {
                        var gratitudePoints = MainGame.me.player.gratitude_points;
                        var smartExpression = __instance.gratitude_points_craft_cost;
                        if (gratitudePoints < (smartExpression?.EvaluateFloat(MainGame.me.player) ?? 0f))
                        {
                            text = text + "(gratitude_points)[c][ff1111]" + num + "[/c]";
                        }
                        else
                        {
                            text = text + "[c](gratitude_points)[/c]" + num;
                        }
                    }
                    else
                    {
                        if (Exhaustless)
                        {
                            var adjustedNum = (float)Math.Round((num * CraftAmount) / 2f, 2);
                            if (adjustedNum % 1 == 0)
                            {
                                text = text + "[c](en)[/c]" + adjustedNum.ToString("0");
                            }
                            else
                            {
                                text = text + "[c](en)[/c]" + adjustedNum.ToString("0.0");
                            }
                        }
                        else
                        {
                            text = text + "[c](en)[/c]" + num;
                        }
                    }
                }

                if (__instance.is_auto)
                {
                    var num4 = __instance.craft_time is not {has_expression: true}
                        ? 0
                        : __instance.craft_time.EvaluateFloat(wgo);
                    if (num4 != 0)
                    {
                        num4 *= CraftAmount;
                        if (FasterCraft)
                        {
                            if (TimeAdjustment < 0)
                            {
                                num4 *= TimeAdjustment;
                            }
                            else
                            {
                                num4 /= TimeAdjustment;
                            }
                        }

                        var timeSpan = TimeSpan.FromSeconds(Mathf.RoundToInt(num4));
                        text = text.ConcatWithSeparator(timeSpan.Hours >= 1
                            ? $"[c](time)[/c]{timeSpan.Hours:0}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
                            : $"[c](time)[/c]{timeSpan.Minutes:0}:{timeSpan.Seconds:00}");
                    }
                }

                foreach (var item in __instance.needs_from_wgo)
                {
                    if (item.id != "fire") continue;
                    var text2 = $"[c](fire2)[/c]{item.value * multiplier:0}";
                    if (!wgo.data.IsEnoughItems(item, "", 0, multiplier))
                    {
                        text2 = "[ff1111]" + text2 + "[/c]";
                    }

                    text = text.ConcatWithSeparator(text2);
                }

                bool flag3;
                if (wgo == null)
                {
                    flag3 = (false);
                }
                else
                {
                    var objDef2 = wgo.obj_def;
                    flag3 = (objDef2?.tool_actions != null);
                }

                if (flag3)
                {
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
                }

                File.WriteAllText("./qmods/resulttext.txt", text);
                 __result = text;
                
            }
        }


        [HarmonyPatch(typeof(CraftItemGUI))]
        [HarmonyPatch(nameof(CraftItemGUI.Draw))]
        public static class DrawPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref CraftDefinition craft_definition)
            {
                CraftAmount = 1;
                var found = Crafts.TryGetValue(craft_definition.id, out var value);
                if (!found)
                {
                    Crafts.Add(craft_definition.id, craft_definition.craft_time);
                }
                else
                {
                    craft_definition.craft_time = value;
                }
            }
        }


        [HarmonyPatch(typeof(CraftItemGUI), "OnCraftPressed", MethodType.Normal)]
        public static class OnCraftPressedPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref CraftItemGUI __instance)
            {
                var craft = GUIElements.me.craft;
                var crafteryWgo = craft.GetCrafteryWGO();

                CraftTimeBackup = __instance.craft_definition.craft_time;
                var originalTimeFloat = CraftTimeBackup.EvaluateFloat(crafteryWgo, MainGame.me.player);

                if (FasterCraft)
                {
                    if (TimeAdjustment < 0)
                    {
                        originalTimeFloat *= TimeAdjustment;
                    }
                    else
                    {
                        originalTimeFloat /= TimeAdjustment;
                    }
                }

                __instance.craft_definition.craft_time =
                    SmartExpression.ParseExpression(originalTimeFloat.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
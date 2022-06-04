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
        public static Dictionary<string, int> FireCrafts = new();
        public static bool FasterCraft, Exhaustless, MaxButton;
        public static float TimeAdjustment;
        public static Dictionary<string, string> Objects = new();
        public static int CraftAmount = 1;
        public static SmartExpression CraftTimeBackup;
        private static Config.Options _cfg;
        public static WorldGameObject PreviouWorldGameObject;

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

            _cfg = Config.GetOptions();

            FasterCraft = false;
            Exhaustless = false;
            MaxButton = false;

            if (Harmony.HasAnyPatches("com.glibfire.graveyardkeeper.fastercraft.mod"))
            {
                FasterCraft = true;
            }

            if (Harmony.HasAnyPatches("p1xel8ted.GraveyardKeeper.exhaust-less"))
            {
                Exhaustless = true;
            }

            if (Harmony.HasAnyPatches("com.graveyardkeeper.urbanvibes.maxbutton"))
            {
                MaxButton = true;
            }

            if (FasterCraft)
            {
                LoadFasterCraftConfig();
            }
        }

        [HarmonyPatch(typeof(CraftItemGUI))]
        [HarmonyPatch(nameof(CraftItemGUI.Redraw))]
        public static class CraftItemGuiRedrawPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref int ____amount)
            {
                CraftAmount = ____amount;
            }
        }

        [HarmonyPatch(typeof(CraftDefinition))]
        [HarmonyPatch(nameof(CraftDefinition.CanCraftMultiple))]
        public static class CraftDefinitionCanCraftMultiplePatch
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

        [HarmonyPatch(typeof(CraftDefinition))]
        [HarmonyPatch(nameof(CraftDefinition.GetSpendTxt))]
        public static class CraftDefinitionGetSpendTxtPatch
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
                        if (num > 0)
                        {
                            num *= CraftAmount;
                        }

                        if (Exhaustless)
                        {
                            var adjustedNum = (float) Math.Round(num / 2f, 2);
                            if (gratitudePoints < (smartExpression?.EvaluateFloat(MainGame.me.player) ?? 0f))
                            {
                                if (adjustedNum % 1 == 0)
                                {
                                    text = text + "(gratitude_points)[c][ff1111]" + adjustedNum.ToString("0") + "[/c]";
                                }
                                else
                                {
                                    text = text + "(gratitude_points)[c][ff1111]" + adjustedNum.ToString("0.0") +
                                           "[/c]";
                                }
                            }
                            else
                            {
                                if (adjustedNum % 1 == 0)
                                {
                                    text = text + "[c](gratitude_points)[/c]" + adjustedNum.ToString("0");
                                }
                                else
                                {
                                    text = text + "[c](gratitude_points)[/c]" + adjustedNum.ToString("0.0");
                                }
                            }
                        }
                        else
                        {
                            if (gratitudePoints < (smartExpression?.EvaluateFloat(MainGame.me.player) ?? 0f))
                            {
                                text = text + "(gratitude_points)[c][ff1111]" + num + "[/c]";
                            }
                            else
                            {
                                text = text + "[c](gratitude_points)[/c]" + num;
                            }
                        }
                    }
                    else
                    {
                        if (num > 0)
                        {
                            num *= CraftAmount;
                        }

                        if (Exhaustless)
                        {
                            var adjustedNum = (float) Math.Round(num / 2f, 2);
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
                        if (num4 > 0)
                        {
                            num4 *= CraftAmount;
                        }

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

                //stops the fire price being the same on everything as the first one you visited
                var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();
                if (PreviouWorldGameObject == null)
                {
                    PreviouWorldGameObject = crafteryWgo;
                }

                if (!string.Equals(crafteryWgo.obj_id, PreviouWorldGameObject.obj_id))
                {
                    FireCrafts.Clear();
                    PreviouWorldGameObject = crafteryWgo;
                }

                foreach (var item in __instance.needs_from_wgo.Where(item => item.id == "fire"))
                {
                    var found = FireCrafts.TryGetValue(item.id, out var value);
                    if (!found)
                    {
                        FireCrafts.Add(item.id, item.value);
                    }
                    else
                    {
                        item.value = (int) Math.Round((double) value / 2, MidpointRounding.AwayFromZero);
                    }

                    var amount = item.value * multiplier;
                    var text2 = amount % 1 == 0 ? $"[c](fire2)[/c]{item.value * multiplier:0}" : $"[c](fire2)[/c]{item.value * multiplier:0.0}";
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
        public static class CraftItemGuiDrawPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref CraftItemGUI __instance, ref CraftDefinition craft_definition)
            {
                CraftAmount = 1;

                foreach (var item in craft_definition.needs_from_wgo.Where(item => item.id == "fire"))
                {
                    var foundFire = FireCrafts.TryGetValue(item.id, out var fireValue);
                    if (!foundFire)
                    {
                        FireCrafts.Add(item.id, item.value);
                    }
                    else
                    {
                        if (_cfg.HalfFireRequirements)
                        {
                            item.value = (int) Math.Round((double) fireValue / 2, MidpointRounding.AwayFromZero);
                        }
                        else
                        {
                            item.value = fireValue;
                        }
                    }
                }

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
        public static class CraftItemGuiOnCraftPressedPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref CraftItemGUI __instance)
            {
                var crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();
                if (crafteryWgo.obj_id.Contains("build")) return;

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
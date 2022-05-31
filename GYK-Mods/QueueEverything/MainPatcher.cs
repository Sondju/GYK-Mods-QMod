using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Debug = UnityEngine.Debug;

namespace QueueEverything
{
    public class MainPatcher
    {
        public static Dictionary<string, float> Crafts = new();
        public static bool FasterCraft;
        public static float TimeAdjustment;

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.QueueEverything");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!asm.FullName.Contains("FasterCraft")) continue;
                Debug.LogError("FasterCraft Found");
                FasterCraft = true;
                break;

            }

            if (FasterCraft)
            {
                LoadFasterCraftConfig();
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
                FasterCraft = false;
            }
        }

        [HarmonyPatch(typeof(CraftItemGUI))]
        [HarmonyPatch(nameof(CraftItemGUI.Draw))]
        public static class DrawPatch
        {
            internal static Dictionary<string, string> FoundCrafts = new();
            internal static SmartExpression CurrentTime;
     
            [HarmonyPrefix]
            public static void Prefix(ref CraftDefinition craft_definition)
            {
                //backup current time
                CurrentTime = craft_definition.craft_time;
                //get current time as float
                var currentCraftTime = craft_definition.craft_time.EvaluateFloat();
                //calculate new time
                var newCraftTime = currentCraftTime;
                if (FasterCraft)
                {
                    if (TimeAdjustment < 0)
                    {
                        newCraftTime = currentCraftTime * TimeAdjustment;
                    }
                    else
                    {
                        newCraftTime = currentCraftTime / TimeAdjustment;
                        
                    }
                }
                //set new time
                craft_definition.craft_time = SmartExpression.ParseExpression(newCraftTime.ToString(CultureInfo.InvariantCulture));
            }

            [HarmonyPostfix]
            public static void Postfix(ref CraftDefinition craft_definition, ref List<CraftDefinition> ____possible_crafts)
            {
                
                //need to clear, otherwise when the player closes and re-opens, nothing gets drawn
                //this also has to be here to stop things having their price adjusted again on the 2nd time around after a user closes and opens the ui
                FoundCrafts.Clear();
                //stuffs been drawn, we can fix
                //find the newly added item in possible crafts
                CraftDefinition ncd = null;
                
                foreach (var cd in ____possible_crafts)
                {
                    if (string.Equals(cd.id, craft_definition.id))
                    {
                        var found1 = FoundCrafts.TryGetValue(cd.id, out _);
                        if (!found1)
                        {
                            ncd = cd;
                            //back up the item so we can correct the time
                            //remove the item
                            FoundCrafts.Add(cd.id, "");
                            ____possible_crafts.Remove(cd);
                            break;
                        }
                    }
                }

                //set time back to original and add to list
                if (ncd == null) return; //<-------- this stops it crashing when the user closes the window....
                ncd.craft_time = CurrentTime; //restore time so other mods function correctly
                ____possible_crafts.Add(ncd); //re-add it back to the games list

                var found = Crafts.TryGetValue(ncd.id, out _);
                if (!found)
                {
                    Crafts.Add(ncd.id, ncd.craft_time.EvaluateFloat());
                }

    
            }
        }


        [HarmonyPatch(typeof(CraftDefinition))]
        [HarmonyPatch(nameof(CraftDefinition.CanCraftMultiple))]
        public static class ModPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                //this makes any item a multi-craft
                __result = true;
            }
        }

        [HarmonyPatch(typeof(CraftItemGUI), "OnCraftPressed", MethodType.Normal)]
        public static class OnCraftPressedPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref CraftItemGUI __instance)
            {
                try
                {
                    //this is where we set the craft time back to its original time so everything functions properly
                    var found = Crafts.TryGetValue(__instance.current_craft.id, out var value);
                    if (found)
                    {
                        __instance.current_craft.craft_time =
                            SmartExpression.ParseExpression(value.ToString(CultureInfo.InvariantCulture));
                    }

                    Debug.LogError($"Cook started: {__instance.current_craft.craft_time.EvaluateFloat()}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"No idea why im erroring: {ex.Message}");
                }
            }
        }



        [HarmonyPatch(typeof(CraftItemGUI))]
        [HarmonyPatch(nameof(CraftItemGUI.OnOver))]
        public static class OnOverPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref CraftItemGUI __instance, ref int ____amount, ref CraftDefinition ____overed_item)
            {
                try
                {
                    //this gets the crafting UI?
                    var craft = GUIElements.me.craft;
                    //I assume this is the object im crafting with
                    var crafteryWgo = craft.GetCrafteryWGO();
                    //the item i want to craft
                    var originalCraft = __instance.current_craft;
         
                   // originalCraft.craft_time = SmartExpression.ParseExpression((originalCraft.craft_time.EvaluateFloat() / timeAdjustment).ToString(CultureInfo.InvariantCulture));

                    //get the qty on the UI
                    var qty = crafteryWgo.GetCraftAmountCounter(originalCraft, ____amount);

                    //calculate total craft time. We pull from this list because it holds the original, unmodified craft time of a single unit
                    Crafts.TryGetValue(__instance.current_craft.id, out var value);
                    //value is the time, amount is the qty we've chosen to craft
                    var totalCraftTime = value * ____amount;

                    //if the faster craft mod is installed, adjust the values based on its config
                    if (FasterCraft)
                    {
                        Debug.LogError($"Time Adjustment: {TimeAdjustment}x");
                        if (TimeAdjustment < 0)
                        {
                            totalCraftTime *= TimeAdjustment;
                        }
                        else
                        {
                            totalCraftTime /= TimeAdjustment;
                        }
                    }
                    Debug.LogError($"Amount: {____amount}");

                    //set the new craft time for display
                    __instance.current_craft.craft_time =
                        SmartExpression.ParseExpression(totalCraftTime.ToString(CultureInfo.InvariantCulture));
                    //redraw so it updates properly
                    __instance.Redraw();
                    //print out stuff
                    Debug.LogError(
                        $"OnOver Postfix - Original Craft:{originalCraft.GetNameNonLocalized()}, Original Time: {value}, Total Craft Time: {totalCraftTime}, Qty: {qty}");
                }
                catch (Exception ex)
                {

                    Debug.LogError(ex.Message);
                }
            }
        }
    }
}

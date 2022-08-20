using HarmonyLib;
using Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace TheSeedEqualizer;

public class MainPatcher
{
    private static Config.Options _cfg;

    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.TheSeedEqualizer");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _cfg = Config.GetOptions();
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
            Tools.Log("TheSeedEqualizer", $"{message}", error);
        }
    }

    private static void ModifyOutput(CraftDefinition craft)
    {
        foreach (var output in craft.output.Where(a => a.id.Contains("seed")))
        {
            output.value = craft.needs[0].value;
            output.min_value = SmartExpression.ParseExpression(craft.needs[0].value.ToString());
            if (output.id.EndsWith(":3"))
            {
               // output.max_value = SmartExpression.ParseExpression(_cfg.BoostPotentialSeedOutput ? "37" : "32");
                var normalBoost = output.max_value.EvaluateFloat() + 4;
                var extraBoost = output.max_value.EvaluateFloat() + 8;
                var boost = _cfg.BoostPotentialSeedOutput ? extraBoost : normalBoost;
                output.max_value = SmartExpression.ParseExpression(boost.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                var normalBoost = output.max_value.EvaluateFloat() + 2;
                var extraBoost = output.max_value.EvaluateFloat() + 4;
                var boost = _cfg.BoostPotentialSeedOutput ? extraBoost : normalBoost;
                output.max_value = SmartExpression.ParseExpression(boost.ToString(CultureInfo.InvariantCulture));
            }
        }
    }

    [HarmonyPatch(typeof(ResModificator), nameof(ResModificator.ProcessItemsListBeforeDrop))]
    public static class ResModificatorProcessItemsListBeforeDropPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref WorldGameObject wgo, ref List<Item> __result)
        {
            var message = string.Empty;
            if (wgo != null && (Tools.ZombieGardenCraft(wgo) || Tools.ZombieVineyardCraft(wgo)) &&
                wgo.components.craft.current_craft != null) 
            {
                message += $"[ResModWgo]: WGO: {wgo.obj_id}\n";
                message += $"[ResModWgo]: CRAFT: {wgo.components.craft.current_craft.id}\n";
                message = __result.Aggregate(message, (current, item) => current + $"[ResModResult]: Item: {item.id}, Value: {item.value}\n");
                Log($"{message}");
            }
        }
    }

    private static bool _alreadyRun;

    [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
    public static class CraftComponentFillCraftsListPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (_alreadyRun) return;
            _alreadyRun = true;

            foreach (var craft in GameBalance.me.craft_data.Where(a => a.needs.Count == 1 && a.needs.Exists(b => b.id.Contains("seed"))))
            {
                if (Tools.PlayerGardenCraft(craft.id) && _cfg.ModifyPlayerGardens)
                {
                    ModifyOutput(craft);
                }

                if (Tools.ZombieGardenCraft(craft.id) && _cfg.ModifyZombieGardens)
                {
                    ModifyOutput(craft);
                }

                if (Tools.ZombieVineyardCraft(craft.id) && _cfg.ModifyZombieVineyards)
                {
                    ModifyOutput(craft);
                }

                if (Tools.RefugeeGardenCraft(craft.id) && _cfg.ModifyRefugeeGardens)
                {
                    ModifyOutput(craft);
                }

                if (Tools.ZombieVineyardCraft(craft.id) && _cfg.AddWasteToZombieVineyards)
                {
                    var item = new Item("crop_waste", 3)
                    {
                        min_value = SmartExpression.ParseExpression("3"),
                        max_value = SmartExpression.ParseExpression("5"),
                        self_chance = craft.needs[0].self_chance
                    };
                    craft.output.Add(item);
                }

                if (Tools.ZombieGardenCraft(craft.id) && _cfg.AddWasteToZombieGardens)
                {
                    var item = new Item("crop_waste", 3)
                    {
                        min_value = SmartExpression.ParseExpression("3"),
                        max_value = SmartExpression.ParseExpression("5"),
                        self_chance = craft.needs[0].self_chance
                    };
                    craft.output.Add(item);
                }
            }
        }
    }
}
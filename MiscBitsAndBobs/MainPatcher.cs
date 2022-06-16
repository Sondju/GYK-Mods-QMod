using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MiscBitsAndBobs;

public class MainPatcher
{
    private static Config.Options _cfg;

    private static readonly string[] TavernItems =
    {
        "npc_tavern_barman", "tavern_cellar_rack", "tavern_cellar_rack_1", "tavern_cellar_rack_2",
        "tavern_cellar_rack_3", "tavern_cellar_rack_4", "tavern_cellar_rack_5"
    };

    private static readonly ItemDefinition.ItemType[] ToolItems =
    {
        ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
        ItemDefinition.ItemType.Pickaxe, ItemDefinition.ItemType.FishingRod, ItemDefinition.ItemType.BodyArmor,
        ItemDefinition.ItemType.HeadArmor, ItemDefinition.ItemType.Sword, ItemDefinition.ItemType.Preach
    };

    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.MiscBitsAndBobs");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _cfg = Config.GetOptions();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MiscBitsAndBobs]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }

    [HarmonyPatch(typeof(InventoryGUI), nameof(InventoryGUI.OnItemOver))]
    public static class InventoryGuiOnItemOverPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref InventoryGUI __instance)
        {
            if (!_cfg.AllowHandToolDestroy) return;
            if (__instance == null) return;
            var itemDef = __instance.selected_item?.definition;
            if (itemDef == null) return;
            if (ToolItems.Contains(itemDef.type)) itemDef.player_cant_throw_out = false;
        }
    }

    //patch tools to be stack-able
    [HarmonyPatch(typeof(GameBalance), "LoadGameBalance")]
    public static class GameBalanceLoadGameBalancePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            if (!_cfg.EnableToolAndPrayerStacking) return;

            foreach (var itemDefinition in GameBalance.me.items_data
                         .Where(itemDefinition => itemDefinition != null)
                         .Where(x => ToolItems.Contains(x.type) || x.id.Contains("book") ||
                                     x.id.Contains("chapter") || x.id.Contains("grave") || x.id.Contains("pen")))
            {
                itemDefinition.stack_count += 1000;
                itemDefinition.base_count += 1000;
            }
        }
    }

    //makes the racks and the barman inventory larger
    [HarmonyPatch(typeof(WorldGameObject), "InitNewObject")]
    public static class WorldGameObjectInitNewObjectPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref WorldGameObject __instance)
        {
            if (TavernItems.Contains(__instance.obj_id))
                __instance.data.SetInventorySize(__instance.obj_def.inventory_size + _cfg.TavernInvIncrease);
        }
    }

    [HarmonyPatch(typeof(GameGUI), nameof(GameGUI.Open))]
    public static class GameGuiOpenPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (_cfg.QuietMusicInGui) SmartAudioEngine.me.SetDullMusicMode();
        }
    }

    [HarmonyPatch(typeof(MainMenuGUI), nameof(MainMenuGUI.Open))]
    public static class MainMenuGuiOpenPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref InGameMenuGUI __instance)
        {
            if (!_cfg.HideCreditsButtonOnMainMenu) return;
            if (__instance == null) return;

            foreach (var comp in __instance.GetComponentsInChildren<UIButton>()
                         .Where(x => x.name.Contains("credits")))
            {
                comp.SetState(UIButtonColor.State.Disabled, true);
                comp.SetActive(false);
            }
        }
    }

    [HarmonyPatch(typeof(HUD), nameof(HUD.Update))]
    public static class HudUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref bool ____inited)
        {
            if (!____inited || !MainGame.game_started || !_cfg.CondenseXpBar) return;

            var r = MainGame.me.player.GetParam("r");
            var g = MainGame.me.player.GetParam("g");
            var b = MainGame.me.player.GetParam("b");

            string red;
            if (r >= 1000)
            {
                r /= 1000f;
                var nSplit = r.ToString(CultureInfo.InvariantCulture).Split('.');
                red = nSplit[1].StartsWith("0") ? $"(r){r:0}K" : $"(r){r:0.0}K";
            }
            else
            {
                red = $"(r){r}";
            }

            string green;
            if (g >= 1000)
            {
                g /= 1000f;
                var nSplit = g.ToString(CultureInfo.InvariantCulture).Split('.');
                green = nSplit[1].StartsWith("0") ? $"(g){g:0}K" : $"(g){g:0.0}K";
            }
            else
            {
                green = $"(g){g}";
            }

            string blue;
            if (b >= 1000)
            {
                b /= 1000f;
                var nSplit = b.ToString(CultureInfo.InvariantCulture).Split('.');
                blue = nSplit[1].StartsWith("0") ? $"(b){b:0}K" : $"(b){b:0.0}K";
            }
            else
            {
                blue = $"(b){b}";
            }

            foreach (var comp in GUIElements.me.hud.tech_points_bar.GetComponentsInChildren<UILabel>())
                comp.text = $"{red} {green} {blue}";
        }
    }

    [HarmonyPatch(typeof(GameGUI), nameof(GameGUI.Hide))]
    public static class GameGuiHidePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (_cfg.QuietMusicInGui) SmartAudioEngine.me.SetDullMusicMode(false);
        }
    }

    [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
    public static class InventoryPanelGuiDoOpeningPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref InventoryPanelGUI __instance, ref MultiInventory multi_inventory)
        {
            if (_cfg.DontShowEmptyRowsInInventory) __instance.dont_show_empty_rows = true;

            if (!_cfg.ShowOnlyPersonalInventory) return;
            var multiInventory = new MultiInventory();
            var num = 0;
            foreach (var inventory in multi_inventory.all)
            {
                multiInventory.AddInventory(inventory);
                num++;
                if (num == 1) break;
            }

            multi_inventory = multiInventory;
        }
    }

    //makes halloween an annual event instead of the original 2018...
    [HarmonyPatch(typeof(GameSave), nameof(GameSave.GlobalEventsCheck))]
    internal class GameSaveGlobalEventsCheckPatch
    {
        [HarmonyPrefix]
        private static bool Prefix()
        {
            return false;
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            if (_cfg.HalloweenNow)
            {
                foreach (var globalEventBase in new List<GlobalEventBase>
                         {
                             new("halloween", DateTime.Now, new TimeSpan(14, 0, 0, 0))
                             {
                                 on_start_script = new Scene1100_To_SceneHelloween(),
                                 on_finish_script = new SceneHelloween_To_Scene1100()
                             }
                         })
                    globalEventBase.Process();
            }
            else
            {
                var year = DateTime.Now.Year;
                foreach (var globalEventBase in new List<GlobalEventBase>
                         {
                             new("halloween", new DateTime(year, 10, 29), new TimeSpan(14, 0, 0, 0))
                             {
                                 on_start_script = new Scene1100_To_SceneHelloween(),
                                 on_finish_script = new SceneHelloween_To_Scene1100()
                             }
                         })
                    globalEventBase.Process();
            }
        }
    }
}
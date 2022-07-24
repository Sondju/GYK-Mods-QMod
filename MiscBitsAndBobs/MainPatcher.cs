using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Helper;

namespace MiscBitsAndBobs;

public class MainPatcher
{
    private static Config.Options _cfg;
    private static WorldGameObject _wgo;
    private const string WheresMaStorage = "WheresMaStorage";

    private static readonly string[] TavernItems =
    {
        "npc_tavern_barman", "tavern_cellar_rack", "tavern_cellar_rack_1", "tavern_cellar_rack_2",
        "tavern_cellar_rack_3", "tavern_cellar_rack_4", "tavern_cellar_rack_5"
    };

    private static readonly string[] MakeStackable =
    {
        "book","chapter"
    };

    private static readonly ItemDefinition.ItemType[] GraveItems =
    {
        ItemDefinition.ItemType.GraveStone, ItemDefinition.ItemType.GraveFence, ItemDefinition.ItemType.GraveCover,
        ItemDefinition.ItemType.GraveStoneReq, ItemDefinition.ItemType.GraveFenceReq, ItemDefinition.ItemType.GraveCoverReq,
    };

    private static readonly ItemDefinition.ItemType[] ToolItems =
    {
        ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
        ItemDefinition.ItemType.Pickaxe, ItemDefinition.ItemType.FishingRod, ItemDefinition.ItemType.BodyArmor,
        ItemDefinition.ItemType.HeadArmor, ItemDefinition.ItemType.Sword, ItemDefinition.ItemType.Preach,
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
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("MiscBitsAndBobs", $"{message}", error);
    }

    
    [HarmonyPatch(typeof(PrayCraftGUI), nameof(PrayCraftGUI.OnPrayButtonPressed))]
    public static class PrayCraftGuiOnPrayButtonPressedPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref PrayCraftGUI __instance, ref Item ____selected_item)
        {
            if (!_cfg.RemovePrayerOnUse) return;
            if (__instance == null) return;
            var playerInv = MainGame.me.player.GetMultiInventory(exceptions: null, force_world_zone: "",
                player_mi: MultiInventory.PlayerMultiInventory.IncludePlayer, include_toolbelt: true,
                include_bags: true, sortWGOS: true);
            foreach (var inv in playerInv.all)
            {
                foreach (var item in inv.data.inventory)
                {
                    if (item != ____selected_item) continue;
                    inv.data.RemoveItemNoCheck(item, 1);
                    Log($"Removed 1x {____selected_item.id} from {inv._obj_id}.");
                    return;
                }
            }
        }
    }

    [HarmonyPatch(typeof(LeaveTrailComponent), "LeaveTrail")]
    public static class LeaveTrailComponentLeaveTrailPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TrailDefinition ____trail_definition, Ground.GroudType ____trail_type, float ____dirty_amount, List<TrailObject> ____all_trails)
        {
            
            if (!_cfg.LessenFootprintImpact) return;
            var byType = ____trail_definition.GetByType(____trail_type);
            if (____all_trails.Count <= 0) return;
            var trailObject = ____all_trails[____all_trails.Count - 1];
            trailObject.SetColor(byType.color, ____dirty_amount * 0.5f);
        }
    }

    [HarmonyPatch(typeof(CameraTools), nameof(CameraTools.TweenLetterbox))]
    public static class GCameraToolsTweenLetterboxPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref bool show)
        {
            if (_cfg.DisableCinematicLetterboxing)
            {
                show = false;
            }
        }
    }

    [HarmonyPatch(typeof(Intro), nameof(Intro.ShowIntro))]
    public static class GameSaveCreateNewSavePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (_cfg.SkipIntroVideoOnNewGame)
            {
                Intro.need_show_first_intro = false;
            }
        }
    }

    [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
    public static class WorldGameObjectInteractPatch
    {
        [HarmonyPrefix]
        public static void Prefix(WorldGameObject __instance, WorldGameObject other_obj)
        {
            if (!MainGame.game_started || __instance == null) return;
            if (other_obj == MainGame.me.player)
            {
                _wgo = __instance;
            }
        }
    }
    
    [HarmonyPatch(typeof(CraftComponent), "End")]
    public static class GraveGuiOnCraftPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref CraftComponent __instance)
        {
            if (!_cfg.KitsuneKitoMode) return;
            if (!MainGame.game_started || __instance == null) return;
            if (__instance.last_craft_id.Equals("set_grave_bot_wd_1"))
            {
                TechPointsDrop.Drop(_wgo.pos3, 0, 0, 1);
            }
        }
    }

    [HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.CollectDrop))]
    public static class DropResGameObjectCollectDrop
    {
        //set stack size back up before collecting
        [HarmonyPrefix]
        public static void Prefix(ref DropResGameObject __instance)
        {
            if (Tools.IsModLoaded(WheresMaStorage)) return;
                if (!GraveItems.Contains(__instance.res.definition.type)) return;
            __instance.res.definition.stack_count = 999;
        }
    }

    [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.GetRemoveCraftForItem))]
    public static class GameBalanceGetRemoveCraftForItemPatch
    {
        //needed for grave removals to work
        [HarmonyPostfix]
        public static void Postfix(ref CraftDefinition __result)
        {
            if (Tools.IsModLoaded(WheresMaStorage)) return;
            foreach (var item in __result.output.Where(a => GraveItems.Contains(a.definition.type)))
            {
                item.definition.stack_count = 1;
            }
        }
    }

    [HarmonyPatch(typeof(CraftDefinition), "takes_item_durability", MethodType.Getter)]
    public static class CraftDefinitionTakesItemDurabilityPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref CraftDefinition __instance, ref bool __result)
        {
           // if (Tools.IsModLoaded(WheresMaStorage)) return;
            if (!_cfg.EnableChiselInkStacking) return;
            if (__instance == null) return;
            if (__instance.needs.Exists(item => item.id.Equals("pen:ink_pen")) && __instance.dur_needs_item > 0)
            {
                __result = false;
            }
            if (__instance.needs.Exists(item => item.id.Contains("chisel")) && __instance.dur_needs_item > 0)
            {
                __result = false;
            }

        }
    }


    [HarmonyAfter("p1xel8ted.GraveyardKeeper.WheresMaStorage")]
    [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
    public static class GameBalanceLoadGameBalancePatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {

            if (_cfg.AllowHandToolDestroy)
            {
                foreach (var itemDef in GameBalance.me.items_data.Where(a => ToolItems.Contains(a.type)))
                {
                    itemDef.player_cant_throw_out = false;
                }
            }

         
            if (_cfg.EnableToolAndPrayerStacking || _cfg.EnableChiselInkStacking)
            {
                foreach (var item in GameBalance.me.items_data.Where(item => item.stack_count == 1))
                {
                    if (_cfg.EnableToolAndPrayerStacking)
                    {
                        if (ToolItems.Contains(item.type) || GraveItems.Contains(item.type) ||
                            MakeStackable.Any(item.id.Contains))
                        {
                            item.stack_count = 999;
                        }
                    }

                    if (_cfg.EnableChiselInkStacking)
                    {
                        if (item.id.Contains("ink") || item.id.Contains("pen") || item.id.Contains("chisel"))
                        {
                            item.stack_count = 999;
                        }
                    }
                }
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
            if (Tools.IsModLoaded(WheresMaStorage)) return;
            if (TavernItems.Contains(__instance.obj_id))
            {
                __instance.data.SetInventorySize(__instance.obj_def.inventory_size + _cfg.TavernInvIncrease);
            }
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
        public static void Prefix(ref MainMenuGUI __instance)
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
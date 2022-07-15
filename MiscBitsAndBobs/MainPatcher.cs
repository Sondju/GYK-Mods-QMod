using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using DarkTonic.MasterAudio;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MiscBitsAndBobs;

public class MainPatcher
{
    private static Config.Options _cfg;
    private static WorldGameObject _wgo;
    private static readonly List<string> LoadedMods = new();
    private const string WheresMaStorage = "WheresMaStorage";

    private static readonly string[] TavernItems =
    {
        "npc_tavern_barman", "tavern_cellar_rack", "tavern_cellar_rack_1", "tavern_cellar_rack_2",
        "tavern_cellar_rack_3", "tavern_cellar_rack_4", "tavern_cellar_rack_5"
    };

    private static readonly string[] MakeStackable =
    {
        "book","chapter","pen"
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
            Debug.LogError($"[MiscBitsAndBobs]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }

    [HarmonyPatch(typeof(MasterAudio), nameof(MasterAudio.PlaySound))]
    public static class MasterAudioPlaySoundPatch
    {
        [HarmonyPrefix]
        public static void Prefix(string sType, ref float volumePercentage)
        {
            if (!_cfg.LessenFootstepSound) return;
            if (sType.Contains("foot"))
            {
                Debug.LogError($"[MBB]: Footstep Volume: {volumePercentage}");
                //volumePercentage = 0.25f;
            }
        }
    }


    [HarmonyPatch(typeof(LeaveTrailComponent), "LeaveTrail")]
    public static class LeaveTrailComponentLeaveTrailPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TrailDefinition ____trail_definition, Ground.GroudType ____trail_type, float ____dirty_amount, List<TrailObject> ____all_trails)
        {
            //Debug.LogError($"[MBB]: Dirty Amount: {____dirty_amount}");
            if (!_cfg.LessenFootprintImpact) return;
            var byType = ____trail_definition.GetByType(____trail_type);
            if (____all_trails.Count <= 0) return;
            var trailObject = ____all_trails[____all_trails.Count - 1];
            trailObject.SetColor(byType.color, ____dirty_amount / 2.0f);
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
                //Debug.LogError($"Object: {_wgo.obj_id}, Craft: {__instance.last_craft_id}");
                TechPointsDrop.Drop(_wgo.pos3, 0, 0, 1);
            }
        }
    }

    //[HarmonyPatch(typeof(InventoryGUI), nameof(InventoryGUI.OnItemOver))]
    //public static class InventoryGuiOnItemOverPatch
    //{
    //    [HarmonyPrefix]
    //    public static void Prefix(ref InventoryGUI __instance)
    //    {
    //        //if (__instance == null) return;

    //        //Debug.LogError(__instance.selected_item.definition.linked_craft != null
    //        //    ? $"[MBB]: Item: {__instance.selected_item.id}, CantDestroy: {__instance.selected_item.definition.player_cant_throw_out}, Unique: {__instance.selected_item.is_unique}, OneTime: {__instance.selected_item.definition.linked_craft.one_time_craft}"
    //        //    : $"[MBB]: Item: {__instance.selected_item.id}, CantDestroy: {__instance.selected_item.definition.player_cant_throw_out}, Unique: {__instance.selected_item.is_unique}");

    //        if (!_cfg.AllowHandToolDestroy) return;
    //        if (__instance == null) return;
    //        var itemDef = __instance.selected_item?.definition;
    //        if (itemDef == null) return;
    //        if (ToolItems.Contains(itemDef.type)) itemDef.player_cant_throw_out = false;
    //    }
    //}

    [HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.CollectDrop))]
    public static class DropResGameObjectCollectDrop
    {
        //set stack size back up before collecting
        [HarmonyPrefix]
        public static void Prefix(ref DropResGameObject __instance)
        {
            if (LoadedMods.Contains(WheresMaStorage)) return;
                if (!GraveItems.Contains(__instance.res.definition.type)) return;
            __instance.res.definition.stack_count = 9999;
            // __instance.res.definition.base_count = 1000;
        }
    }

    [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.GetRemoveCraftForItem))]
    public static class GameBalanceGetRemoveCraftForItemPatch
    {
        //needed for grave removals to work
        [HarmonyPostfix]
        public static void Postfix(ref CraftDefinition __result)
        {
            if (LoadedMods.Contains(WheresMaStorage)) return;
            foreach (var item in __result.output.Where(a => GraveItems.Contains(a.definition.type)))
            {
                item.definition.stack_count = 1;
                //item.definition.base_count = 1;
            }
        }
    }

    [HarmonyPatch(typeof(CraftDefinition), "takes_item_durability", MethodType.Getter)]
    public static class CraftDefinitionTakesItemDurabilityPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref CraftDefinition __instance, ref bool __result)
        {
            if (LoadedMods.Contains(WheresMaStorage)) return;
            if (!_cfg.EnableToolAndPrayerStacking) return;
            if (__instance == null) return;
            if (__instance.needs.Exists(item => item.id.Equals("pen:ink_pen")) && __instance.dur_needs_item > 0)
            {
                __result = false;
            }
            //Debug.LogError($"[MiscBitsAndBobs] Def: {__instance.id}, Dur: {__instance.dur_needs_item}");
        }
    }

    //patch tools to be stack-able
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

            if (LoadedMods.Contains(WheresMaStorage)) return;

            if (_cfg.EnableToolAndPrayerStacking)
            {
                foreach (var item in GameBalance.me.items_data.Where(item =>
                             ToolItems.Contains(item.type) || GraveItems.Contains(item.type) ||
                             MakeStackable.Any(item.id.Contains)))
                {
                    if (item.stack_count >= 1000) continue;
                    item.stack_count += 1000;
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
            if (LoadedMods.Contains(WheresMaStorage)) return;
            if (TavernItems.Contains(__instance.obj_id))
            {
               // Debug.LogError($"[MBB]: TavernStorage InitNewObject Hit");
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

        [HarmonyPostfix]
        public static void Postfix()
        {
            try
            {
                var mods = AppDomain.CurrentDomain.GetAssemblies()
             .Where(a => a.Location.ToLowerInvariant().Contains("qmods"));
                LoadedMods.Clear();
                foreach (var mod in mods)
                {
                    var modInfo = FileVersionInfo.GetVersionInfo(mod.Location);
                    if (!string.IsNullOrEmpty(modInfo.Comments))
                    {
                        LoadedMods.Add(modInfo.Comments);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Loaded Mod: {ex.Message}");
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
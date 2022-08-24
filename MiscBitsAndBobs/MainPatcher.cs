using HarmonyLib;
using Helper;
using MiscBitsAndBobs.lang;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace MiscBitsAndBobs;

public class MainPatcher
{
    private static Config.Options _cfg;
    private static WorldGameObject _wgo;
    private static bool _sprintTools, _sprintHarmony, _sprint;

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

    [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
    public static class GameBalanceLoadGameBalancePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (_cfg.AddCoalToTavernOven)
            {
                var coal = GameBalance.me.GetData<CraftDefinition>("mf_furnace_0_fuel_coal");
                coal?.craft_in.Add("tavern_oven");
            }

            if (_cfg.AddZombiesToPyreAndCrematorium)
            {
                var mfPyre = GameBalance.me.GetData<ObjectDefinition>("mf_pyre");
                mfPyre.can_insert_items.Add("working_zombie_on_ground_1");
                mfPyre.can_insert_items.Add("working_zombie_pseudoitem_1");
                mfPyre.can_insert_zombie = true;

                var mfCrematorium = GameBalance.me.GetData<ObjectDefinition>("mf_crematorium");
                mfCrematorium.can_insert_items.Add("working_zombie_on_ground_1");
                mfCrematorium.can_insert_items.Add("working_zombie_pseudoitem_1");
                mfCrematorium.can_insert_items.Add("body");
                mfCrematorium.can_insert_items.Add("body_guard");
                mfCrematorium.can_insert_zombie = true;

                var mfCrematoriumCorp = GameBalance.me.GetData<ObjectDefinition>("mf_crematorium_corp");
                mfCrematoriumCorp.can_insert_items.Add("working_zombie_on_ground_1");
                mfCrematoriumCorp.can_insert_items.Add("working_zombie_pseudoitem_1");
                mfCrematoriumCorp.can_insert_items.Add("body");
                mfCrematoriumCorp.can_insert_items.Add("body_guard");
                mfCrematoriumCorp.can_insert_zombie = true;
            }
        }
    }

    private static bool WorkerHasBackpack(WorldGameObject workerWgo)
    {
        return workerWgo.data.inventory.Any(backpack => backpack.id == "porter_backpack");
    }

    [HarmonyPatch(typeof(MovementComponent), "UpdateMovement", typeof(Vector2), typeof(float))]
    public static class MovementComponentUpdateMovementPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref MovementComponent __instance)
        {
            if (__instance.wgo.is_dead || __instance.player_controlled_by_script) return;
            //Log($"[MoveSpeed]: Instance: {__instance.wgo.obj_id}, Speed: {__instance.wgo.data.GetParam("speed")}");

            if (__instance.wgo.is_player && !_sprint)
            {
                var speed = __instance.wgo.data.GetParam("speed");
                if (speed > 0)
                {
                    speed = LazyConsts.PLAYER_SPEED + __instance.wgo.data.GetParam("speed_buff");
                }

                if (_cfg.ModifyPlayerMovementSpeed)
                {
                    __instance.SetSpeed(speed * _cfg.PlayerMovementSpeed);
                }
                else
                {
                    __instance.SetSpeed(speed);
                }
            }

            if (__instance.wgo.IsWorker() && WorkerHasBackpack(__instance.wgo))
            {
                //1 and 0 = the same speed in game for zombies
                if (_cfg.ModifyPorterMovementSpeed)
                {
                    __instance.SetSpeed(_cfg.PorterMovementSpeed);
                }
                else
                {
                    __instance.SetSpeed(0);
                }
            }
        }
    }

    private static string GetLocalizedString(string content)
    {
        Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
        return content;
    }

    private static bool _sprintMsgShown = false;

    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
    public static class TimeOfDayUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!MainGame.game_started) return;

            if (MainGame.game_started && !_sprintMsgShown && _sprint && _cfg.ModifyPlayerMovementSpeed)
            {
                Tools.ShowAlertDialog(GetLocalizedString(strings.Title), GetLocalizedString(strings.Content), separateWithStars:true);
                _sprintMsgShown = true;
            }

            if (Input.GetKeyUp(KeyCode.F5))
            {
                _cfg = Config.GetOptions();

                if (!CrossModFields.ConfigReloadShown)
                {
                    Tools.ShowMessage(GetLocalizedString(strings.ConfigMessage), Vector3.zero);
                    CrossModFields.ConfigReloadShown = true;
                }
            }
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

    [HarmonyPatch(typeof(GameGUI), nameof(GameGUI.Open))]
    public static class GameGuiOpenPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (_cfg.QuietMusicInGui) SmartAudioEngine.me.SetDullMusicMode();
        }
    }

    [HarmonyAfter("p1xel8ted.GraveyardKeeper.QModHelper")]
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
            _sprintTools = Tools.ModLoaded("", "SprintReloaded.dll", "Sprint Reloaded");
            _sprintHarmony = Harmony.HasAnyPatches("mugen.GraveyardKeeper.SprintReloaded");
            _sprint = _sprintTools || _sprintHarmony;

            Log($"[MBB]: Sprint Detected via Tools: {_sprintTools}");

            Log($"[MBB]: Sprint Detected via Harmony: {_sprintHarmony}");
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
            var year = DateTime.Now.Year;
            foreach (var globalEventBase in new List<GlobalEventBase>
                         {
                             new("halloween",_cfg.HalloweenNow ? DateTime.Now : new DateTime(year, 10, 29), new TimeSpan(14, 0, 0, 0))
                             {
                                 on_start_script = new Scene1100_To_SceneHelloween(),
                                 on_finish_script = new SceneHelloween_To_Scene1100()
                             }
                         })
                globalEventBase.Process();
        }
    }
}
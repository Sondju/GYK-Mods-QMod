using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rewired;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IBuildWhereIWant
{
    public class MainPatcher
    {
        private static Config.Options _cfg;
        private static WorldGameObject _buildDesk;
        private static CraftsInventory _craftsInventory;
        private static List<ObjectCraftDefinition> _sortedList;
        private static List<string> _nameList;
        private static bool _isTalkingWithNpc;

        public static void Patch()
        {
            try
            {
                _cfg = Config.GetOptions();

                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.IBuildWhereIWant");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IBuildWhereIWant]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
            }
        }

        private static void OpenCraftAnywhere()
        {
            _craftsInventory ??= new CraftsInventory();
            _sortedList ??= new List<ObjectCraftDefinition>();
            _nameList ??= new List<string>();
            _buildDesk ??= Object.FindObjectsOfType<WorldGameObject>(true)
                .FirstOrDefault(x => x.obj_id.Contains("build"));


            Debug.Log(_buildDesk != null
                ? $"[IBuildWhereIWant] Build Desk Located: {_buildDesk.obj_id}"
                : "[IBuildWhereIWant] Build Desk Not Located");

            if (_craftsInventory != null)
            {
                Debug.Log(
                    $"[IBuildWhereIWant] ObjectCrafts List Count: {_craftsInventory.GetObjectCraftsList().Count}");
                Debug.Log(
                    $"[IBuildWhereIWant] Crafts List Count: {_craftsInventory.GetCraftsList().Count}");
            }

            if (_craftsInventory.GetObjectCraftsList().Count <= 0)
            {
                foreach (var objectCraftDefinition in GameBalance.me.craft_obj_data.Where(x =>
                                 x.build_type == ObjectCraftDefinition.BuildType.Put)
                             .Where(a => a.icon.Length > 0)
                             .Where(b => !b.id.Contains("refugee")))
                {
                    Debug.Log(
                        $"[IBuildWhereIWant] Object ID: {objectCraftDefinition.id}, Object name: {objectCraftDefinition.GetNameNonLocalized()}, Energy: {objectCraftDefinition.energy.EvaluateFloat()}");
                    var name = objectCraftDefinition.GetNameNonLocalized();
                    if (MainGame.me.save.IsCraftVisible(objectCraftDefinition) && !_nameList.Contains(name) &&
                        !_sortedList.Contains(objectCraftDefinition))
                    {
                        _nameList.Add(name);
                        _sortedList.Add(objectCraftDefinition);
                    }
                }

                _sortedList = _sortedList.OrderBy(a => a.tab_id).ToList();


                foreach (var item in _sortedList.OrderBy(a => a.tab_id))
                {
                    _craftsInventory.AddCraft(item.id);
                }
            }

            BuildModeLogics.last_build_desk = null;
            GUIElements.me.craft.OpenAsBuild(_buildDesk, _craftsInventory);
            MainGame.paused = false;
        }


        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.Update))]
        public static class TimeOfDayUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (!MainGame.game_started || MainGame.me.player.is_dead || MainGame.me.player.IsDisabled() ||
                    MainGame.paused || _isTalkingWithNpc) return;

                if (LazyInput.gamepad_active && ReInput.players.GetPlayer(0).GetButtonDown(6))
                {
                    OpenCraftAnywhere();
                }

                if (Input.GetKeyUp(KeyCode.Q))
                {
                    OpenCraftAnywhere();
                }

            }
        }

        [HarmonyPatch(typeof(SmartAudioEngine))]
        public static class SmartAudioEngineOnStartNpcInteractionPatch
        {
            [HarmonyPatch(nameof(SmartAudioEngine.OnEndNPCInteraction))]
            [HarmonyPostfix]
            public static void OnEndPostfix()
            {
                _isTalkingWithNpc = false;
            }

            [HarmonyPatch(nameof(SmartAudioEngine.OnStartNPCInteraction))]
            [HarmonyPostfix]
            public static void OnStartPostfix()
            {
                _isTalkingWithNpc = true;
            }
        }


        [HarmonyPatch(typeof(BuildGrid), nameof(BuildGrid.ShowBuildGrid))]
        public static class BuildGridShowBuildGridPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref bool show)
            {
                if (_cfg.DisableGrid)
                {
                    show = false;
                }
            }
        }


        [HarmonyPatch(typeof(FlowGridCell))]
        public static class FlowGridCellPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(FlowGridCell.IsInsideWorldZone))]
            public static void IsInsideWorldZonePostfix(ref bool __result)
            {
                __result = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(FlowGridCell.IsPlaceAvailable))]
            public static void IsPlaceAvailablePostfix(ref bool __result)
            {
                __result = true;
            }
        }


        [HarmonyPatch(typeof(BuildModeLogics))]
        public static class BuildModeLogicsPatch
        {

            [HarmonyPrefix]
            [HarmonyPatch("CancelCurrentMode")]
            public static void CancelCurrentModePrefix()
            {
                if (BuildModeLogics.last_build_desk == null)
                {
                    FloatingWorldGameObject.StopCurrentFloating();
                    MainGame.me.ExitBuildMode();
                    GUIElements.me.build_mode_gui.Hide();
                    BuildGrid.me.ClearPreviousTotemRadius(true);
                    OpenCraftAnywhere();
                }
            }


            [HarmonyPrefix]
            [HarmonyPatch(nameof(BuildModeLogics.OnBuildCraftSelected))]
            private static void OnBuildCraftSelectedPrefix(ref string ____cur_build_zone_id, ref WorldZone ____cur_build_zone,
                ref Bounds ____cur_build_zone_bounds)
            {
                if (BuildModeLogics.last_build_desk == null)
                {
                   // const string zoneId = "mf_wood_builddesk";
                    const string zone = "mf_wood";
                    ____cur_build_zone_id = zone;
                    ____cur_build_zone = WorldZone.GetZoneByID(zone, true);
                    ____cur_build_zone_bounds = ____cur_build_zone.GetBounds();
                }

            }

            [HarmonyPatch(nameof(BuildModeLogics.CanBuild))]
            [HarmonyPrefix]
                private static void CanBuildPrefix(ref MultiInventory ____multi_inventory)
                {
                    ____multi_inventory = MainGame.me.player.GetMultiInventoryForInteraction();
                }
            

            [HarmonyPatch("DoPlace")]
            [HarmonyPrefix]
                private static void DoPlacePrefix(ref MultiInventory ____multi_inventory)
                {
                    ____multi_inventory = MainGame.me.player.GetMultiInventoryForInteraction();
                }
            
        }
    }
}
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            _buildDesk ??= Object.FindObjectsOfType<WorldGameObject>(true).FirstOrDefault(x => x.obj_id.Contains("build"));


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
                    if (MainGame.me.save.IsCraftVisible(objectCraftDefinition) && !_nameList.Contains(name) && !_sortedList.Contains(objectCraftDefinition) )
                    {
                        _nameList.Add(name);
                        _sortedList.Add(objectCraftDefinition);
                    }
                }

                _sortedList = _sortedList.OrderBy(a => a.tab_id).ToList();


                foreach (var item in _sortedList.OrderBy(a =>a.tab_id))
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
                if (!MainGame.game_started || MainGame.me.player.is_dead || MainGame.me.player.IsDisabled()) return;

                if (Input.GetKeyUp(KeyCode.Q))
                {
                    OpenCraftAnywhere();
                }

            }
        }


        [HarmonyPatch(typeof(BuildModeLogics), "CancelCurrentMode")]
        public static class BuildModeLogicsCancelCurrentModePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
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

        [HarmonyPatch(typeof(FloatingWorldGameObject), nameof(FloatingWorldGameObject.RecalculateAvailability))]
        public static class FloatingWorldGameObjectRecalculateAvailabilityPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                FloatingWorldGameObject.can_be_built = true;
            }
        }

        [HarmonyPatch(typeof(FlowGridCell), nameof(FlowGridCell.IsInsideWorldZone))]
        public static class FlowGridCellIsInsideWorldZonePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(FlowGridCell), nameof(FlowGridCell.IsPlaceAvailable))]
        public static class FlowGridCellIsPlaceAvailablePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }

        [HarmonyPatch(typeof(BuildModeLogics), nameof(BuildModeLogics.CanBuild))]
        internal class BuildModeLogicsCanBuildPatch
        {
            [HarmonyPrefix]
            private static void Prefix(BuildModeLogics __instance, ref MultiInventory ____multi_inventory)
            {
                ____multi_inventory = MainGame.me.player.GetMultiInventoryForInteraction();
            }
        }

        [HarmonyPatch(typeof(BuildModeLogics), "DoPlace")]
        internal class BuildModeLogicsDoPlacePatch
        {
            [HarmonyPrefix]
            private static void Prefix(BuildModeLogics __instance, ref MultiInventory ____multi_inventory)
            {
                ____multi_inventory = MainGame.me.player.GetMultiInventoryForInteraction();
            }
        }
    }
}
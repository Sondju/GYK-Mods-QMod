using HarmonyLib;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IBuildWhereIWant
{
    public class MainPatcher
    {
        private static List<string> _alreadyAddedList;
        private static List<string> _alreadyAddedNameList;
        private static WorldGameObject _buildDesk;
        private static WorldGameObject _buildDeskClone;
        private static Config.Options _cfg;
        private static bool _craftAnywhere;
        private static CraftsInventory _craftsInventory;
        private static List<string> _hasRemoveCraftList;

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
            if (MainGame.me.player.GetParamInt("in_tutorial") == 1 && MainGame.me.player.GetParamInt("tut_shown_tut_1") == 0)
            {
                MainGame.me.player.Say("cant_do_it_now");
                return;
            }

            _craftsInventory ??= new CraftsInventory();
            _hasRemoveCraftList ??= new List<string>();
            _alreadyAddedList ??= new List<string>();
            _alreadyAddedNameList ??= new List<string>();
            _buildDesk = Object.FindObjectsOfType<WorldGameObject>(true)
                .FirstOrDefault(x => x.obj_id.Contains("build"));
            _buildDeskClone = Object.Instantiate(_buildDesk);
            _buildDeskClone.name = "buildanywhere_desk";
            _buildDeskClone.obj_id = "buildanywhere_desk";

            foreach (var objectCraftDefinition in GameBalance.me.craft_obj_data.Where(objectCraftDefinition => objectCraftDefinition.build_type == ObjectCraftDefinition.BuildType.Remove))
            {
                _hasRemoveCraftList.Add(objectCraftDefinition.out_obj);
            }

            if (_craftsInventory.GetObjectCraftsList().Count <= 0)
            {
                foreach (var objectCraftDefinition in GameBalance.me.craft_obj_data.Where(x =>
                                 x.build_type == ObjectCraftDefinition.BuildType.Put)
                             .Where(a => a.icon.Length > 0)
                             .Where(b => !b.id.Contains("refugee"))
                             .Where(c => _hasRemoveCraftList.Contains(c.out_obj))
                             .Where(d => MainGame.me.save.IsCraftVisible(d))
                             .Where(e => !_alreadyAddedList.Contains(e.id))
                             .Where(f => !_alreadyAddedNameList.Contains(f.GetNameNonLocalized())))

                {
                    _alreadyAddedNameList.Add(objectCraftDefinition.GetNameNonLocalized());
                    _alreadyAddedList.Add(objectCraftDefinition.id);
                    _craftsInventory.AddCraft(objectCraftDefinition.id);
                }
            }

            _craftAnywhere = true;
            BuildModeLogics.last_build_desk = _buildDeskClone;
            MainGame.me.build_mode_logics.SetCurrentBuildZone(_buildDeskClone?.obj_def.zone_id, "");
            GUIElements.me.craft.OpenAsBuild(_buildDeskClone, _craftsInventory);
            MainGame.paused = false;
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

        [HarmonyPatch(typeof(BuildModeLogics))]
        public static class BuildModeLogicsPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("CancelCurrentMode")]
            public static void CancelCurrentModePrefix()
            {
                if (_craftAnywhere)
                {
                    OpenCraftAnywhere();
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
            private static void DoPlacePrefix(ref BuildModeLogics __instance, ref MultiInventory ____multi_inventory)
            {
                ____multi_inventory = MainGame.me.player.GetMultiInventoryForInteraction();
                if (_craftAnywhere && MainGame.me.player.cur_zone.Length<=0)
                {
                    BuildGrid.ShowBuildGrid(false);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("FocusCameraOnBuildZone")]
            private static void FocusCameraOnBuildZonePrefix(ref string zone_id)
            {
                if (_craftAnywhere)
                {
                    zone_id = string.Empty;
                }
            }

            [HarmonyPatch(nameof(BuildModeLogics.GetObjectRemoveCraftDefinition))]
            [HarmonyPostfix]
            private static void GetObjectRemoveCraftDefinitionPostfix(string obj_id, ref ObjectCraftDefinition __result)
            {
                if (_craftAnywhere)
                {
                    foreach (var objectCraftDefinition in GameBalance.me.craft_obj_data.Where(objectCraftDefinition =>
                                 objectCraftDefinition.out_obj == obj_id && objectCraftDefinition.build_type ==
                                 ObjectCraftDefinition.BuildType.Remove))
                    {
                        __result = objectCraftDefinition;
                        return;
                    }
                    __result = null;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(BuildModeLogics.OnBuildCraftSelected))]
            private static void OnBuildCraftSelectedPrefix(ref string ____cur_build_zone_id, ref WorldZone ____cur_build_zone,
                ref Bounds ____cur_build_zone_bounds)
            {
                if (_craftAnywhere)
                {
                    BuildModeLogics.last_build_desk = _buildDeskClone;
                    const string zone = "mf_wood";
                    ____cur_build_zone_id = zone;
                    ____cur_build_zone = WorldZone.GetZoneByID(zone, true);
                    ____cur_build_zone_bounds = ____cur_build_zone.GetBounds();
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

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        public static class TimeOfDayUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (!MainGame.game_started || MainGame.me.player.is_dead || MainGame.me.player.IsDisabled() ||
                    MainGame.paused || !BaseGUI.all_guis_closed) return;

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

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref WorldGameObject __instance)
            {
                if (__instance.obj_def.interaction_type is not ObjectDefinition.InteractionType.None)
                {
                    _craftAnywhere = false;
                }
            }
        }
    }
}
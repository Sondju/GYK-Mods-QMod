using HarmonyLib;
using Helper;
using IBuildWhereIWant.lang;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IBuildWhereIWant
{
    public class MainPatcher
    {
        private static WorldGameObject _buildDesk;
        private static WorldGameObject _buildDeskClone;
        private static Config.Options _cfg;
        private static CraftsInventory _craftsInventory;

        private static Dictionary<string, string> _craftDictionary;
        private const string Zone = "mf_wood";

        private const string BuildDesk = "buildanywhere_desk";

        private static int _unlockedCraftListCount;


        public static void Patch()
        {
            try
            {
                _cfg = Config.GetOptions();
                //_gerrysJunkTrunk = Tools.IsModLoaded("GerrysJunkTrunk") || Harmony.HasAnyPatches("p1xel8ted.GraveyardKeeper.GerrysJunkTrunk");

                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.IBuildWhereIWant");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        private static void Log(string message, bool error = false)
        {
            Tools.Log("IBuildWhereIWant", $"{message}", error);
        }


        private static void OpenCraftAnywhere()
        {
            if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
            if (MainGame.me.player.GetParamInt("in_tutorial") == 1 &&
                MainGame.me.player.GetParamInt("tut_shown_tut_1") == 0)
            {
                MainGame.me.player.Say("cant_do_it_now");
                return;
            }

            _craftsInventory ??= new CraftsInventory();

            _craftDictionary ??= new Dictionary<string, string>();

            if (_buildDesk == null)
            {
                _buildDesk = Object.FindObjectsOfType<WorldGameObject>(true)
                    .FirstOrDefault(x => string.Equals(x.obj_id, "mf_wood_builddesk"));
            }

            Log(
                _buildDesk != null
                    ? $"Found Build Desk: {_buildDesk}, Zone: {_buildDesk.GetMyWorldZone()}"
                    : "Unable to locate a build desk.");

            if (_buildDeskClone != null)
            {
                Object.Destroy(_buildDeskClone);
            }
            _buildDeskClone = Object.Instantiate(_buildDesk);

            _buildDeskClone.name = BuildDesk;

            var needsRefresh = false;
            if (MainGame.me.save.unlocked_crafts.Count > _unlockedCraftListCount)
            {
                _unlockedCraftListCount = MainGame.me.save.unlocked_crafts.Count;
                needsRefresh = true;
            }

            if (needsRefresh)
            {
                foreach (var objectCraftDefinition in GameBalance.me.craft_obj_data.Where(x =>
                                 x.build_type == ObjectCraftDefinition.BuildType.Put)
                             .Where(a => a.icon.Length > 0)
                             .Where(b => !b.id.Contains("refugee"))
                             .Where(d => MainGame.me.save.IsCraftVisible(d))
                             .Where(e => !_craftDictionary.TryGetValue(GJL.L(e.GetNameNonLocalized()), out _)))

                {
                    var itemName = GJL.L(objectCraftDefinition.GetNameNonLocalized());
                    _craftDictionary.Add(itemName, objectCraftDefinition.id);
                }

                var craftList = _craftDictionary.ToList();
                craftList.Sort((pair1, pair2) => string.CompareOrdinal(pair1.Key, pair2.Key));

                craftList.ForEach(craft => { _craftsInventory.AddCraft(craft.Value); });
            }

            CrossModFields.CraftAnywhere = true;

            BuildModeLogics.last_build_desk = _buildDeskClone;

            MainGame.me.build_mode_logics.SetCurrentBuildZone(_buildDeskClone.obj_def.zone_id, "");
            GUIElements.me.craft.OpenAsBuild(_buildDeskClone, _craftsInventory);
            MainGame.paused = false;
        }

        [HarmonyPatch(typeof(BuildGrid))]
        public static class BuildGridPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(BuildGrid.ShowBuildGrid))]
            public static void ShowBuildGridPrefix(ref bool show)
            {
                if (!_cfg.DisableGrid) return;
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                show = false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(BuildGrid.ClearPreviousTotemRadius))]
            public static void ClearPreviousTotemRadiusPrefix(ref bool apply_colors)
            {
                if (!_cfg.DisableGrid) return;
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                apply_colors = false;
            }
        }

        [HarmonyPatch(typeof(BuildModeLogics))]
        public static class BuildModeLogicsPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(BuildModeLogics.EnterRemoveMode))]
            public static void BuildModeLogicsEnterRemoveMode(ref GameObject ____remove_grey_spr)
            {
                if (!_cfg.DisableGreyRemoveOverlay) return;
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                ____remove_grey_spr.SetActive(false);
            }

            [HarmonyPrefix]
            [HarmonyPatch("CancelCurrentMode")]
            public static void CancelCurrentModePrefix()
            {
                if (!CrossModFields.CraftAnywhere) return;
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                OpenCraftAnywhere();
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
                if (CrossModFields.CraftAnywhere && MainGame.me.player.cur_zone.Length <= 0)
                {
                    if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                    BuildGrid.ShowBuildGrid(false);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch("FocusCameraOnBuildZone")]
            private static void FocusCameraOnBuildZonePrefix(ref string zone_id)
            {
                if (!CrossModFields.CraftAnywhere) return;
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                zone_id = string.Empty;
            }

            [HarmonyPatch(nameof(BuildModeLogics.GetObjectRemoveCraftDefinition))]
            [HarmonyPostfix]
            private static void GetObjectRemoveCraftDefinitionPostfix(string obj_id, ref ObjectCraftDefinition __result)
            {
                if (!CrossModFields.CraftAnywhere) return;
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                Debug.LogError($"[Remove]{obj_id}");
                foreach (var objectCraftDefinition in GameBalance.me.craft_obj_data.Where(objectCraftDefinition =>
                             objectCraftDefinition.out_obj == obj_id && objectCraftDefinition.build_type ==
                             ObjectCraftDefinition.BuildType.Remove))
                {
                    __result = objectCraftDefinition;
                    return;
                }
                __result = null;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(BuildModeLogics.OnBuildCraftSelected))]
            private static void OnBuildCraftSelectedPrefix(ref string ____cur_build_zone_id, ref WorldZone ____cur_build_zone,
                ref Bounds ____cur_build_zone_bounds)
            {
                if (!CrossModFields.CraftAnywhere) return;
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                BuildModeLogics.last_build_desk = _buildDeskClone;
                ____cur_build_zone_id = Zone;
                ____cur_build_zone = WorldZone.GetZoneByID(Zone);
                ____cur_build_zone_bounds = ____cur_build_zone.GetBounds();
            }
        }

        [HarmonyPatch(typeof(FloatingWorldGameObject), "RecalculateAvailability")]
        public static class FloatingWorldGameObjectPatch
        {
   
            [HarmonyPostfix]
            [HarmonyPatch("RecalculateAvailability")]
            public static void Postfix()
            {
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                if (_cfg.DisableBuildingCollision)
                {
                    FloatingWorldGameObject.can_be_built = true;
                }
            }
        }

        [HarmonyPatch(typeof(FlowGridCell))]
        public static class FlowGridCellPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(FlowGridCell.IsInsideWorldZone))]
            public static void InsideWorldZonePostfix(ref bool __result)
            {
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                __result = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(FlowGridCell.IsPlaceAvailable))]
            public static void PlaceAvailablePostfix(ref bool __result)
            {
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                __result = true;
            }
            
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        public static class TimeOfDayUpdatePatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
//
                if (!MainGame.game_started || MainGame.me.player.is_dead || MainGame.me.player.IsDisabled() ||
                    MainGame.paused || !BaseGUI.all_guis_closed) return;

                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;

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

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.GetUniversalObjectInfo))]
        public static class GlobalCraftControlGuiUpdatePatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref WorldGameObject __instance, ref UniversalObjectInfo __result)
            {
//
                if (_buildDeskClone == null) return;
                if (__instance != _buildDeskClone) return;
                if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
                Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
                __result.header = strings.Header;
                __result.descr = strings.Description;
            }
        }

    }
}
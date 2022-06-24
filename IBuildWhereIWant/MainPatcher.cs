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
        private static Config.Options _cfg;
        private static WorldGameObject _buildDesk;
        private static CraftsInventory _craftsInventory;
        private static List<string> _hasRemoveCraftList;
        private static List<string> _alreadyAddedList;
        private static List<string> _alreadyAddedNameList;
        private static bool _isTalkingWithNpc;
        private static bool _craftAnywhere;

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
            BuildModeLogics.last_build_desk = _buildDesk;
            MainGame.me.build_mode_logics.SetCurrentBuildZone(_buildDesk?.obj_def.zone_id, "");
            GUIElements.me.craft.OpenAsBuild(_buildDesk, _craftsInventory);
            MainGame.paused = false;
        }

        [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.Interact))]
        public static class WorldGameObjectInteractPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref WorldGameObject __instance)
            {
                if (__instance.obj_def.interaction_type is ObjectDefinition.InteractionType.Builder)
                {
                    _craftAnywhere = false;
                }
            }
        }

        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
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

        [HarmonyPatch(typeof(BaseGUI), nameof(BaseGUI.Open), typeof(bool))]
        public static class BaseGuiOpenPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref BaseGUI __instance)
            {
                if (_craftAnywhere)
                {
                    foreach (var comp in __instance.GetComponentsInChildren<UILabel>().Where(a => a.name.Contains("header")))
                    {
                        comp.text = "Craft anywhere";
                    }
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
                if (_craftAnywhere)
                {
                    OpenCraftAnywhere();
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

            [HarmonyPrefix]
            [HarmonyPatch(nameof(BuildModeLogics.OnBuildCraftSelected))]
            private static void OnBuildCraftSelectedPrefix(ref string ____cur_build_zone_id, ref WorldZone ____cur_build_zone,
                ref Bounds ____cur_build_zone_bounds, ref MultiInventory ____multi_inventory)
            {
                if (_craftAnywhere)
                {
                    BuildModeLogics.last_build_desk = _buildDesk;
                    const string zone = "mf_wood";
                    ____cur_build_zone_id = zone;
                    ____cur_build_zone = WorldZone.GetZoneByID(zone, true);
                    ____cur_build_zone_bounds = ____cur_build_zone.GetBounds();
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

                    MainGame.me.player.Say($"Hmmm - I don't know how to remove {obj_id}...", null, false,
                        SpeechBubbleGUI.SpeechBubbleType.Think,
                        SmartSpeechEngine.VoiceID.None, true);
                    __result = null;
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
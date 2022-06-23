using HarmonyLib;
using System.Reflection;

using Rewired;
using UnityEngine;
using MaxButton;

//build project to resolve reference errors
namespace MaxButtonControllerSupport
{
    public class MainPatcher
    {
        private static bool _craftGuiOpen;
        private static bool _itemCountGuiOpen;
        private static CraftItemGUI _craftItemGui;
        private static WorldGameObject _crafteryWgo;
        private static SmartSlider _slider;

        public static void Patch()
        {
            try
            {
                if (Harmony.HasAnyPatches("com.graveyardkeeper.urbanvibes.maxbutton"))
                {
                    Debug.LogError($"[MaxButtonControllerSupport]: MaxButton found, continuing with patch process.");
                    var harmony = new Harmony("p1xel8ted.GraveyardKeeper.MaxButtonControllerSupport");
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                }
                else
                {
                    Debug.LogError($"[MaxButtonControllerSupport]: MaxButton not found, aborting patch process.");
                }
            }
            catch (System.Exception ex) 
            {
                Debug.LogError($"[MaxButtonControllerSupport]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
            }
        }


        [HarmonyPatch(typeof(CraftGUI))]
        public static class CraftGuiPatchMbcs
        {
            [HarmonyPatch(nameof(CraftGUI.Open))]
            [HarmonyPostfix]
            public static void OpenPostfix()
            {
                _craftGuiOpen = true;
            }

            [HarmonyPatch(nameof(CraftGUI.OnClosePressed))]
            [HarmonyPostfix]
            public static void ClosePostfix()
            {
                _craftGuiOpen = false;
            }
        }


        [HarmonyPatch(typeof(CraftItemGUI), nameof(CraftItemGUI.OnOver))]
        public static class CraftItemGuiOnOverPatchMbcs
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                _craftItemGui = CraftItemGUI.current_overed;
                _crafteryWgo = GUIElements.me.craft.GetCrafteryWGO();

            }
        }

        [HarmonyPatch(typeof(ItemCountGUI))]
        public static class ItemCountGuiMbcs
        {
            [HarmonyPostfix]
            [HarmonyPatch("Open")]
            public static void OpenPostfix(ref ItemCountGUI __instance)
            {
                //if (!MainGame.game_started || MainGame.me.player.is_dead || MainGame.me.player.IsDisabled()) return;
                _itemCountGuiOpen = true;
                _slider = __instance.transform.Find("window/Container/smart slider").GetComponent<SmartSlider>();
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnPressedBack")]
            public static void HidePostfix()
            {
                _itemCountGuiOpen = false;
            }


            [HarmonyPostfix]
            [HarmonyPatch("OnConfirm")]
            public static void OnClosePressedPostfix()
            {
                _itemCountGuiOpen = false;
            }

        }


        [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
        public static class TimeOfDayUpdatePatchMbcs
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (!MainGame.game_started || MainGame.me.player.is_dead || MainGame.me.player.IsDisabled()) return;


                //Up = 10
                if (LazyInput.gamepad_active && ReInput.players.GetPlayer(0).GetButtonDown(10) && _itemCountGuiOpen)
                {
                    typeof(MaxButtonVendor).GetMethod("SetMaxPrice", AccessTools.all)
                        ?.Invoke(typeof(MaxButtonVendor), new object[]
                        {
                            _slider

                        });
                }

                //Down = 11
                if (LazyInput.gamepad_active && ReInput.players.GetPlayer(0).GetButtonDown(11) && _itemCountGuiOpen)
                {
                    typeof(MaxButtonVendor).GetMethod("SetSliderValue", AccessTools.all)
                        ?.Invoke(typeof(MaxButtonVendor), new object[]
                        {
                            _slider,
                            1

                        });
                }


                //RT = 19
                if (LazyInput.gamepad_active && ReInput.players.GetPlayer(0).GetButtonDown(19) && _craftGuiOpen)
                {
                    typeof(MaxButtonCrafting).GetMethod("SetMaximumAmount", AccessTools.all)
                        ?.Invoke(typeof(MaxButtonCrafting), new object[]
                        {
                            _craftItemGui,
                            _crafteryWgo

                        });
                }

                //LT = 20
                if (LazyInput.gamepad_active && ReInput.players.GetPlayer(0).GetButtonDown(20) && _craftGuiOpen)
                {
                    typeof(MaxButtonCrafting).GetMethod("SetMinimumAmount", AccessTools.all)
                        ?.Invoke(typeof(MaxButtonCrafting), new object[]
                        {
                            _craftItemGui
                        });
                }

            }
        }

    }
}
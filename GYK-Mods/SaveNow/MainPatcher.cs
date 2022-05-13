using System.Globalization;
using System.IO;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace SaveNow
{
    public class MainPatcher
    {
        public static Vector3 Pos;
        public static string[] Xyz;
        public static float X, Y, Z;
        public static string DataPath, ErrorPath;


        public static void Patch()
        {
            var val = HarmonyInstance.Create("p1xel8ted.graveyardkeeper.savenow");
            val.PatchAll(Assembly.GetExecutingAssembly());
            DataPath = "QMods//SaveNow//dont-remove.dat";
            ErrorPath = "QMods//SaveNow//error.txt";
        }

        public static bool SaveLocation(bool menuExit)
        {
            Pos = MainGame.me.player.pos3;
            var x = Pos.x;
            var y = Pos.y;
            var z = Pos.z;
            string[] xyz =
            {
                x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture),
                z.ToString(CultureInfo.InvariantCulture)
            };
            File.WriteAllLines(DataPath, xyz);
            if (!menuExit)
            {
                EffectBubblesManager.ShowImmediately(Pos, "Game Saved!", EffectBubblesManager.BubbleColor.Relation,
                    true, 3f, false);
            }

            return true;
        }

        public static void RestoreLocation()
        {
            Xyz = File.ReadAllLines(DataPath);
            X = float.Parse(Xyz[0]);
            Y = float.Parse(Xyz[1]);
            Z = float.Parse(Xyz[2]);
            Pos.Set(X, Y, Z);
            MainGame.me.player.PlaceAtPos(Pos);
            var home = MainGame.me.player.GetMyWorldZone().name;
            if (!home.EndsWith("home_zone"))
            {
                EffectBubblesManager.ShowImmediately(Pos, "Woooah! What a rush! Gets me every time!",
                    EffectBubblesManager.BubbleColor.Relation, true, 4f, false);
            }
        }

        [HarmonyPatch(typeof(InGameMenuGUI), "OnPressedSaveAndExit")]
        public static class PatchSaveAndExit
        {
            public static bool Prefix()
            {
                return false;
            }

            public static void Postfix(InGameMenuGUI __instance)
            {
                __instance.SetControllsActive(false);
                __instance.OnClosePressed();
                GUIElements.me.dialog.OpenYesNo(
                    "Are you sure you want to exit?" + "\n\n" + "Progress and current location will be saved.", delegate
                    {
                        if (SaveLocation(true))
                        {
                            LoadingGUI.Show(__instance.ReturnToMainMenu);
                        }
                    }, null, delegate { __instance.SetControllsActive(true); });
            }
        }

        [HarmonyPatch(typeof(SleepGUI), "WakeUp")]
        public static class PatchSavePosWhenUsingBed
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                SaveLocation(false);
            }
        }

        [HarmonyPatch(typeof(GameSave))]
        [HarmonyPatch(nameof(GameSave.GlobalEventsCheck))]
        public static class PatchLoadGame
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                RestoreLocation();
            }
        }

        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.Update))]
        public static class PatchSaveGame
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (Input.GetKeyUp(KeyCode.K))
                {
                    PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                        delegate(SaveSlotData slot) { SaveLocation(false); });
                }
            }
        }
    }
}
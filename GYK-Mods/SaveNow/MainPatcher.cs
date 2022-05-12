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
        public static string DataPath;

        public static void Patch()
        {
            var val = HarmonyInstance.Create("p1xel8ted.graveyardkeeper.savenow");
            val.PatchAll(Assembly.GetExecutingAssembly());
            DataPath = "QMods//SaveNow//dont-remove.dat";
        }

        public static void SaveLocation()
        {
            Pos = MainGame.me.player.pos3;
            var x = Pos.x;
            var y = Pos.y;
            var z = Pos.z;
            string[] xyz = { x.ToString(CultureInfo.InvariantCulture), y.ToString(CultureInfo.InvariantCulture), z.ToString(CultureInfo.InvariantCulture) };
            File.WriteAllLines(DataPath, xyz);
        }

        public static void RestoreLocation()
        {
            Xyz = File.ReadAllLines(DataPath);
            X = float.Parse(Xyz[0]);
            Y = float.Parse(Xyz[1]);
            Z = float.Parse(Xyz[2]);
            Pos.Set(X, Y, Z);
            MainGame.me.player.PlaceAtPos(Pos);
        }

        [HarmonyPatch(typeof(SleepGUI), "WakeUp")]
        public static class PatchSavePosWhenUsingBed
        {
            public static void Prefix()
            {
                SaveLocation();
            }
        }

        [HarmonyPatch(typeof(GameSave))]
        [HarmonyPatch(nameof(GameSave.GlobalEventsCheck))]
        public static class PatchLoadGame 
        {
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
                    PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save, delegate (SaveSlotData slot)
                    {
                        SaveLocation();
                    });

                }
            }
        }
    }
}
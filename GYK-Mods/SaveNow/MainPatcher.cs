using System.IO;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace SaveNow
{
    public class MainPatcher
    {
        public static Vector3 pos;
        public static string[] xyz;
        public static float x, y, z;
        public static string dataPath;


        public static void Patch()
        {
            HarmonyInstance val = HarmonyInstance.Create("p1xel8ted.graveyardkeeper.savenow");
            val.PatchAll(Assembly.GetExecutingAssembly());
            dataPath = "QMods//SaveNow//last_loc.txt";
        }

        public static void SaveLocation()
        {
            pos = MainGame.me.player.pos3;
            var x = pos.x;
            var y = pos.y;
            var z = pos.z;
            string[] xyz = { x.ToString(), y.ToString(), z.ToString() };
            File.WriteAllLines(dataPath, xyz);
        }

        public static void RestoreLocation()
        {
            xyz = File.ReadAllLines(dataPath);
            x = float.Parse(xyz[0]);
            y = float.Parse(xyz[1]);
            z = float.Parse(xyz[2]);
            pos.Set(x, y, z);
            MainGame.me.player.PlaceAtPos(pos);
        }

        [HarmonyPatch(typeof(SleepGUI), "WakeUp")]
        public static class Patch_SavePosWhenUsingBed
        {
            public static void Prefix()
            {
                SaveLocation();
            }
        }

        [HarmonyPatch(typeof(GameSave))]
        [HarmonyPatch(nameof(GameSave.GlobalEventsCheck))]
        public static class Patch_LoadGame 
        {
            public static void Prefix()
            {
                RestoreLocation();
            }
        }

        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.Update))]
        public static class Patch_SaveGame
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
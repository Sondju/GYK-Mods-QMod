using HarmonyLib;
using Helper;
using System;
using System.Reflection;

namespace TreesNoMore;

public class MainPatcher
{
    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.TreesNoMore");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("TreesNoMore", $"{message}", error);
    }

    //makes the racks and the barman inventory larger
    [HarmonyPatch(typeof(WorldGameObject), "InitNewObject")]
    public static class WorldGameObjectInitNewObjectPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref WorldGameObject __instance)
        {
            if (__instance == null) return;
            if (__instance.obj_id.Contains("stump"))
            {
                UnityEngine.Object.Destroy(__instance.gameObject);
            }
        }

        [HarmonyFinalizer]
        public static Exception Finalizer()
        {
            return null;
        }
    }

    [HarmonyPatch(typeof(WorldGameObject), "SmartInstantiate")]
    public static class WorldGameObjectSmartInstantiatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref WorldObjectPart prefab)
        {
            if (prefab == null) return;
            if ((!MainGame.game_started && !MainGame.game_starting) || !prefab.name.Contains("tree") || prefab.name.Contains("bees")) return;
            if (prefab.name.Contains("apple")) return;
            prefab = null;
        }

        [HarmonyFinalizer]
        public static Exception Finalizer()
        {
            return null;
        }
    }
}
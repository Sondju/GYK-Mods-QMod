using HarmonyLib;
using System.Reflection;
using UnityEngine;

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
        catch (System.Exception ex)
        {
            Debug.LogError($"[TreesNoMore]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }

    [HarmonyPatch(typeof(WorldGameObject), "SmartInstantiate")]
    public static class WorldGameObjectSmartInstantiatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref WorldObjectPart prefab)
        {
            if ((!MainGame.game_started && !MainGame.game_starting) || !prefab.name.Contains("tree")) return;
            if (prefab.name.Contains("apple")) return;
            prefab = null;
        }
    }
}
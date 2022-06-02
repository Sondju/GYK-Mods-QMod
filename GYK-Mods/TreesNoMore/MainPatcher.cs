using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace TreesNoMore
{
    public class MainPatcher
    {

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.TreesNoMore");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        [HarmonyPatch(typeof(WorldGameObject), "SmartInstantiate")]
        public static class PatchLoadGame 
        {
            [HarmonyPrefix]
            public static void Prefix(ref WorldObjectPart prefab)
            {
                if ((!MainGame.game_started && !MainGame.game_starting) || !prefab.name.Contains("tree")) return;
                UnityEngine.Debug.LogError(prefab.name);
                if (prefab.name.Contains("apple")) return;
                prefab = null;
            }
        }
    }
}
using System.Reflection;
using HarmonyLib;

namespace TreesNoMore;

public class MainPatcher
{
    public static void Patch()
    {
        var harmony = new Harmony("p1xel8ted.GraveyardKeeper.TreesNoMore");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
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
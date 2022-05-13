using System.Reflection;
using Harmony;

namespace TreesNoMore
{
    public class MainPatcher
    {

        public static void Patch()
        {
            var val = HarmonyInstance.Create("p1xel8ted.graveyardkeeper.TreesNoMore");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(WorldGameObject), "SmartInstantiate")]
        public static class PatchLoadGame 
        {
            [HarmonyPrefix]
            public static void Prefix(ref WorldObjectPart prefab)
            {
                if ((MainGame.game_started || MainGame.game_starting) && prefab.name.Contains("tree"))
                {
                    prefab = null;
                }
            }
        }
    }
}
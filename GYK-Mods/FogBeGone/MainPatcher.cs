using Harmony;
using System.Reflection;

namespace FogBeGone
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.FogBeGone");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(FogObject))]
        [HarmonyPatch(nameof(FogObject.InitFog))]
        public static class ModPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref FogObject prefab)
            {
                prefab = null;
            }
        }
    }
}
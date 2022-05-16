using Harmony;
using System.Reflection;

namespace Test
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.Test");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof())]
        [HarmonyPatch(nameof())]
        public static class ModPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {

            }

            [HarmonyPostfix]
            public static void Postfix()
            {

            }
        }
    }
}
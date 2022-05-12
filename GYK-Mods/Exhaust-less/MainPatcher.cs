using System.Reflection;
using Harmony;

namespace Exhaust_Less
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create("p1xel8ted.graveyardkeeper.exhaust-less");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(PlayerComponent))]
        [HarmonyPatch(nameof(PlayerComponent.TrySpendEnergy))]
        public static class PatchTrySpendEnergy
        {
            [HarmonyPrefix]
            public static void Prefix(ref float needEnergy)
            {
                needEnergy /= 2f;
            }
        }
    }
}
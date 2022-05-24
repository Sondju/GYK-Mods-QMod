using System.Collections.Generic;
using Harmony;
using System.Reflection;

namespace CraftToStorage
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.CraftToStorage");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(WorldGameObject))]
        [HarmonyPatch(nameof(WorldGameObject.DropItem))]
        public static class ModPatch
        {

            [HarmonyPostfix]
            public static void Postfix(WorldGameObject __instance, ref Item item)
            {
                var inL = new List<Item> {item};
                __instance.PutToAllPossibleInventories(inL, out var outL);
                if (outL is {Count: > 0})
                {
                    __instance.DropItems(outL);
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject))]
        [HarmonyPatch(nameof(WorldGameObject.DropItems))]
        public static class ModPatch2
        {

            [HarmonyPostfix]
            public static void Postfix(WorldGameObject __instance, ref List<Item> items)
            {
                __instance.PutToAllPossibleInventories(items, out var outL);
                if (outL is { Count: > 0 })
                {
                    __instance.DropItems(outL);
                }
            }
        }
    }
}
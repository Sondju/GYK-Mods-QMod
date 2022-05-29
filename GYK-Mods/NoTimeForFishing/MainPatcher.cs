using Harmony;
using System.Reflection;
using Fishing;
using UnityEngine;

namespace NoTimeForFishing
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.NoTimeForFishing");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }
        
        [HarmonyPatch(typeof(FishLogic), "CalculateFishPos")]
        public class PatchCalculateFishPos
        {
            [HarmonyPrefix]
            public static void Prefix(ref float pos, ref float rod_zone_size)
            {
                pos = 0f;
                rod_zone_size = 100f;
            }
        }

        [HarmonyPatch(typeof(FishingGUI), "UpdateWaitingForBite", null)]
        internal class PatchUpdateWaitingForBite
        {
            [HarmonyPrefix]
            public static void Prefix(ref float ____waiting_for_bite_delay)
            {
                ____waiting_for_bite_delay = 0f;
            }

            [HarmonyPostfix]
            private static void Postfix(FishingGUI __instance, ref Item ____fish, ref float ____waiting_for_bite_delay, ref FishDefinition ____fish_def, ref FishPreset ____fish_preset)
            {
                var fishy = (FishDefinition)typeof(FishingGUI)
                    .GetMethod("GetRandomFish", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(__instance, new object[]
                    {
                        ____waiting_for_bite_delay
                    });

                ____fish_def = fishy;
                ____fish = new Item(____fish_def.item_id, 1);
                ____fish_preset = Resources.Load<FishPreset>("MiniGames/Fishing/" + ____fish_def.fish_preset);
                typeof(FishingGUI).GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(__instance, new object[]
                {
                    FishingGUI.FishingState.WaitingForPulling
                });
                
            }
        }

        [HarmonyPatch(typeof(FishingGUI), "UpdateWaitingForPulling", null)]
        internal class PatchUpdateWaitingForPulling
        {
            [HarmonyPostfix]
            private static void PostFix(FishingGUI __instance, ref bool ___is_success_fishing)
            {
                ___is_success_fishing = true;
                typeof(FishingGUI).GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(__instance, new object[]
                {
                    FishingGUI.FishingState.Pulling
                });
                
            }
        }
        
        [HarmonyPatch(typeof(FishingGUI), "UpdatePulling", null)]
        internal class PatchUpdatePulling
        {

            [HarmonyPostfix]
            private static void Postfix(FishingGUI __instance)
            {
                typeof(FishingGUI).GetMethod("ChangeState", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(__instance, new object[]
                {
                    FishingGUI.FishingState.TakingOut
                });
            }
        }
    }
}
    

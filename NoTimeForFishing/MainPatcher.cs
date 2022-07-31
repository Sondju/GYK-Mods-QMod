using Fishing;
using HarmonyLib;
using Helper;
using System;
using System.Reflection;
using UnityEngine;

namespace NoTimeForFishing;

public class MainPatcher
{
    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.NoTimeForFishing");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("NoTimeForFishing", $"{message}", error);
    }

    [HarmonyPatch(typeof(FishLogic), "CalculateFishPos")]
    public class FishLogicCalculateFishPosPath
    {
        [HarmonyPrefix]
        public static void Prefix(ref float pos, ref float rod_zone_size)
        {
            pos = 0f;
            rod_zone_size = 100f;
        }
    }

    [HarmonyPatch(typeof(FishingGUI), "UpdateWaitingForBite", null)]
    internal class FishingGuiUpdateWaitingForBitePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref float ____waiting_for_bite_delay)
        {
            ____waiting_for_bite_delay = 0f;
        }

        [HarmonyPostfix]
        private static void Postfix(FishingGUI __instance, ref Item ____fish, ref float ____waiting_for_bite_delay,
            ref FishDefinition ____fish_def, ref FishPreset ____fish_preset)
        {
            var fishy = (FishDefinition)typeof(FishingGUI)
                .GetMethod("GetRandomFish", AccessTools.all)
                ?.Invoke(__instance, new object[]
                {
                    ____waiting_for_bite_delay
                });

            ____fish_def = fishy;
            ____fish = new Item(____fish_def.item_id, 1);
            ____fish_preset = Resources.Load<FishPreset>("MiniGames/Fishing/" + ____fish_def.fish_preset);
            typeof(FishingGUI).GetMethod("ChangeState", AccessTools.all)
                ?.Invoke(__instance, new object[]
                {
                    FishingGUI.FishingState.WaitingForPulling
                });
        }
    }

    [HarmonyPatch(typeof(FishingGUI), "UpdateWaitingForPulling", null)]
    internal class FishingGuiUpdateWaitingForPullingPatch
    {
        [HarmonyPostfix]
        private static void PostFix(FishingGUI __instance, ref bool ___is_success_fishing)
        {
            ___is_success_fishing = true;
            typeof(FishingGUI).GetMethod("ChangeState", AccessTools.all)
                ?.Invoke(__instance, new object[]
                {
                    FishingGUI.FishingState.Pulling
                });
        }
    }

    [HarmonyPatch(typeof(FishingGUI), "UpdatePulling", null)]
    internal class FishingGuiUpdatePullingPatch
    {
        [HarmonyPostfix]
        private static void Postfix(FishingGUI __instance)
        {
            typeof(FishingGUI).GetMethod("ChangeState", AccessTools.all)
                ?.Invoke(__instance, new object[]
                {
                    FishingGUI.FishingState.TakingOut
                });
        }
    }
}
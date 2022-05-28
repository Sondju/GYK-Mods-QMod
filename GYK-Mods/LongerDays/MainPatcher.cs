using Harmony;
using System.Reflection;

namespace LongerDays
{
    public class MainPatcher
    {
        public static float SecondsInDay50 = 675f;
        public static float SecondsInDay100 = 900f;
        private static Config.Options _cfg;
        public static float Seconds;

        public static void Patch()
        {
            _cfg = Config.GetOptions();
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.LongerDays");
            val.PatchAll(Assembly.GetExecutingAssembly());
            Seconds = _cfg.DoubleLengthDays ? SecondsInDay100 : SecondsInDay50;
        }

        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.GetSecondsToTheMorning))]
        public static class ModPatch0
        {
            [HarmonyPostfix]
            public static void Postfix(TimeOfDay __instance, ref float __result)
            {
                var num = __instance.GetTimeK() - 0.15f;
                float result;
                if (num < 0f)
                {
                    result = num * -1f * Seconds;
                }
                else
                {
                    result = (1f - __instance.GetTimeK() + 0.15f) * Seconds;
                }
                __result = result;
            }
        }

        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.GetSecondsToTheMidnight))]
        public static class ModPatch1
        {
            [HarmonyPostfix]
            public static void Postfix(TimeOfDay __instance, ref float __result)
            {
                __result = (1f - __instance.GetTimeK()) * Seconds;
            }
        }

        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.FromSecondsToTimeK))]
        public static class ModPatch2
        {
            [HarmonyPostfix]
            public static void Postfix(ref float time_in_secs, ref float __result)
            {
                __result = time_in_secs / Seconds;
            }
        }

        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.FromTimeKToSeconds))]
        public static class ModPatch3
        {
            [HarmonyPostfix]
            public static void Postfix(ref float time_in_time_k, ref float __result)
            {
                
                __result = time_in_time_k * Seconds;
            }
        }
    }
}
using System.Reflection;
using HarmonyLib;

namespace LongerDays;

public class MainPatcher
{
    //public static float SecondsInDay = 450f; //0x increase
    private const float SecondsInDay50 = 675f; //0.5x increase
    private const float SecondsInDay100 = 900f; //2x increase
    private const float SecondsInDay150 = 1125f; //2.5x increase
    private const float SecondsInDay200 = 1350f; //3x increase
    private static Config.Options _cfg;
    private static float _seconds;

    public static void Patch()
    {
        _cfg = Config.GetOptions();

        var harmony = new Harmony("p1xel8ted.GraveyardKeeper.LongerDays");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        if (_cfg.Madness)
            _seconds = SecondsInDay200;
        else if (_cfg.EvenLongerDays)
            _seconds = SecondsInDay150;
        else if (_cfg.DoubleLengthDays)
            _seconds = SecondsInDay100;
        else
            _seconds = SecondsInDay50;
    }

    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.GetSecondsToTheMorning))]
    public static class TimeOfDayGetSecondsToTheMorningPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TimeOfDay __instance, ref float __result)
        {
            var num = __instance.GetTimeK() - 0.15f;
            float result;
            if (num < 0f)
                result = num * -1f * _seconds;
            else
                result = (1f - __instance.GetTimeK() + 0.15f) * _seconds;

            __result = result;
        }
    }

    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.GetSecondsToTheMidnight))]
    public static class TimeOfDayGetSecondsToTheMidnightPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TimeOfDay __instance, ref float __result)
        {
            __result = (1f - __instance.GetTimeK()) * _seconds;
        }
    }

    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.FromSecondsToTimeK))]
    public static class TimeOfDayFromSecondsToTimeKPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float time_in_secs, ref float __result)
        {
            __result = time_in_secs / _seconds;
        }
    }

    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.FromTimeKToSeconds))]
    public static class TimeOfDayFromTimeKToSecondsPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float time_in_time_k, ref float __result)
        {
            __result = time_in_time_k * _seconds;
        }
    }
}
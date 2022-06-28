using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LongerDays;

public class MainPatcher
{
    //public static float SecondsInDay450 = 450f; //0x increase - game default
    private const float SecondsInDay675 = 675f; //1.5x increase

    private const float SecondsInDay900 = 900f; //2x increase
    private const float SecondsInDay1125 = 1125f; //2.5x increase
    private const float SecondsInDay1350 = 1350f; //3x increase
    private static Config.Options _cfg;
    private static float _seconds;

    public static void Patch()
    {
        try
        {
            _cfg = Config.GetOptions();

            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.LongerDays");

            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

            if (_cfg.Madness)
                _seconds = SecondsInDay1350;
            else if (_cfg.EvenLongerDays)
                _seconds = SecondsInDay1125;
            else if (_cfg.DoubleLengthDays)
                _seconds = SecondsInDay900;
            else
                _seconds = SecondsInDay675;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LongerDays]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }

    [HarmonyPatch(typeof(EnvironmentEngine), nameof(EnvironmentEngine.Update))]
    public static class EnvironmentEngineUpdatePatch
    {
        public static float GetTime()
        {
            var adj = GetTimeMulti();
            var time = Time.deltaTime;
            var newTime = time / adj;
            return newTime;
        }

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            var time = AccessTools.Property(typeof(Time), nameof(Time.deltaTime)).GetGetMethod();

            foreach (var t in instructionsList)
            {
                var instruction = t;
                if (instruction.opcode == OpCodes.Call && instruction.OperandIs(time))
                {
                    yield return instruction;
                    instruction = new CodeInstruction(opcode: OpCodes.Call,
                        operand: typeof(EnvironmentEngineUpdatePatch).GetMethod("GetTime"));
                }
                yield return instruction;
            }
        }
    }

    private static float GetTimeMulti()
    {
        var num = _seconds switch
        {
            SecondsInDay675 => 1.5f,
            SecondsInDay900 => 2f,
            SecondsInDay1125 => 2.5f,
            SecondsInDay1350 => 3f,
            _ => 1f
        };
        return num;
    }

    //only used by refugee cooking
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

    //this is only used by flow canvas?
    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.GetSecondsToTheMidnight))]
    public static class TimeOfDayGetSecondsToTheMidnightPatch
    {
        [HarmonyPostfix]
        public static void Postfix(TimeOfDay __instance, ref float __result)
        {
            __result = (1f - __instance.GetTimeK()) * _seconds;
        }
    }

    //used by weather systems
    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.FromSecondsToTimeK))]
    public static class TimeOfDayFromSecondsToTimeKPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float time_in_secs, ref float __result)
        {
            __result = time_in_secs / _seconds;
        }
    }
}
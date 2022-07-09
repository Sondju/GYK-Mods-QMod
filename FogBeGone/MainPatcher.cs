using System;
using HarmonyLib;
using System.Reflection;
using FlowCanvas;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FogBeGone;

public class MainPatcher
{

    private static bool _introPlaying;
    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.FogBeGone");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _introPlaying = false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FogBeGone]: {ex.Message}, {ex.Source}, {ex.StackTrace}");
        }
    }


    [HarmonyPatch(typeof(CustomFlowScript), nameof(CustomFlowScript.Create), typeof(GameObject), typeof(FlowGraph), typeof(bool), typeof(CustomFlowScript.OnFinishedDelegate), typeof(string))]
    public static class CustomFlowScriptCreatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref FlowGraph g)
        {
            _introPlaying = string.Equals(g.name, "red_eye_talk_1");
        }
    }

    [HarmonyPatch(typeof(SmartWeatherState), nameof(SmartWeatherState.Update))]
    public static class SmartWeatherStateUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(SmartWeatherState __instance, ref bool ____previously_enabled,
            ref bool ____enabled, ref float ____cur_amount)
        {
            if (!MainGame.game_started) return;
            if (__instance == null) return;
            if (_introPlaying) return;
            switch (__instance.type)
            {
                case SmartWeatherState.WeatherType.Fog:
                    ____previously_enabled = true;
                    ____enabled = false;
                    ____cur_amount = 0;
                    __instance.value = 0;
                    break;

                case SmartWeatherState.WeatherType.Wind:
                    ____previously_enabled = true;
                    ____enabled = false;
                    ____cur_amount = 0;
                    __instance.value = 0;
                    break;

                case SmartWeatherState.WeatherType.Rain:
                    ____previously_enabled = false;
                    break;

                case SmartWeatherState.WeatherType.LUT:
                    break;
            }
        }
    }
}
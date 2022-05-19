using System.Collections.Generic;
using System.IO;
using Harmony;
using System.Reflection;

namespace FogBeGone
{
    public class MainPatcher
    {
        public static void Patch()
        {
            try
            {
                var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.FogBeGone");
                val.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (System.Exception ex)
            {
                File.AppendAllText("./QMods/errors.txt",
                    "FogBeGone: " + ex.Message + " : " + ex.Source + " : " + ex.StackTrace + " : " + ex.Data);
            }
        }

        [HarmonyPatch(typeof(SmartWeatherState))]
        [HarmonyPatch(nameof(SmartWeatherState.Update))]
        public static class WeatherPatch1
        {
              [HarmonyPrefix]
            public static bool Prefix(SmartWeatherState __instance, ref bool ____previously_enabled, ref bool ____enabled, ref float ____cur_amount)
            {
                try
                {
                    if (__instance != null)
                    {
                        if (__instance.type == SmartWeatherState.WeatherType.Fog)
                        {
                            ____previously_enabled = true;
                            ____enabled = false;
                            ____cur_amount = 0;
                            __instance.value = 0;
                        }

                        if (__instance.type == SmartWeatherState.WeatherType.Wind)
                        {
                            ____previously_enabled = true;
                            ____enabled = false;
                            ____cur_amount = 0;
                            __instance.value = 0;
                        }

                        if (__instance.type == SmartWeatherState.WeatherType.Rain)
                        {
                            ____previously_enabled = false;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    File.AppendAllText("./QMods/errors.txt",
                        "FogBeGone: " + ex.Message + " : " + ex.Source + " : " + ex.StackTrace + " : " + ex.Data);
                }

                return true;
            }
        }


        //[HarmonyPatch(typeof(EnvironmentEngine), "SetWeatherEnabled")]
        //public static class WeatherPatch2
        //{
        //    [HarmonyPrefix]
        //    public static bool Prefix()
        //    {
        //        return false;
        //    }

        //    [HarmonyPostfix]
        //    public static void Postfix(EnvironmentEngine __instance, bool enable)
        //    {
        //        string message = null;
        //        var array = __instance.states;
        //        foreach (var t in array)
        //        {
        //            message += $"Type: {t.type.ToString()}";
        //            if (t.type.ToString().Equals("Fog"))
        //            {
        //                t.enabled = false;
        //                message += $"Disabling {t.type}!\n";
        //                continue;
        //            }

        //            if (t.type.ToString().Equals("Wind"))
        //            {
        //                t.enabled = false;
        //                message += $"Disabling {t.type}!\n";
        //                continue;
        //            }

        //            t.enabled = true;
        //            message += $"Enabling {t.type}!\n";
        //        }

        //        File.AppendAllText("./QMods/FogBeGone/info.txt", message);
        //    }
        //}


        //[HarmonyPatch(typeof(FogObject))]
        //[HarmonyPatch(nameof(FogObject.InitFog))]
        //public static class FogModPatch
        //{
        //    [HarmonyPrefix]
        //    public static void Prefix(ref FogObject prefab)
        //    {
        //        prefab = null;
        //    }
        //}
    }
}
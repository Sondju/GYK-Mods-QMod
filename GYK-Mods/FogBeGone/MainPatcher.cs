using System.IO;
using Harmony;
using System.Reflection;

namespace FogBeGone
{
    public class MainPatcher
    {
        public static void Patch()
        {
  
                var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.FogBeGone");
                val.PatchAll(Assembly.GetExecutingAssembly());
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
    }
}
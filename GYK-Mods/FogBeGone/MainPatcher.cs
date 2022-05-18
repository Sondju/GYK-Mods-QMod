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
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.FogBeGone");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }


        [HarmonyPatch(typeof(EnvironmentEngine), "SetWeatherEnabled")]
        //[HarmonyPatch(nameof(EnvironmentEngine.Update))]
        public static class WeatherPatch
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                return false;
            }

            [HarmonyPostfix]
            public static void Postfix(EnvironmentEngine __instance, bool enable)
            {
                string message = null;
                var array = __instance.states;
                foreach (var t in array)
                {
                    message += $"State: {t.type}\n";
                    if (t.type != SmartWeatherState.WeatherType.Rain ||
                        t.type != SmartWeatherState.WeatherType.LUT)
                    {
                        message += $"{t.type} : Enabled, False\n";
                        t.SetEnabled(false);
                    }
                    else
                    {
                        message += $"{t.type} : Enabled, True\n";
                        t.SetEnabled(true);
                    }
                }
                File.WriteAllText("./QMods/FogBeGone/info.txt", message);
            }
        }


        [HarmonyPatch(typeof(FogObject))]
        [HarmonyPatch(nameof(FogObject.InitFog))]
        public static class FogModPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref FogObject prefab)
            {
                prefab = null;
            }
        }
    }
}
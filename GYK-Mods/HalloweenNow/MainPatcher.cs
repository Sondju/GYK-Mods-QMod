using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace HalloweenNow
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.graveyardkeeper.HalloweenNow");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);

            _cfg = Config.GetOptions();
        }

        //makes halloween an annual event instead of the original 2018...
        [HarmonyPatch(typeof(GameSave))]
        [HarmonyPatch(nameof(GameSave.GlobalEventsCheck))]
        internal class PatchHalloweenEvent
        {
            [HarmonyPrefix]
            private static bool Prefix()
            {
                return false;
            }

            [HarmonyPostfix]
            private static void Postfix()
            {
                var year = DateTime.Now.Year;
                foreach (var globalEventBase in new List<GlobalEventBase>()
                         {
                           new("halloween", new DateTime(year, 10, 29), new TimeSpan(14, 0, 0, 0))
                             {
                                 on_start_script = new Scene1100_To_SceneHelloween(),
                                 on_finish_script = new SceneHelloween_To_Scene1100()
                             }
                         })
                    globalEventBase.Process();
            }
        }
    }
}
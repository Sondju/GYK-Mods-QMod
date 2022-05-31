using System;
using System.IO;
using HarmonyLib;

namespace FasterCraft
{
    [HarmonyPatch(typeof(CraftComponent))]
    [HarmonyPatch("DoAction")]
    internal class CraftComponent_Patcher
    {
        [HarmonyPrefix]
        private static bool Prefix(CraftComponent __instance, ref WorldGameObject other_obj, ref float delta_time)
        {
            string[] strArray = new StreamReader("./QMods/FasterCraft/config.txt").ReadLine().Split('=');
            if (__instance.current_craft.is_auto)
                delta_time *= (float) Convert.ToDouble(strArray[1]);
            if (!__instance.current_craft.is_auto)
                delta_time *= (float) Convert.ToDouble(strArray[1]);
            return true;
        }
    }
}
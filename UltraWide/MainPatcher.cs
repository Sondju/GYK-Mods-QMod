using System.Reflection;
using HarmonyLib;

namespace UltraWide;

public class MainPatcher
{
    public static void Patch()
    {
        var harmony = new Harmony("p1xel8ted.GraveyardKeeper.UltraWide");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    [HarmonyBefore("com.p1xel8ted.graveyardkeeper.LargerScale")]
    [HarmonyPatch(typeof(ResolutionConfig), nameof(ResolutionConfig.GetResolutionConfigOrNull))]
    public static class ResolutionConfigGetResolutionConfigOrNullPatch
    {
        [HarmonyPostfix]
        public static void Postfix(int width, int height, ref ResolutionConfig __result)
        {
            __result ??= new ResolutionConfig(width, height);
        }
    }
}
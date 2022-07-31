using HarmonyLib;
using Helper;
using System;
using System.Reflection;

namespace LargerScale;

public class MainPatcher
{
    private static Config.Options _cfg;

    public static void Patch()
    {
        try
        {
            _cfg = Config.GetOptions();

            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.LargerScale");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("LargerScale", $"{message}", error);
    }

    [HarmonyAfter("com.p1xel8ted.graveyardkeeper.UltraWide")]
    [HarmonyPatch(typeof(ResolutionConfig), nameof(ResolutionConfig.GetResolutionConfigOrNull))]
    public static class ResolutionConfigGetResolutionConfigOrNullPatch
    {
        [HarmonyPostfix]
        public static void Postfix(int width, int height, ref ResolutionConfig __result)
        {
            __result ??= new ResolutionConfig(width, height)
            {
                pixel_size = _cfg.GameScale
            };
        }
    }
}
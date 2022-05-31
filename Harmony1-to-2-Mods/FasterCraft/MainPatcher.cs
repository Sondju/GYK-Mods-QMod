using System.Reflection;
using HarmonyLib;

namespace FasterCraft
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var harmony = new Harmony("om.glibfire.graveyardkeeper.fastercraft.mod");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }
    }
}
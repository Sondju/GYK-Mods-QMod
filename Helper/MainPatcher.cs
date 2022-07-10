using System.IO;
using HarmonyLib;
using System.Linq;
using System.Reflection;

namespace Helper
{
    public class MainPatcher
    {
        private const string DisablePath = "./QMods/disable";
        private static bool _disableMods;
        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.QModHelper");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            _disableMods = File.Exists(DisablePath);
            if (_disableMods)
            {
                File.Delete(DisablePath);
            }
        }
        
        [HarmonyPatch(typeof(MainMenuGUI), nameof(MainMenuGUI.Open))]
        public static class MainMenuGuiOpenPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref MainMenuGUI __instance)
            {
                if (__instance == null) return;

                foreach (var comp in __instance.GetComponentsInChildren<UILabel>()
                             .Where(x => x.name.Contains("credits")))
                {
                    comp.text = "[F7B000]QMod Reloaded[-] by [F7B000]p1xel8ted[-]\r\ngame by: [F7B000]Lazy Bear Games[-]\r\npublished by: [F7B000]tinyBuild[-]";
                    comp.overflowMethod = UILabel.Overflow.ResizeFreely;
                    comp.multiLine = true;
                    comp.MakePixelPerfect();
                }

                foreach (var comp in __instance.GetComponentsInChildren<UILabel>()
                             .Where(x => x.name.Contains("ver txt")))
                {
                    if (_disableMods)
                    {
                        comp.text += ", [F7B000] QMod Reloaded[-] [F70000]Disabled[-]";
                    }
                    else
                    {
                        comp.text += ", [F7B000] QMod Reloaded[-] [2BFF00]Enabled[-]";
                    }
                    comp.overflowMethod = UILabel.Overflow.ResizeFreely;
                    comp.multiLine = true;
                    comp.MakePixelPerfect();

                }
            }
        }
    }
}
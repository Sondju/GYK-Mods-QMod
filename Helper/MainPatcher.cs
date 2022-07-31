using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Helper
{
    //public static class CrossModFields
    //{
    //    public static class GerrysJunkTrunk
    //    {
    //        public static bool UnlockedShippingBox { get; set; }
    //        public static bool ShippingBoxBuilt { get; set; }
    //        public static WorldGameObject ShippingBox { get; set; }

    //        public static ObjectCraftDefinition ShippingBoxOcd { get; set; }
    //    }
    //}

    public static class Tools
    {
        private static readonly string[] Quests =
        {
            "start",
            "get_out_from_house_tech",
            "get_out_from_house",
            "dig_graved_skull",
            "go_to_talk_with_donkey_first_time",
            "go_to_mortuary_after_skull_tech",
            "go_to_mortuary_after_skull",
            "on_skull_talk_autopsi",
            "go_to_graveyard_and_talk_with_skull",
            "ghost_come_after_1st_burial",
            "skull_talk_after_burial",
            "tools_from_grave_chest_taken_tech",
            "take_tools_from_grave_chest",
            "goto_tavern",
            "goto_tavern_tech",
            "goto_tavern_2",
            "player_repairs_sword_before",
            "player_repairs_sword",
    };

        internal static readonly List<string> LoadedMods = new();
        internal static bool IsNpcInteraction;

        public static bool IsNpc()
        {
            return IsNpcInteraction;
        }

        public static bool TutorialDone()
        {
            if (!MainGame.game_started) return false;
            var completed = false;
            foreach (var q in Quests)
            {
                completed = MainGame.me.save.quests.IsQuestSucced(q);
                if (!completed) break;
            }
            return !MainGame.me.save.IsInTutorial() && completed;
        }

        public static bool IsModLoaded(string mod)
        {
            return LoadedMods.Contains(mod);
        }

        public static void Log(string caller, string message, bool error = false)
        {
            Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);
            if (error)
            {
                Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
                Debug.LogError($"[{caller}][ERROR]: {message}");
                return;
            }
            Debug.LogError($"[{caller}]: {message}");
        }
    }

    public static class MainPatcher
    {
        private const string DisablePath = "./QMods/disable";
        private static bool _disableMods;

        private static void Log(string message, bool error = false)
        {
            Tools.Log("QModHelper", $"{message}", error);
        }

        public static void Patch()
        {
            try
            {
                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.QModHelper");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _disableMods = File.Exists(DisablePath);
                if (_disableMods)
                {
                    File.Delete(DisablePath);
                }
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        [HarmonyPatch(typeof(QuestSystem), "OnQuestSucceed", typeof(QuestState))]
        public static class QuestSystemOnQuestSucceedPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ref List<string> ____succed_quests)
            {
                //if (!Tools.TutorialDone()) return;
                foreach (var q in ____succed_quests)
                {
                    Log($"[QuestSucceed]: {q}");
                }
            }
        }

        [HarmonyPatch(typeof(SmartAudioEngine))]
        private static class SmartAudioEnginePatch
        {
            [HarmonyPatch(nameof(SmartAudioEngine.OnStartNPCInteraction))]
            [HarmonyPrefix]
            public static void OnStartNPCInteractionPrefix()
            {
                Tools.IsNpcInteraction = true;
            }

            [HarmonyPatch(nameof(SmartAudioEngine.OnEndNPCInteraction))]
            [HarmonyPrefix]
            public static void OnEndNPCInteractionPrefix()
            {
                Tools.IsNpcInteraction = false;
            }
        }

        [HarmonyPatch(typeof(MainMenuGUI), nameof(MainMenuGUI.Open))]
        public static class MainMenuGuiOpenPatch
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                try
                {
                    var mods = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => a.Location.ToLowerInvariant().Contains("qmods"));
                    //foreach (var m in mods)
                    //{
                    //    File.AppendAllText("./qmods/loaded.assemblie.txt", $"Location: {m.Location}, Name: {m.FullName}\n");
                    //}
                    Tools.LoadedMods.Clear();
                    foreach (var mod in mods)
                    {
                        var modInfo = FileVersionInfo.GetVersionInfo(mod.Location);
                        if (!string.IsNullOrEmpty(modInfo.Comments))
                        {
                            Tools.LoadedMods.Add(modInfo.Comments);
                        }
                    }
                }
                catch (Exception)
                {
                    //
                }
            }

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
                    comp.text = _disableMods ? $"[F7B000] QMod Reloaded[-] [F70000]Disabled[-] [F7B000](Helper v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor})[-]" : $"[F7B000] QMod Reloaded[-] [2BFF00]Enabled[-] [F7B000](Helper v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor})[-]";
                    comp.overflowMethod = UILabel.Overflow.ResizeFreely;
                    comp.multiLine = true;
                    comp.MakePixelPerfect();
                }
            }
        }
    }
}
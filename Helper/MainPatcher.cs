using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Helper
{
    [HarmonyPriority(1)]
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


        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(BaseGUI))]
        public static class BaseGuiPatches
        {
            [HarmonyPriority(1)]
            [HarmonyPostfix]
            [HarmonyPatch("Hide", typeof(bool))]
            public static void BaseGuiHidePostfix()
            {
                if (BaseGUI.all_guis_closed)
                {
                    Tools.SetAllInteractionsFalse();
                }
            }
        }

        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(GameSettings))]
        public static class GameSettingsPatches
        {
            [HarmonyPriority(1)]
            [HarmonyPostfix]
            [HarmonyPatch(nameof(GameSettings.ApplyLanguageChange))]
            public static void GameSettingsApplyLanguageChangePostfix()
            {
                CrossModFields.Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
                CrossModFields.Culture = CultureInfo.GetCultureInfo(CrossModFields.Lang);
                Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
            }
        }

        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(QuestSystem))]
        public static class QuestSystemPatches
        {
            [HarmonyPriority(1)]
            [HarmonyPostfix]
            [HarmonyPatch("OnQuestSucceed", typeof(QuestState))]
            public static void QuestSystemOnQuestSucceedPostfix(ref List<string> ____succed_quests)
            {
                foreach (var q in ____succed_quests)
                {
                    Log($"[QuestSucceed]: {q}");
                }
            }
        }
        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(TimeOfDay))]
        public static class TimeOfDayPatches
        {
            [HarmonyPriority(1)]
            [HarmonyPostfix]
            [HarmonyPatch(nameof(TimeOfDay.Update))]
            public static void TimeOfDayUpdatePostfix(TimeOfDay __instance)
            {
                CrossModFields.TimeOfDayFloat = __instance.GetTimeK();
                CrossModFields.ConfigReloadShown = false;
            }
        }

        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(SmartAudioEngine))]
        private static class SmartAudioEnginePatches
        {
            [HarmonyPriority(1)]
            [HarmonyPatch(nameof(SmartAudioEngine.OnStartNPCInteraction))]
            [HarmonyPrefix]
            public static void OnStartNPCInteractionPrefix(BalanceBaseObject npc)
            {
                if (npc.id.Contains("zombie")) return;
                CrossModFields.TalkingToNpc = true;
            }
            [HarmonyPriority(1)]
            [HarmonyPatch(nameof(SmartAudioEngine.OnEndNPCInteraction))]
            [HarmonyPrefix]
            public static void OnEndNPCInteractionPrefix()
            {
                CrossModFields.TalkingToNpc = false;
            }
        }

        //private static UILabel labelToMimic;
        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(MainMenuGUI))]
        public static class MainMenuGuiPatches
        {
            [HarmonyPriority(1)]
            [HarmonyPrefix]
            [HarmonyPatch(nameof(MainMenuGUI.Open))]
            public static void MainMenuGuiOpenPrefix()
            {
                try
                {
                    var mods = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => a.Location.ToLowerInvariant().Contains("qmods"));
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
            [HarmonyPriority(1)]
            [HarmonyPostfix]
            [HarmonyPatch(nameof(MainMenuGUI.Open))]
            public static void MainMenuGuiOpenPostfix(ref MainMenuGUI __instance)
            {
                if (__instance == null) return;

                foreach (var comp in __instance.GetComponentsInChildren<UILabel>()
                             .Where(x => x.name.Contains("credits")))
                {
                    comp.text =
                        "[F7B000]QMod Reloaded[-] by [F7B000]p1xel8ted[-]\r\ngame by: [F7B000]Lazy Bear Games[-]\r\npublished by: [F7B000]tinyBuild[-]";
                    comp.overflowMethod = UILabel.Overflow.ResizeFreely;
                    comp.multiLine = true;
                    comp.MakePixelPerfect();
                }

                foreach (var comp in __instance.GetComponentsInChildren<UILabel>()
                             .Where(x => x.name.Contains("ver txt")))
                {
                    comp.text = _disableMods
                        ? $"[F7B000] QMod Reloaded[-] [F70000]Disabled[-] [F7B000](Helper v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build})[-]"
                        : $"[F7B000] QMod Reloaded[-] [2BFF00]Enabled[-] [F7B000](Helper v{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build})[-]";
                    comp.overflowMethod = UILabel.Overflow.ResizeFreely;
                    comp.multiLine = true;
                    comp.MakePixelPerfect();
                    //labelToMimic = comp;
                }

                // var hudGameObject = GameObject.Find("UI Root/Main menu");
                // var layerNgui = LayerMask.NameToLayer("NGUI");

                // var go = new GameObject("(Game Object Name)");
                // var pos = new Vector3(380, 265, 0);

                // go.transform.SetParent(hudGameObject.transform);
                // go.transform.localPosition = pos;
                // go.transform.localScale = Vector3.one;
                // go.layer = layerNgui;

                // var label = go.AddComponent<UILabel>();

                // var assemblies = new List<string>
                // {
                //     "Test1",
                //     "Test2",
                //     "Test3",
                //     "Test4",
                //     "Test5",
                //     "Test6",
                //     "Test7"
                // };

                // var joinedAssemblies = string.Join("\n", Tools.LoadedMods.ToArray());

                // UITextStyles.Set(label, UITextStyles.TextStyle.Usual);
                ////label.fontStyle = FontStyle.Normal;
                //label.alignment = NGUIText.Alignment.Right;
                // label.text = joinedAssemblies;
                // label.depth = 12;
                // label.color = Color.white;
                // label.fontSize = 14;

                // label.maxLineCount = Tools.LoadedMods.Count();
                // label.MakePixelPerfect();

                // go.transform.localPosition = new Vector3(pos.x, pos.y - (label.printedSize.y / 2), 0.0f);
            }
        }
        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(VendorGUI))]
        public static class VendorGuiPatches
        {
            [HarmonyPriority(1)]
            [HarmonyPrefix]
            [HarmonyPatch(nameof(VendorGUI.Open), typeof(WorldGameObject), typeof(GJCommons.VoidDelegate))]
            public static void VendorGuiOpenPrefix()
            {
                if (!MainGame.game_started) return;
                CrossModFields.TalkingToNpc = true;
            }
            [HarmonyPriority(1)]
            [HarmonyPatch(nameof(VendorGUI.Hide), typeof(bool))]
            [HarmonyPrefix]
            public static void VendorGuiHidePrefix()
            {
                if (!MainGame.game_started) return;
                CrossModFields.TalkingToNpc = false;
            }
            [HarmonyPriority(1)]
            [HarmonyPatch(nameof(VendorGUI.OnClosePressed))]
            [HarmonyPrefix]
            public static void VendorGUIOnClosePressedPrefix()
            {
                if (!MainGame.game_started) return;
                CrossModFields.TalkingToNpc = false;
            }
        }
        [HarmonyPriority(1)]
        [HarmonyPatch(typeof(WorldGameObject))]
        public static class WorldGameObjectPatches
        {
            [HarmonyPriority(1)]
            [HarmonyPrefix]
            [HarmonyPatch(nameof(WorldGameObject.Interact))]
            public static void WorldGameObjectInteractPrefix(ref WorldGameObject __instance, ref WorldGameObject other_obj)
            {
                if (!MainGame.game_started || __instance == null) return;

                //Where's Ma Storage
                CrossModFields.PreviousWgoInteraction = CrossModFields.CurrentWgoInteraction;
                CrossModFields.CurrentWgoInteraction = __instance;
                CrossModFields.IsVendor = __instance.vendor != null;
                CrossModFields.IsCraft = other_obj.is_player && __instance.obj_def.interaction_type != ObjectDefinition.InteractionType.Chest && __instance.obj_def.has_craft;
                CrossModFields.IsChest = __instance.obj_def.interaction_type == ObjectDefinition.InteractionType.Chest;
                CrossModFields.IsBarman = __instance.obj_id.ToLowerInvariant().Contains("barman");
                CrossModFields.IsTavernCellar = __instance.obj_id.ToLowerInvariant().Contains("tavern_cellar");
                CrossModFields.IsRefugee = __instance.obj_id.ToLowerInvariant().Contains("refugee");
                CrossModFields.IsWritersTable = __instance.obj_id.ToLowerInvariant().Contains("writer");
               
                //Beam Me Up & Save Now
                CrossModFields.IsInDungeon = __instance.obj_id.ToLowerInvariant().Contains("dungeon_enter");
               // Log($"[InDungeon]: {CrossModFields.IsInDungeon}");

                //I Build Where I Want
                if (__instance.obj_def.interaction_type is not ObjectDefinition.InteractionType.None)
                {
                    CrossModFields.CraftAnywhere = false;
                }

                //Beam Me Up Gerry
                CrossModFields.TalkingToNpc = __instance.obj_def.IsNPC() && !__instance.obj_id.Contains("zombie");

               // Log($"[WorldGameObject.Interact]: Instance: {__instance.obj_id}, InstanceIsPlayer: {__instance.is_player},  Other: {other_obj.obj_id}, OtherIsPlayer: {other_obj.is_player}");
            }
        }
    }
}
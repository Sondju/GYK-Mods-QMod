using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Helper
{
    public static class Tools
    {

        public static void ShowMessage(
            string msg,
            Vector3 pos = new(),
            EffectBubblesManager.BubbleColor color = EffectBubblesManager.BubbleColor.Relation,
            float time = 3f,
            bool sayAsPlayer = false,
            SpeechBubbleGUI.SpeechBubbleType speechBubbleType = SpeechBubbleGUI.SpeechBubbleType.Think,
            SmartSpeechEngine.VoiceID voice = SmartSpeechEngine.VoiceID.None)
        {
            if (GJL.IsEastern())
            {
                MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                    SmartSpeechEngine.VoiceID.None, true);
            }
            else
            {

                if (sayAsPlayer)
                {
                    MainGame.me.player.Say(msg, null, false, speechBubbleType,
                        voice, true);
                }
                else
                {
                    var newPos = pos;
                    if (newPos == Vector3.zero)
                    {
                        newPos = MainGame.me.player.pos3;
                        newPos.y += 125f;
                    }

                    EffectBubblesManager.ShowImmediately(newPos, msg,
                        color,
                        true, time);
                }
            }
        }

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

        public static void SetAllInteractionsFalse()
        { 
            CrossModFields.IsVendor = false;
            CrossModFields.IsCraft = false;
            CrossModFields.IsChest = false;
            CrossModFields.IsBarman = false;
            CrossModFields.IsTavernCellar = false;
            CrossModFields.IsRefugee = false;
            CrossModFields.IsWritersTable = false;
        }
    }
}

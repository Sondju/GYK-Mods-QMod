using System;
using Harmony;
using System.Reflection;
using UnityEngine;

namespace ThoughtfulReminders
{
    public class MainPatcher
    {
        public static int PrevDayOfWeek;
        private static Config.Options _cfg;
        private static TimeOfDay.TimeOfDayEnum _timeOfDay;
        private static float _timeOfDayFloat;

        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.ThoughtfulReminders");
            val.PatchAll(Assembly.GetExecutingAssembly());
            _cfg = Config.GetOptions();
        }

        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.Update))]
        public static class TimeOfDayPatch
        {
            [HarmonyPostfix]
            public static void Postfix(TimeOfDay __instance)
            {
                _timeOfDay = __instance.time_of_day_enum;
                _timeOfDayFloat = __instance.GetTimeK();
                //if (Input.GetKeyDown(KeyCode.L))
                //{
                //    SayMessage($"Time: {_timeOfDayFloat}");
                //}
            }
        }

        public static void SayMessage(string msg)
        {
            if (_cfg.SpeechBubbles)
            {
                MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                    SmartSpeechEngine.VoiceID.None, true);
            }
            else
            {
                EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, msg,
                    EffectBubblesManager.BubbleColor.White,
                    true, 4f);
            }

        }


        [HarmonyPatch(typeof(MainGame))]
        [HarmonyPatch(nameof(MainGame.Update))]
        public static class ModPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                var newDayOfWeek = MainGame.me.save.day_of_week;
                if (MainGame.me.player.is_dead)
                {
                    return;
                }
                if (!Application.isFocused)
                {
                    return;
                }
                if (PrevDayOfWeek == newDayOfWeek)
                {
                    return;
                }
                //if (_timeOfDay != TimeOfDay.TimeOfDayEnum.Day)
                //{
                //    return;
                //}
                if (_timeOfDayFloat is <= 0.22f or >= 0.25f) return;
                if (_cfg.DaysOnly)
                {
                    switch (newDayOfWeek)
                    {
                        case 0: //day of Sloth
                            SayMessage("Day of Sloth...");
                            break;
                        case 1: //day of Pride
                            SayMessage("Day of Pride...");
                            break;
                        case 2: //day of Lust
                            SayMessage("Day of Lust...");
                            break;
                        case 3: //day of Gluttony
                            SayMessage("Day of Gluttony...");
                            break;
                        case 4: //day of Envy
                            SayMessage("Day of Envy...");
                            break;
                        case 5: //day of Wrath
                            SayMessage("Day of Wrath...");
                            break;
                        default:
                            SayMessage("Day of ...");
                            break;
                    }
                }
                else
                {
                    switch (newDayOfWeek)
                    {
                        case 0: //day of Sloth
                            SayMessage("Day of Sloth...wonder what the astrologer is up to...");
                            break;
                        case 1: //day of Pride
                            SayMessage(MainGame.me.save.unlocked_perks.Contains("p_preacher")
                                ? "Day of Pride...can't forget my sermon today..."
                                : "Day of Pride...might go see the bishop...");
                            break;
                        case 2: //day of Lust
                            SayMessage("Day of Lust...mmm Ms. Charm...");
                            break;
                        case 3: //day of Gluttony
                            SayMessage("Day of Gluttony...should see the merchant today...");
                            break;
                        case 4: //day of Envy
                            SayMessage("Day of Envy ...");
                            break;
                        case 5: //day of Wrath
                            SayMessage("Day of Wrath...wonder if there are any witches today...");
                            break;
                        default:
                            SayMessage("Day of ...");
                            break;
                    }
                }

                PrevDayOfWeek = newDayOfWeek;
            }
        }
    }
}
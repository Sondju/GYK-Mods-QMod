using System.Reflection;
using UnityEngine;
using System.Threading;
using System.Globalization;
using HarmonyLib;
using ThoughtfulReminders.lang;

namespace ThoughtfulReminders
{
    public class MainPatcher
    {
        public static int PrevDayOfWeek;
        private static Config.Options _cfg;
        private static float _timeOfDayFloat;

        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.ThoughtfulReminders");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);

            _cfg = Config.GetOptions();
        }
        
        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.Update))]
        public static class TimeOfDayPatch
        {
            [HarmonyPostfix]
            public static void Postfix(TimeOfDay __instance)
            {
                _timeOfDayFloat = __instance.GetTimeK();
            }
        }

        public static void SayMessage(string msg)
        {
            var lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
            if (_cfg.SpeechBubbles)
            {
                MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                    SmartSpeechEngine.VoiceID.None, true);
            }
            else
            {
                //game doesn't appear to support these languages fonts in the effects bubbles aka floaty text
                if (lang.Contains("ko") || lang.Contains("ja") || lang.Contains("zh"))
                {

                    MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                        SmartSpeechEngine.VoiceID.None, true);
                }
                else
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, msg,
                        EffectBubblesManager.BubbleColor.Red,
                        true, 4f);
                }
            }

        }


        [HarmonyPatch(typeof(MainGame))]
        [HarmonyPatch(nameof(MainGame.Update))]
        public static class ModPatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {

                if (!MainGame.game_started) return;
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
                if (_timeOfDayFloat is <= 0.22f or >= 0.25f) return;

                if (_cfg.DaysOnly)
                {
                    var lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
                    switch (newDayOfWeek)
                    {
                        case 0: //day of Sloth
                            SayMessage(strings.dSloth);
                            break;
                        case 1: //day of Pride
                            SayMessage(strings.dPride);
                            break;
                        case 2: //day of Lust
                            SayMessage(strings.dLust);
                            break;
                        case 3: //day of Gluttony
                            SayMessage(strings.dGluttony);
                            break;
                        case 4: //day of Envy
                            SayMessage(strings.dEnvy);
                            break;
                        case 5: //day of Wrath
                            SayMessage(strings.dWrath);
                            break;
                        default:
                            SayMessage(strings._default);
                            break;
                    }
                }
                else
                {
                    var lang = GameSettings.me.language.Replace('_', '-').ToLower().Trim();
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(lang);
                    switch (newDayOfWeek)
                    {
                        case 0: //day of Sloth
                            SayMessage(strings.dhSloth);
                            break;
                        case 1: //day of Pride
                            SayMessage(MainGame.me.save.unlocked_perks.Contains("p_preacher")
                                ? strings.dhPrideSermon
                                : strings.dhPride);
                            break;
                        case 2: //day of Lust
                            SayMessage(strings.dhLust);
                            break;
                        case 3: //day of Gluttony
                            SayMessage(strings.dhGluttony);
                            break;
                        case 4: //day of Envy
                            SayMessage(strings.dhEnvy);
                            break;
                        case 5: //day of Wrath
                            SayMessage(strings.dhWrath);
                            break;
                        default:
                            SayMessage(strings._default);
                            break;
                    }
                }
                PrevDayOfWeek = newDayOfWeek;
            }
        }
    }
}
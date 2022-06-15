using System.Globalization;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using ThoughtfulReminders.lang;
using UnityEngine;

namespace ThoughtfulReminders;

public class MainPatcher
{
    private static int _prevDayOfWeek;
    private static Config.Options _cfg;
    private static float _timeOfDayFloat;
    private static string Lang { get; set; }

    public static void Patch()
    {
        var harmony = new Harmony("p1xel8ted.GraveyardKeeper.ThoughtfulReminders");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        _cfg = Config.GetOptions();

        Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
    }

    private static void SayMessage(string msg)
    {
        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        if (_cfg.SpeechBubbles)
        {
            MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                SmartSpeechEngine.VoiceID.None, true);
        }
        else
        {
            //game doesn't appear to support these languages fonts in the effects bubbles aka floaty text
            if (Lang.Contains("ko") || Lang.Contains("ja") || Lang.Contains("zh"))
                MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                    SmartSpeechEngine.VoiceID.None, true);
            else
                EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, msg,
                    EffectBubblesManager.BubbleColor.Red,
                    true, 4f);
        }
    }


    [HarmonyPatch(typeof(InGameMenuGUI), nameof(InGameMenuGUI.OnClosePressed))]
    public static class InGameMenuGuiOnClosePressedPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        }
    }

    [HarmonyPatch(typeof(TimeOfDay), nameof(TimeOfDay.Update))]
    public static class TimeOfDayUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(TimeOfDay __instance)
        {
            _timeOfDayFloat = __instance.GetTimeK();
        }
    }


    [HarmonyPatch(typeof(MainGame), nameof(MainGame.Update))]
    public static class MainGameUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!MainGame.game_started) return;
            var newDayOfWeek = MainGame.me.save.day_of_week;
            if (MainGame.me.player.is_dead) return;

            if (!Application.isFocused) return;

            if (_prevDayOfWeek == newDayOfWeek) return;

            if (_timeOfDayFloat is <= 0.22f or >= 0.25f) return;

            if (_cfg.DaysOnly)
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
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
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
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

            _prevDayOfWeek = newDayOfWeek;
        }
    }
}
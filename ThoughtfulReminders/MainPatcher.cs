using HarmonyLib;
using Helper;
using System;
using System.Reflection;
using System.Threading;
using ThoughtfulReminders.lang;
using UnityEngine;

namespace ThoughtfulReminders;

public class MainPatcher
{
    private static int _prevDayOfWeek;
    private static Config.Options _cfg;

    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.ThoughtfulReminders");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            _cfg = Config.GetOptions();
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("ThoughtfulReminders", $"{message}", error);
    }

    private static void SayMessage(string msg)
    {
        Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
        if (_cfg.SpeechBubbles)
        {
            Tools.ShowMessage(msg, sayAsPlayer:true);
        }
        else
        {
            Tools.ShowMessage(msg, sayAsPlayer: false, color:EffectBubblesManager.BubbleColor.Red, time:4f);
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

            if (CrossModFields.TimeOfDayFloat is <= 0.22f or >= 0.25f) return;

            if (_cfg.DaysOnly)
            {
                Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
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
                Thread.CurrentThread.CurrentUICulture = CrossModFields.Culture;
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
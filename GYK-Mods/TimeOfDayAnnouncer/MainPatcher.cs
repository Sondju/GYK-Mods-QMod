using System;
using Harmony;
using System.Reflection;

namespace TimeOfDayAnnouncer
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var val = HarmonyInstance.Create($"p1xel8ted.graveyardkeeper.TimeOfDayAnnouncer");
            val.PatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(TimeOfDay))]
        [HarmonyPatch(nameof(TimeOfDay.Update))]
        public static class ModPatch
        {
            [HarmonyPrefix]
            public static void Prefix(TimeOfDay __instance, ref float ____prev_time_of_day)
            {
                var timeOfDay = __instance.time_of_day;
                var prevTimeOfDay = ____prev_time_of_day;

                if (!(Math.Abs(timeOfDay - prevTimeOfDay) > 0.05)) return;
                if (Math.Abs(MainGame.me.save.day - TimeOfDay.MORNING) < 0.05)
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Morning!",
                        EffectBubblesManager.BubbleColor.Red, true, 2f);
                }
                if (Math.Abs(MainGame.me.save.day - TimeOfDay.DAYTIME) < 0.05)
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Day!",
                        EffectBubblesManager.BubbleColor.Red, true, 2f);
                }
                if (Math.Abs(MainGame.me.save.day - TimeOfDay.EVENING) < 0.05)
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Evening!",
                        EffectBubblesManager.BubbleColor.Red, true, 2f);
                }
                if (Math.Abs(MainGame.me.save.day - TimeOfDay.NIGHT) < 0.05)
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Night!",
                        EffectBubblesManager.BubbleColor.Red, true, 2f);
                }
            }

            [HarmonyPostfix]
            public static void Postfix(TimeOfDay __instance, ref float ____prev_time_of_day)
            {
                var timeOfDay = __instance.time_of_day;
                var prevTimeOfDay = ____prev_time_of_day;

                if (!(Math.Abs(timeOfDay - prevTimeOfDay) > 0.05)) return;
                if (Math.Abs(MainGame.me.save.day - TimeOfDay.MORNING) < 0.05)
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Morning!",
                        EffectBubblesManager.BubbleColor.Red, true, 2f);
                }
                if (Math.Abs(MainGame.me.save.day - TimeOfDay.DAYTIME) < 0.05)
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Day!",
                        EffectBubblesManager.BubbleColor.Red, true, 2f);
                }
                if (Math.Abs(MainGame.me.save.day - TimeOfDay.EVENING) < 0.05)
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Evening!",
                        EffectBubblesManager.BubbleColor.Red, true, 2f);
                }
                if (Math.Abs(MainGame.me.save.day - TimeOfDay.NIGHT) < 0.05)
                {
                    EffectBubblesManager.ShowImmediately(MainGame.me.player_pos, "Night!",
                        EffectBubblesManager.BubbleColor.Red, true, 2f);
                }
            }
        }
    }
}
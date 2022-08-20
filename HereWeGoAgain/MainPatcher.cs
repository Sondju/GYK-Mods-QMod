using HarmonyLib;
using Helper;
using System;
using System.Reflection;
using UnityEngine;

namespace HereWeGoAgain
{
    public class MainPatcher
    {
        public static void Patch()
        {
            try
            {
                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.HereWeGoAgain");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
            }
        }

        private static void Log(string message, bool error = false)
        {
            Tools.Log("HereWeGoAgain", $"{message}", error);
        }

        private static int _count;

        [HarmonyPatch(typeof(QuestSystem), nameof(QuestSystem.StartQuest))]
        public static class QuestSystemStartQuest
        {
            [HarmonyPostfix]
            public static void Postfix(QuestSystem __instance, QuestDefinition quest)
            {
                if (quest is not { id: "start" }) return;
                if (_count >= Tools.Quests.Length) return;
                GJTimer.AddTimer(5f, delegate
                {
                    foreach (var q in Tools.Quests)
                    {
                        var questToStart = GameBalance.me.GetData<QuestDefinition>(q);
                        __instance.StartQuest(questToStart);
                        __instance.ForceQuestEnd(q, true);
                        _count++;
                    }

                    MainGame.me.player.PlaceAtPos(new Vector3(15944.8f, -2081.9f, -430.4f));
                });
           
            }
        }
    }
}
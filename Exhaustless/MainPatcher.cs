using Exhaustless.lang;
using HarmonyLib;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using Helper;
using UnityEngine;

namespace Exhaustless;

[HarmonyBefore("p1xel8ted.GraveyardKeeper.QueueEverything")]
public static class MainPatcher
{
    private static readonly ItemDefinition.ItemType[] ToolItems =
    {
        ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
        ItemDefinition.ItemType.Pickaxe, ItemDefinition.ItemType.FishingRod, ItemDefinition.ItemType.BodyArmor,
        ItemDefinition.ItemType.HeadArmor, ItemDefinition.ItemType.Sword,
    };

    private static Config.Options _cfg;
    private static string Lang { get; set; }

    public static void Patch()
    {
        try
        {
            _cfg = Config.GetOptions();
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.exhaust-less");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("Exhaustless", $"{message}", error);
    }

    [HarmonyPatch(typeof(BuffsLogics), nameof(BuffsLogics.AddBuff))]
    public static class BuffsLogicsAddBuffPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref string buff_id)
        {
            if (!_cfg.YawnMessage) return;
            if (buff_id.Equals("buff_tired"))
                MainGame.me.player.Say(strings.Yawn, null, null,
                    SpeechBubbleGUI.SpeechBubbleType.Think, SmartSpeechEngine.VoiceID.None, true);
        }
    }

    [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.TrySpendPlayerGratitudePoints))]
    public static class CraftComponentTrySpendPlayerGratitudePointsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref float value)
        {
            if (_cfg.SpendHalfGratitude) value /= 2f;
        }
    }

    [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
    public static class GameBalanceLoadGameBalancePatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (!_cfg.MakeToolsLastLonger) return;
            foreach (var itemDef in GameBalance.me.items_data.Where(a => ToolItems.Contains(a.type)))
            {
                if (itemDef.durability_decrease_on_use)
                {
                    itemDef.durability_decrease_on_use_speed = 0.005f;
                }
            }
        }
    }

    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.ApplyLanguageChange))]
    public static class GameSettingsApplyLanguageChange
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            Lang = GameSettings.me.language.Replace('_', '-').ToLower(CultureInfo.InvariantCulture).Trim();
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Lang);
        }
    }

    [HarmonyPatch(typeof(MainGame), nameof(MainGame.OnEquippedToolBroken))]
    public static class MainGameOnEquippedToolBrokenPatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!_cfg.AutoEquipNewTool) return;
            var equippedTool = MainGame.me.player.GetEquippedTool();
            var save = MainGame.me.save;
            var playerInv = save.GetSavedPlayerInventory();

            foreach (var item in playerInv.inventory.Where(item =>
                         item.definition.type == equippedTool.definition.type))
            {
                if (item.durability_state is not (Item.DurabilityState.Full or Item.DurabilityState.Used))
                    continue;
                MainGame.me.player.EquipItem(item, -1, playerInv.is_bag ? playerInv : null);
                MainGame.me.player.Say(
                    $"{strings.LuckyHadAnotherPartOne} {item.definition.GetItemName()} {strings.LuckyHadAnotherPartTwo}",
                    null, false,
                    SpeechBubbleGUI.SpeechBubbleType.Think,
                    SmartSpeechEngine.VoiceID.None, true);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerComponent), nameof(PlayerComponent.SpendSanity))]
    public static class PlayerComponentSpendSanityPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref float need_sanity)
        {
            if (_cfg.SpendHalfSanity) need_sanity /= 2f;
        }
    }

    [HarmonyPatch(typeof(PlayerComponent), nameof(PlayerComponent.TrySpendEnergy))]
    public static class PlayerComponentTrySpendEnergyPatch
    {
        [HarmonyPrefix]
        public static void Prefix(ref float need_energy)
        {
            if (_cfg.SpendHalfEnergy) need_energy /= 2f;
        }
    }

    [HarmonyPatch(typeof(SleepGUI), nameof(SleepGUI.Update))]
    public static class SleepGuiUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!_cfg.SpeedUpSleep) return;
            MainGame.me.player.energy += 0.25f;
            MainGame.me.player.hp += 0.25f;
        }
    }

    [HarmonyPatch(typeof(WaitingGUI), nameof(WaitingGUI.Update))]
    public static class WaitingGuiUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(WaitingGUI __instance)
        {
            if (!_cfg.AutoWakeFromMeditation) return;
            var save = MainGame.me.save;
            if (MainGame.me.player.energy.EqualsOrMore(save.max_hp) &&
                MainGame.me.player.hp.EqualsOrMore(save.max_energy))
                typeof(WaitingGUI).GetMethod("StopWaiting", AccessTools.all)
                    ?.Invoke(__instance, null);
        }

        [HarmonyPrefix]
        public static void Prefix()
        {
            if (!_cfg.SpeedUpMeditation) return;
            MainGame.me.player.energy += 0.25f;
            MainGame.me.player.hp += 0.25f;
        }
    }

    [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.EquipItem))]
    public static class WorldGameObjectEquipItemPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref Item item)
        {
            if (!_cfg.MakeToolsLastLonger) return;
            if (!ToolItems.Contains(item.definition.type)) return;
            if (item.definition.durability_decrease_on_use)
            {
                item.definition.durability_decrease_on_use_speed = 0.005f;
            }
        }
    }

    [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.GetParam))]
    public static class WorldGameObjectGetParamPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref WorldGameObject __instance, ref string param_name, ref Item ____data,
            ref float __result)
        {
            if (!param_name.Contains("tiredness")) return;
            var tiredness = ____data.GetParam("tiredness");

            var newTirednessLimit = (float)_cfg.EnergySpendBeforeSleepDebuff;
            __result = tiredness < newTirednessLimit ? 250 : 350;
        }
    }
}
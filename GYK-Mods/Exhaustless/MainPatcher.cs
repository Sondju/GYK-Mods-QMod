using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using UnityEngine;

namespace Exhaustless
{
    public class MainPatcher
    {
        private static Config.Options _cfg;

        public static void Patch()
        {
            try
            {
                _cfg = Config.GetOptions();
                var val = HarmonyInstance.Create("p1xel8ted.graveyardkeeper.exhaust-less");
                val.PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception ex)
            {
                File.AppendAllText("./qmods/dura.txt", $"{ex.Message} - {ex.Source} - {ex.StackTrace}\n");
            }
        }


        [HarmonyPatch(typeof(PlayerComponent))]
        [HarmonyPatch(nameof(PlayerComponent.TrySpendEnergy))]
        public static class PatchTrySpendEnergy
        {
            [HarmonyPrefix]
            public static void Prefix(ref float need_energy)
            {
                if (_cfg.SpendHalfEnergy)
                {
                    need_energy /= 2f;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerComponent))]
        [HarmonyPatch(nameof(PlayerComponent.SpendSanity))]
        public static class PatchSpendSanity
        {
            [HarmonyPrefix]
            public static void Prefix(ref float need_sanity)
            {
                if (_cfg.SpendHalfSanity)
                {
                    need_sanity /= 2f;
                }
            }
        }


        [HarmonyPatch(typeof(WaitingGUI))]
        [HarmonyPatch(nameof(WaitingGUI.Update))]
        public static class PatchWaiting
        {

            [HarmonyPrefix]
            public static void Prefix()
            {
                if (_cfg.SpeedUpMeditation)
                {
                    MainGame.me.player.energy += 0.25f;
                    MainGame.me.player.hp += 0.25f;
                }
            }

            [HarmonyPostfix]
            public static void Postfix(WaitingGUI __instance)
            {
                if (_cfg.AutoWakeFromMeditation)
                {
                    var save = MainGame.me.save;
                    if (MainGame.me.player.energy.EqualsOrMore(save.max_hp) &&
                        MainGame.me.player.hp.EqualsOrMore(save.max_energy))
                    {
                        typeof(WaitingGUI).GetMethod("StopWaiting", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?.Invoke(__instance, null);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(WorldGameObject))]
        [HarmonyPatch(nameof(WorldGameObject.EquipItem))]
        public static class PatchToolDurabilitySpeed2
        {
            [HarmonyPostfix]
            public static void Postfix(ref Item item)
            {
                if (item.definition.durability_decrease_on_use)
                {
                    item.definition.durability_decrease_on_use_speed = 0.005f;
                }
            }
        }


        [HarmonyPatch(typeof(MainGame))]
        [HarmonyPatch(nameof(MainGame.OnEquippedToolBroken))]
        public static class PatchBrokenTool
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                var equippedTool = MainGame.me.player.GetEquippedTool();
                var save = MainGame.me.save;
                var playerInv = save.GetSavedPlayerInventory();
                foreach (var item in playerInv.inventory.Where(item => item.definition.id.Equals(equippedTool.definition.id)))
                {
                    if (item.durability_state is not (Item.DurabilityState.Full or Item.DurabilityState.Used)) continue;
                    MainGame.me.player.EquipItem(item, -1, playerInv.is_bag ? playerInv : null);
                    MainGame.me.player.Say($"Lucky I had another {item.definition.GetItemName(true).ToLower()} on me!", null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                        SmartSpeechEngine.VoiceID.None, true);
                }
            }
        }


        [HarmonyPatch(typeof(SleepGUI))]
        [HarmonyPatch(nameof(SleepGUI.Update))]
        public static class PatchSleeping
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (_cfg.SpeedUpSleep)
                {
                    MainGame.me.player.energy += 0.25f;
                    MainGame.me.player.hp += 0.25f;
                }
            }
        }

        [HarmonyPatch(typeof(BuffsLogics))]
        [HarmonyPatch(nameof(BuffsLogics.AddBuff))]
        public static class PatchBuff
        {
            [HarmonyPrefix]
            public static void Prefix(ref string buff_id)
            {
                if (_cfg.YawnMessage)
                {
                    if (buff_id.Equals("buff_tired"))
                    {
                        MainGame.me.player.Say("Yawn...*rubs eyes*..", null, null,
                            SpeechBubbleGUI.SpeechBubbleType.Think, SmartSpeechEngine.VoiceID.None, true, null);
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(InventoryGUI))]
        [HarmonyPatch(nameof(InventoryGUI.OnItemOver))]
        public static class PatchCantDestroy
        {
            [HarmonyPrefix]
            public static void Prefix(InventoryGUI __instance)
            {
                string message = string.Empty;
                if (_cfg.AllowHandToolDestroy)
                {
                    var itemDef = __instance.selected_item.definition;
                    ItemDefinition.ItemType[] items =
                    {
                        ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
                        ItemDefinition.ItemType.Pickaxe
                    };
                    if (items.Contains(itemDef.type))
                    {
                        File.AppendAllText("./qmods/dura.txt", $"Item: {__instance.selected_item.GetItemName()}, Decrease: {__instance.selected_item.definition.durability_decrease_on_use_speed.ToString()}\n");
                        itemDef.player_cant_throw_out = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameGUI))]
        [HarmonyPatch(nameof(GameGUI.Open))]
        public static class QuietMusicPatch1
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (_cfg.QuietMusicInGui)
                {
                    SmartAudioEngine.me.SetDullMusicMode(true);
                }
            }
        }

        [HarmonyPatch(typeof(GameGUI))]
        [HarmonyPatch(nameof(GameGUI.Hide))]
        public static class QuietMusicPatch2
        {
            [HarmonyPrefix]
            public static void Prefix()
            {
                if (_cfg.QuietMusicInGui)
                {
                    SmartAudioEngine.me.SetDullMusicMode(false);
                }
            }
        }


        [HarmonyPatch(typeof(InventoryPanelGUI), "DoOpening")]
        public static class ShowInvOnly
        {
            [HarmonyPrefix]
            private static void Prefix(InventoryPanelGUI __instance, ref MultiInventory multi_inventory)
            {
                if (_cfg.DontShowEmptyRowsInInventory)
                {
                    __instance.dont_show_empty_rows = true;
                }

                if (_cfg.ShowOnlyPersonalInventory)
                {
                    var multiInventory = new MultiInventory(null, null, null);
                    var num = 0;
                    foreach (var inventory in multi_inventory.all)
                    {
                        multiInventory.AddInventory(inventory, -1);
                        num++;
                        if (num == 1)
                        {
                            break;
                        }
                    }

                    multi_inventory = multiInventory;
                }
            }
        }
    }
}
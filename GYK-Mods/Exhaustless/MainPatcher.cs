using System;
using System.Linq;
using System.Reflection;
using Exhaustless.lang;
using HarmonyLib;

namespace Exhaustless
{
    public class MainPatcher
    {
        private static Config.Options _cfg;
        public static ItemDefinition.ItemType[] Items =
        {
            ItemDefinition.ItemType.Axe, ItemDefinition.ItemType.Shovel, ItemDefinition.ItemType.Hammer,
            ItemDefinition.ItemType.Pickaxe, ItemDefinition.ItemType.FishingRod, ItemDefinition.ItemType.BodyArmor,
            ItemDefinition.ItemType.HeadArmor, ItemDefinition.ItemType.Sword
        };
        public static void Patch()
        {
            try
            {
                _cfg = Config.GetOptions();
                var harmony = new Harmony("p1xel8ted.GraveyardKeeper.exhaust-less");
                var assembly = Assembly.GetExecutingAssembly();
                harmony.PatchAll(assembly);
            }
            catch (Exception)
            {
                //  File.AppendAllText("./qmods/dura.txt", $"{ex.Message} - {ex.Source} - {ex.StackTrace}\n");
            }
        }

        [HarmonyPatch(typeof(CraftComponent))]
        [HarmonyPatch(nameof(CraftComponent.TrySpendPlayerGratitudePoints))]
        public static class CraftComponentTrySpendPlayerGratitudePointsPatch
        {
            [HarmonyPrefix]
            public static void Prefix(ref float value)
            {
                if (_cfg.SpendHalfGratitude)
                {
                    value /= 2f;
                }
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
                if (!_cfg.SpeedUpMeditation) return;
                MainGame.me.player.energy += 0.25f;
                MainGame.me.player.hp += 0.25f;
            }

            [HarmonyPostfix]
            public static void Postfix(WaitingGUI __instance)
            {
                if (!_cfg.AutoWakeFromMeditation) return;
                var save = MainGame.me.save;
                if (MainGame.me.player.energy.EqualsOrMore(save.max_hp) &&
                    MainGame.me.player.hp.EqualsOrMore(save.max_energy))
                {
                    typeof(WaitingGUI).GetMethod("StopWaiting", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.Invoke(__instance, null);
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
                if (!_cfg.MakeToolsLastLonger) return;
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
                if (!_cfg.AutoEquipNewTool) return;
                var equippedTool = MainGame.me.player.GetEquippedTool();
                var save = MainGame.me.save;
                var playerInv = save.GetSavedPlayerInventory();
                foreach (var item in playerInv.inventory.Where(item =>
                             item.definition.id.Equals(equippedTool.definition.id)))
                {
                    if (item.durability_state is not (Item.DurabilityState.Full or Item.DurabilityState.Used))
                        continue;
                    MainGame.me.player.EquipItem(item, -1, playerInv.is_bag ? playerInv : null);
                    MainGame.me.player.Say(
                        $"{strings.LuckyHadAnotherPartOne} {item.definition.GetItemName().ToLower()} {strings.LuckyHadAnotherPartTwo}", null, false,
                        SpeechBubbleGUI.SpeechBubbleType.Think,
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
                if (!_cfg.SpeedUpSleep) return;
                MainGame.me.player.energy += 0.25f;
                MainGame.me.player.hp += 0.25f;
            }
        }

        [HarmonyPatch(typeof(BuffsLogics))]
        [HarmonyPatch(nameof(BuffsLogics.AddBuff))]
        public static class PatchBuff
        {
            [HarmonyPrefix]
            public static void Prefix(ref string buff_id)
            {
                if (!_cfg.YawnMessage) return;
                if (buff_id.Equals("buff_tired"))
                {
                    MainGame.me.player.Say(strings.Yawn, null, null,
                        SpeechBubbleGUI.SpeechBubbleType.Think, SmartSpeechEngine.VoiceID.None, true);
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
                if (!_cfg.AllowHandToolDestroy) return;
                if (__instance == null) return;
                var itemDef = __instance.selected_item?.definition;
                if (itemDef == null) return;
                if (Items.Contains(itemDef.type))
                {
                    itemDef.player_cant_throw_out = false;
                }
            }
        }

        //patch tools to be stack-able
        [HarmonyPatch(typeof(GameBalance), "LoadGameBalance")]
        public static class PatchTools
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                foreach (var itemDefinition in GameBalance.me.items_data.Where(itemDefinition => Items.Contains(itemDefinition.type)).Where(itemDefinition => itemDefinition.stack_count < _cfg.ToolStackSize))
                {
                    itemDefinition.stack_count = _cfg.ToolStackSize;
                }
            }
        }

        //makes the racks and the barman inventory larger
        [HarmonyPatch(typeof(WorldGameObject), "InitNewObject")]
        internal class PatchTavernInventorySize
        {
            [HarmonyPostfix]
            private static void Postfix(WorldGameObject __instance)
            {
                if (__instance.obj_id is "npc_tavern_barman" or "tavern_cellar_rack" or "tavern_cellar_rack_1"
                    or "tavern_cellar_rack_2" or "tavern_cellar_rack_3" or "tavern_cellar_rack_4"
                    or "tavern_cellar_rack_5")
                {
                    __instance.data.SetInventorySize(__instance.obj_def.inventory_size + _cfg.TavernInvIncrease);
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
                    SmartAudioEngine.me.SetDullMusicMode();
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

                if (!_cfg.ShowOnlyPersonalInventory) return;
                var multiInventory = new MultiInventory();
                var num = 0;
                foreach (var inventory in multi_inventory.all)
                {
                    multiInventory.AddInventory(inventory);
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
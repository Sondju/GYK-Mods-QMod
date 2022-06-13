using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace AddStraightToTable
{
    public class MainPatcher
    {
        public static void Patch()
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.AddStraightToTable");
            var assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);
        }

        [HarmonyPatch(typeof(AutopsyGUI), "OnBodyItemPress")]
        public static class PatchTable
        {
            [HarmonyPrefix]
            public static bool Prefix()
            {
                return false;
            }

            [HarmonyPostfix]
            public static void Postfix(ref AutopsyGUI __instance, BaseItemCellGUI item_gui, ref Item ____body,
                ref WorldGameObject ____autopti_obj, ref Inventory ____parts_inventory)
            {
                if (item_gui.item.id == "insertion_button_pseudoitem")
                {
                    var obj = MainGame.me.player;
                    if (GlobalCraftControlGUI.is_global_control_active && ____autopti_obj != null)
                    {
                        obj = ____autopti_obj;
                    }

                    var inventory = ____parts_inventory;
                    var instance = __instance;
                    GUIElements.me.resource_picker.Open(obj, delegate (Item item, InventoryWidget _)
                    {
                        if (item == null || item.IsEmpty())
                        {
                            return InventoryWidget.ItemFilterResult.Hide;
                        }

                        if (item.definition.type != ItemDefinition.ItemType.BodyUniversalPart)
                        {
                            return InventoryWidget.ItemFilterResult.Inactive;
                        }

                        var text = item.id;
                        if (text.Contains(":"))
                        {
                            text = text.Split(':')[0];
                        }

                        text = text.Replace("_dark", "");
                        if (inventory.data.inventory.Any(item2 =>
                                item2 != null && !item2.IsEmpty() && item2.id.StartsWith(text)))
                        {
                            return InventoryWidget.ItemFilterResult.Inactive;
                        }

                        return instance.GetInsertCraftDefinition(item) == null ? InventoryWidget.ItemFilterResult.Inactive : InventoryWidget.ItemFilterResult.Active;

                    },
                        __instance.OnItemForInsertionPicked);
                    return;
                }

                var craftDefinition = (CraftDefinition)typeof(AutopsyGUI)
                    .GetMethod("GetExtractCraftDefinition", AccessTools.all)
                    ?.Invoke(__instance, new object[]
                    {
                        item_gui.item
                    });


                if (craftDefinition == null)
                {
                    return;
                }

                typeof(AutopsyGUI).GetMethod("RemoveBodyPartFromBody", AccessTools.all)
                    ?.Invoke(__instance, new object[]
                    {
                        ____body,
                        item_gui.item
                    });

                ____autopti_obj.components.craft.CraftAsPlayer(craftDefinition, item_gui.item);
                {
                    __instance.Hide();
                }
            }
        }
    }
}


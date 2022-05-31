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
            public static void Postfix(AutopsyGUI __instance, BaseItemCellGUI item_gui, Item ____body,
                WorldGameObject ____autopti_obj, Inventory ____parts_inventory)
            {
                if (item_gui.item.id == "insertion_button_pseudoitem")
                {
                    var obj = MainGame.me.player;
                    if (GlobalCraftControlGUI.is_global_control_active && ____autopti_obj != null)
                    {
                        obj = ____autopti_obj;
                    }

                    GUIElements.me.resource_picker.Open(obj, delegate(Item item, InventoryWidget _)
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
                            if (____parts_inventory.data.inventory.Any(item2 => item2 != null && !item2.IsEmpty() && item2.id.StartsWith(text)))
                            {
                                return InventoryWidget.ItemFilterResult.Inactive;
                            }

                            return GetInsertCraftDefinition(item, ____autopti_obj) == null ? InventoryWidget.ItemFilterResult.Inactive : InventoryWidget.ItemFilterResult.Active;
                        },
                        __instance.OnItemForInsertionPicked);
                    return;
                }

                var craftDefinition = GetExtractCraftDefinition(item_gui.item, ____autopti_obj);
                if (craftDefinition == null)
                {
                    return;
                }

                RemoveBodyPartFromBody(____body, item_gui.item);
                ____autopti_obj.components.craft.CraftAsPlayer(craftDefinition, item_gui.item);
                __instance.Hide();
            }
        }

        public static void RemoveBodyPartFromBody(Item body, Item item)
        {
            foreach (var item2 in body.inventory)
            {
                if (item2.id == item.id)
                {
                    body.RemoveItem(item, 1);
                    break;
                }

                using var enumerator2 = item2.inventory.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    if (enumerator2.Current?.id != item.id) continue;
                    item2.RemoveItem(item, 1);
                    return;
                }
            }
        }

        public static CraftDefinition GetInsertCraftDefinition(Item item, WorldGameObject obj)
        {
            if (item == null || item.IsEmpty())
            {
                return null;
            }

            var text = item.id;
            if (text.Contains(":"))
            {
                text = text.Split(':')[0];
            }

            var dataOrNull =
                GameBalance.me.GetDataOrNull<CraftDefinition>("insert:" + obj.obj_id + ":" + text);
            if (dataOrNull != null && !MainGame.me.save.IsCraftVisible(dataOrNull))
            {
                return null;
            }

            return dataOrNull;
        }

        private static CraftDefinition GetExtractCraftDefinition(Item item, WorldGameObject obj)
        {
            if (item.IsEmpty())
            {
                return null;
            }

            var text = item.id;
            if (text.Contains(":"))
            {
                text = text.Split(':')[0];
            }

            var dataOrNull = GameBalance.me.GetDataOrNull<CraftDefinition>("ex:" + obj.obj_id + ":" + text);
            if (dataOrNull != null && !MainGame.me.save.IsCraftVisible(dataOrNull))
            {
                return null;
            }

            return dataOrNull;
        }



    }
}


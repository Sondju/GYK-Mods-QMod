using HarmonyLib;
using Helper;
using System;
using System.Linq;
using System.Reflection;

namespace AddStraightToTable;

public static class MainPatcher
{
    private const string WheresMaStorageId = "WheresMaStorage";
    private const string WheresMaStorageFileName = "WheresMaStorage.dll";
    private const string WheresMaStorageName = "Where's Ma' Storage!";
    private static Config.Options _cfg;
    private static bool _wms;

    public static void Patch()
    {
        try
        {
            var harmony = new Harmony("p1xel8ted.GraveyardKeeper.AddStraightToTable");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
        catch (Exception ex)
        {
            Log($"{ex.Message}, {ex.Source}, {ex.StackTrace}", true);
        }
    }

    private static void Log(string message, bool error = false)
    {
        Tools.Log("AddStraightToTable", $"{message}", error);
    }

    [HarmonyPatch(typeof(AutopsyGUI), "OnBodyItemPress")]
    public static class AutopsyGuiOnBodyItemPressPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            _wms = Tools.ModLoaded(WheresMaStorageId, WheresMaStorageFileName, WheresMaStorageName) || Harmony.HasAnyPatches("p1xel8ted.GraveyardKeeper.WheresMaStorage");
            if (_wms)
            {
                _cfg = Config.GetOptions();
            }
            return false;
        }

        [HarmonyPostfix]
        public static void Postfix(ref AutopsyGUI __instance, BaseItemCellGUI item_gui, ref Item ____body,
            ref WorldGameObject ____autopti_obj, ref Inventory ____parts_inventory)
        {
            if (item_gui.item.id == "insertion_button_pseudoitem")
            {
                var obj = MainGame.me.player;
                if (GlobalCraftControlGUI.is_global_control_active && ____autopti_obj != null) obj = ____autopti_obj;

                var inventory = ____parts_inventory;
                var instance = __instance;
                GUIElements.me.resource_picker.Open(obj, delegate (Item item, InventoryWidget _)
                    {
                        if (item == null || item.IsEmpty())
                        {
                            if (_wms && _cfg.hideInvalidSelections)
                            {
                                return InventoryWidget.ItemFilterResult.Inactive;
                            }

                            return InventoryWidget.ItemFilterResult.Hide;
                        }

                        if (item.definition.type != ItemDefinition.ItemType.BodyUniversalPart)
                            return InventoryWidget.ItemFilterResult.Inactive;

                        var text = item.id;
                        if (text.Contains(":")) text = text.Split(':')[0];

                        text = text.Replace("_dark", "");
                        if (inventory.data.inventory.Any(item2 =>
                                item2 != null && !item2.IsEmpty() && item2.id.StartsWith(text)))
                            return InventoryWidget.ItemFilterResult.Inactive;

                        return instance.GetInsertCraftDefinition(item) == null
                            ? InventoryWidget.ItemFilterResult.Inactive
                            : InventoryWidget.ItemFilterResult.Active;
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

            if (craftDefinition == null) return;

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
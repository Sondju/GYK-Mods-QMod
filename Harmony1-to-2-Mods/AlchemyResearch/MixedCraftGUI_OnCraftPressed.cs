using System.Collections.Generic;
using HarmonyLib;

namespace AlchemyResearch
{
  [HarmonyPatch(typeof (MixedCraftGUI), "OnCraftPressed", new System.Type[] {})]
  public class MixedCraftGUI_OnCraftPressed
  {
    [HarmonyPrefix]
    public static void Patch(MixedCraftGUI __instance)
    {
      AlchemyRecipe.Initialize();
      if (!(bool) Reflection.MethodIsCraftAllowed.Invoke((object) __instance, new object[0]))
        return;
      MixedCraftPresetGUI mixedCraftPresetGui = (MixedCraftPresetGUI) Reflection.FieldCurrentPreset.GetValue((object) __instance);
      CraftDefinition craftDefinition = (CraftDefinition) Reflection.MethodGetCraftDefinition.Invoke((object) __instance, new object[2]
      {
        (object) false,
        null
      });
      if (craftDefinition == null)
        craftDefinition = (CraftDefinition) Reflection.MethodGetCraftDefinition.Invoke((object) __instance, new object[2]
        {
          (object) true,
          null
        });
      if (craftDefinition == null || !craftDefinition.id.StartsWith("mix:mf_alchemy"))
        return;
      List<Item> selectedItems = mixedCraftPresetGui.GetSelectedItems();
      if (selectedItems.Count < 2)
        return;
      for (int index = 0; index < selectedItems.Count; ++index)
      {
        switch (index)
        {
          case 0:
            AlchemyRecipe.Ingredient1 = selectedItems[index].id;
            break;
          case 1:
            AlchemyRecipe.Ingredient2 = selectedItems[index].id;
            break;
          case 2:
            AlchemyRecipe.Ingredient3 = selectedItems[index].id;
            break;
        }
        AlchemyRecipe.Result = craftDefinition.GetFirstRealOutput().id;
        AlchemyRecipe.WorkstationUnityID = __instance.GetCrafteryWGO().GetInstanceID();
        AlchemyRecipe.WorkstationObjectID = __instance.GetCrafteryWGO().obj_id;
      }
      Logg.Log(string.Format("Processed Recipe: {0}|{1}|{2} => {3} | WGO: {4} / {5}", (object) AlchemyRecipe.Ingredient1, (object) AlchemyRecipe.Ingredient2, (object) AlchemyRecipe.Ingredient3, (object) AlchemyRecipe.Result, (object) AlchemyRecipe.WorkstationUnityID, (object) AlchemyRecipe.WorkstationObjectID));
    }
  }
}

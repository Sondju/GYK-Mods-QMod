using HarmonyLib;
using UnityEngine;

namespace AlchemyResearch
{
  [HarmonyPatch(typeof (MixedCraftGUI), "OnResourcePickerClosed", new System.Type[] {typeof (Item)})]
  public class MixedCraftGUI_OnResourcePickerClosed
  {
    [HarmonyPostfix]
    public static void Patch(MixedCraftGUI __instance, Item item)
    {
      string objId = __instance.GetCrafteryWGO().obj_id;
      Transform crafteryTransform = MixedCraftGUI_OpenAsAlchemy.GetCrafteryTransform(__instance.transform, objId);
      Transform ResultPreview = __instance.transform.Find("ingredient container result");
      MixedCraftGUI_OpenAsAlchemy.ResultPreviewDrawUnknown(ResultPreview);
      if (!(bool) (UnityEngine.Object) crafteryTransform)
        return;
      Transform transform1 = crafteryTransform.transform.Find("ingredients/ingredient container/Base Item Cell");
      Transform transform2 = crafteryTransform.transform.Find("ingredients/ingredient container (1)/Base Item Cell");
      Transform transform3 = crafteryTransform.transform.Find("ingredients/ingredient container (2)/Base Item Cell");
      if (!(bool) (UnityEngine.Object) transform1 || !(bool) (UnityEngine.Object) transform2)
        return;
      BaseItemCellGUI component1 = transform1.GetComponent<BaseItemCellGUI>();
      BaseItemCellGUI component2 = transform2.GetComponent<BaseItemCellGUI>();
      BaseItemCellGUI baseItemCellGui = (BaseItemCellGUI) null;
      if ((bool) (UnityEngine.Object) transform3)
        baseItemCellGui = transform3.GetComponent<BaseItemCellGUI>();
      if (!(bool) (UnityEngine.Object) component1 || !(bool) (UnityEngine.Object) component2)
      {
        MixedCraftGUI_OpenAsAlchemy.ResultPreviewDrawUnknown(ResultPreview);
      }
      else
      {
        string id1 = component1.item.id;
        string id2 = component2.item.id;
        string Ingredient3 = "empty";
        if ((bool) (UnityEngine.Object) baseItemCellGui)
          Ingredient3 = baseItemCellGui.item.id;
        if (id1 == "empty" || id2 == "empty" || Ingredient3 == "empty" && objId == "mf_alchemy_craft_03")
        {
          MixedCraftGUI_OpenAsAlchemy.ResultPreviewDrawUnknown(ResultPreview);
        }
        else
        {
          string ItemID = ResearchedAlchemyRecipes.IsRecipeKnown(id1, id2, Ingredient3);
          MixedCraftGUI_OpenAsAlchemy.ResultPreviewDrawItem(ResultPreview, ItemID);
        }
      }
    }
  }
}

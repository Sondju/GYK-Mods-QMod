using HarmonyLib;
using UnityEngine;

namespace AlchemyResearch
{
  [HarmonyPatch(typeof (MixedCraftGUI), "Hide", new System.Type[] {typeof (bool)})]
  public class MixedCraftGUI_Hide
  {
    [HarmonyPostfix]
    public static void Postfix(MixedCraftGUI __instance)
    {
      Transform transform = __instance.transform.Find("ingredient container result");
      if (!(bool) (UnityEngine.Object) transform || !(bool) (UnityEngine.Object) transform.gameObject)
        return;
      UnityEngine.Object.Destroy((UnityEngine.Object) transform.gameObject);
    }
  }
}

using System.Reflection;
using HarmonyLib;

namespace AlchemyResearch
{
  public class Reflection
  {
    public static MethodInfo MethodIsCraftAllowed;
    public static MethodInfo MethodGetCraftDefinition;
    public static FieldInfo FieldCurrentPreset;

    public static void Initialization()
    {
      AlchemyResearch.Reflection.MethodIsCraftAllowed = typeof (MixedCraftGUI).GetMethod("IsCraftAllowed", AccessTools.all);
      AlchemyResearch.Reflection.MethodGetCraftDefinition = typeof (MixedCraftGUI).GetMethod("GetCraftDefinition", AccessTools.all);
      AlchemyResearch.Reflection.FieldCurrentPreset = typeof (MixedCraftGUI).GetField("_current_preset", AccessTools.all);
    }
  }
}

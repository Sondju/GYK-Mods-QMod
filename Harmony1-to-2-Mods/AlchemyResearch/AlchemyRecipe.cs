namespace AlchemyResearch
{
  public class AlchemyRecipe
  {
    public static int WorkstationUnityID = -1;
    public static string WorkstationObjectID = string.Empty;
    public static string Ingredient1 = "empty";
    public static string Ingredient2 = "empty";
    public static string Ingredient3 = "empty";
    public static string Result = "empty";
    public const string ItemEmpty = "empty";
    public const string ItemUnknown = "unknown";

    public static void Initialize()
    {
      AlchemyRecipe.WorkstationUnityID = -1;
      AlchemyRecipe.WorkstationObjectID = string.Empty;
      AlchemyRecipe.Ingredient1 = "empty";
      AlchemyRecipe.Ingredient2 = "empty";
      AlchemyRecipe.Ingredient3 = "empty";
      AlchemyRecipe.Result = "empty";
    }

    public static bool HasValidRecipe() => AlchemyRecipe.Ingredient1 != "empty" && !string.IsNullOrEmpty(AlchemyRecipe.Ingredient1) && AlchemyRecipe.Ingredient2 != "empty" && !string.IsNullOrEmpty(AlchemyRecipe.Ingredient2) && AlchemyRecipe.Result != "empty" && !string.IsNullOrEmpty(AlchemyRecipe.Result);

    public static bool MatchesResult(
      int CraftingStationUnityID,
      string CraftingStationObjectID,
      string ResultItemID)
    {
      if (AlchemyRecipe.WorkstationUnityID != CraftingStationUnityID || AlchemyRecipe.WorkstationObjectID != CraftingStationObjectID)
        return false;
      if (ResultItemID == AlchemyRecipe.Result)
        return true;
      return ResultItemID.StartsWith("goo_") && AlchemyRecipe.Result.StartsWith("goo_");
    }

    public static string AlchemyRecipeToString(
      string Ingredient1,
      string Ingredient2,
      string Ingredient3,
      string Result)
    {
      return string.Format("{0}|{1}", (object) ResearchedAlchemyRecipe.GetKey(Ingredient1, Ingredient2, Ingredient3), (object) Result);
    }

    public static string GetCurrentRecipe() => string.Format("{0}|{1}|{2}|{3}|{4}|{5}", (object) AlchemyRecipe.Ingredient1, (object) AlchemyRecipe.Ingredient2, (object) AlchemyRecipe.Ingredient3, (object) AlchemyRecipe.Result, (object) AlchemyRecipe.WorkstationUnityID, (object) AlchemyRecipe.WorkstationObjectID);
  }
}

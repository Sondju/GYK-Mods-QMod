using System.Collections.Generic;
using System.IO;

namespace AlchemyResearch
{
  public class ResearchedAlchemyRecipes
  {
    public static Dictionary<string, ResearchedAlchemyRecipe> ResearchedRecipes = new Dictionary<string, ResearchedAlchemyRecipe>();

    public static void AddCurrentRecipe(string ItemResult)
    {
      ResearchedAlchemyRecipe researchedAlchemyRecipe = new ResearchedAlchemyRecipe(AlchemyRecipe.Ingredient1, AlchemyRecipe.Ingredient2, AlchemyRecipe.Ingredient3, ItemResult);
      Logg.Log(string.Format("Adding Recipe: {0}|{1}|{2} => {3} | WGO: {4} / {5}", (object) AlchemyRecipe.Ingredient1, (object) AlchemyRecipe.Ingredient2, (object) AlchemyRecipe.Ingredient3, (object) AlchemyRecipe.Result, (object) AlchemyRecipe.WorkstationUnityID, (object) AlchemyRecipe.WorkstationObjectID));
      string key = researchedAlchemyRecipe.GetKey();
      if (!ResearchedAlchemyRecipes.ResearchedRecipes.ContainsKey(key))
        ResearchedAlchemyRecipes.ResearchedRecipes.Add(researchedAlchemyRecipe.GetKey(), researchedAlchemyRecipe);
      AlchemyRecipe.Initialize();
    }

    public static string IsRecipeKnown(string Ingredient1 = "empty", string Ingredient2 = "empty", string Ingredient3 = "empty")
    {
      string key = ResearchedAlchemyRecipe.GetKey(Ingredient1, Ingredient2, Ingredient3);
      return ResearchedAlchemyRecipes.ResearchedRecipes.ContainsKey(key) ? ResearchedAlchemyRecipes.ResearchedRecipes[key].result : "unknown";
    }

    public static Dictionary<string, Dictionary<string, ResearchedAlchemyRecipe>> ReadRecipesFromFile()
    {
      Dictionary<string, Dictionary<string, ResearchedAlchemyRecipe>> dictionary = new Dictionary<string, Dictionary<string, ResearchedAlchemyRecipe>>();
      string empty = string.Empty;
      string key = "1";
      string[] strArray1;
      try
      {
        strArray1 = File.ReadAllLines(MainPatcher.KnownRecipesFilePathAndName);
      }
      catch
      {
        return dictionary;
      }
      if (strArray1 == null || strArray1.Length == 0)
        return dictionary;
      foreach (string str1 in strArray1)
      {
        string str2 = str1.Trim();
        if (!str2.StartsWith(MainPatcher.ParameterComment) && !string.IsNullOrEmpty(str2))
        {
          if (str2.StartsWith(MainPatcher.ParameterSectionBegin) && str2.EndsWith(MainPatcher.ParameterSectionEnd))
          {
            key = str2.Replace(MainPatcher.ParameterSectionBegin, string.Empty).Replace(MainPatcher.ParameterSectionEnd, string.Empty).Trim();
            if (!dictionary.ContainsKey(key))
              dictionary.Add(key, new Dictionary<string, ResearchedAlchemyRecipe>());
          }
          else
          {
            string[] strArray2 = str2.Split(MainPatcher.ParameterSeparator);
            if (strArray2.Length >= 4)
            {
              ResearchedAlchemyRecipe researchedAlchemyRecipe = new ResearchedAlchemyRecipe(strArray2[0].Trim(), strArray2[1].Trim(), strArray2[2].Trim(), strArray2[3].Trim());
              dictionary[key].Add(researchedAlchemyRecipe.GetKey(), researchedAlchemyRecipe);
            }
          }
        }
      }
      using (Dictionary<string, Dictionary<string, ResearchedAlchemyRecipe>>.Enumerator enumerator = dictionary.GetEnumerator())
      {
        while (enumerator.MoveNext())
          Logg.Log(string.Format("Loaded Recipes for Savegame [{0}]: {1}", (object) enumerator.Current.Key, (object) enumerator.Current.Value.Count));
      }
      return dictionary;
    }

    public static void WriteRecipesToFile(string SaveGameName)
    {
      List<string> contents = new List<string>();
      Dictionary<string, Dictionary<string, ResearchedAlchemyRecipe>> dictionary1 = ResearchedAlchemyRecipes.ReadRecipesFromFile();
      if (!dictionary1.ContainsKey(SaveGameName))
        dictionary1.Add(SaveGameName, new Dictionary<string, ResearchedAlchemyRecipe>());
      using (Dictionary<string, ResearchedAlchemyRecipe>.Enumerator enumerator = ResearchedAlchemyRecipes.ResearchedRecipes.GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          Dictionary<string, ResearchedAlchemyRecipe> dictionary2 = dictionary1[SaveGameName];
          KeyValuePair<string, ResearchedAlchemyRecipe> current = enumerator.Current;
          string key1 = current.Key;
          if (!dictionary2.ContainsKey(key1))
          {
            Dictionary<string, ResearchedAlchemyRecipe> dictionary3 = dictionary1[SaveGameName];
            current = enumerator.Current;
            string key2 = current.Key;
            current = enumerator.Current;
            ResearchedAlchemyRecipe researchedAlchemyRecipe = current.Value;
            dictionary3.Add(key2, researchedAlchemyRecipe);
          }
        }
      }
      Dictionary<string, Dictionary<string, ResearchedAlchemyRecipe>>.Enumerator enumerator1 = dictionary1.GetEnumerator();
      while (enumerator1.MoveNext())
      {
        contents.Add(string.Format("[{0}]", (object) enumerator1.Current.Key));
        Dictionary<string, ResearchedAlchemyRecipe>.Enumerator enumerator2 = enumerator1.Current.Value.GetEnumerator();
        while (enumerator2.MoveNext())
        {
          KeyValuePair<string, ResearchedAlchemyRecipe> current = enumerator2.Current;
          ResearchedAlchemyRecipe researchedAlchemyRecipe = current.Value;
          current = enumerator2.Current;
          string ingredient1 = current.Value.ingredient1;
          current = enumerator2.Current;
          string ingredient2 = current.Value.ingredient2;
          current = enumerator2.Current;
          string ingredient3 = current.Value.ingredient3;
          current = enumerator2.Current;
          string result = current.Value.result;
          string str = researchedAlchemyRecipe.AlchemyRecipeToString(ingredient1, ingredient2, ingredient3, result);
          contents.Add(str);
        }
      }
      try
      {
        File.WriteAllLines(MainPatcher.KnownRecipesFilePathAndName, (IEnumerable<string>) contents);
      }
      catch
      {
      }
    }
  }
}

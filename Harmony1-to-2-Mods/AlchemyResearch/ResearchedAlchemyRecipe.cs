using System.Collections.Generic;

namespace AlchemyResearch
{
  public class ResearchedAlchemyRecipe
  {
    public string ingredient1 = "empty";
    public string ingredient2 = "empty";
    public string ingredient3 = "empty";
    public string result = "empty";

    public ResearchedAlchemyRecipe(
      string Ingredient1,
      string Ingredient2,
      string Ingredient3,
      string Result)
    {
      this.ingredient1 = Ingredient1;
      this.ingredient2 = Ingredient2;
      this.ingredient3 = Ingredient3;
      this.result = Result;
    }

    public string GetKey() => ResearchedAlchemyRecipe.GetKey(this.ingredient1, this.ingredient2, this.ingredient3);

    public static string GetKey(string Ingredient1, string Ingredient2, string Ingredient3)
    {
      List<string> stringList = new List<string>(3);
      stringList.Add(Ingredient1);
      stringList.Add(Ingredient2);
      stringList.Add(Ingredient3);
      stringList.Sort();
      return string.Format("{0}|{1}|{2}", (object) stringList[0], (object) stringList[1], (object) stringList[2]);
    }

    public string AlchemyRecipeToString(
      string Ingredient1,
      string Ingredient2,
      string Ingredient3,
      string Result)
    {
      return string.Format("{0}|{1}", (object) ResearchedAlchemyRecipe.GetKey(Ingredient1, Ingredient2, Ingredient3), (object) Result);
    }
  }
}

using System;

public enum RecipeType
{
    Italian, Eastern, Dutch, Ease, Jamie, Allerhande, Random
}
[Serializable]
public class Recipe
{
    public int ID;
    
    public RecipeType recipeType;
    public string name;
    public string description;
    public int cookTime;

    public Recipe(RecipeType recipeType, string name, string description, int cookTime)
    {
        this.ID = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        this.recipeType = recipeType;
        this.name = name;
        this.description = description;
        this.cookTime = cookTime;
    }
    public Recipe(int ID)
    {
        this.ID = ID;
    }
}


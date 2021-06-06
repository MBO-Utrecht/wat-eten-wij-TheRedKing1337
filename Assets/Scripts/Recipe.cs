using System;
using UnityEngine;

public enum RecipeType
{
    Italian, Eastern, Dutch, Ease, Jamie, Allerhande, Random, Length
}
[Serializable] [CreateAssetMenu(fileName ="Recipe",menuName ="Recipe", order = 0)]
public class Recipe : ScriptableObject
{
    public int ID;
    
    public RecipeType recipeType;
    public string name;
    [TextArea(5,40)]
    public string description;
    public int cookTime;

    public Recipe()
    {

    }
    public Recipe(RecipeType recipeType, string name, string description, int cookTime)
    {
        this.ID = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        this.recipeType = recipeType;
        this.name = name;
        this.description = description;
        this.cookTime = cookTime;
    }
    public Recipe(int ID, RecipeType recipeType, string name, string description, int cookTime)
    {
        this.ID = ID;
        this.recipeType = recipeType;
        this.name = name;
        this.description = description;
        this.cookTime = cookTime;
    }
    private void OnValidate()
    {
        if(ID == 0)
        {
            this.ID = (int)DateTimeOffset.Now.ToUnixTimeSeconds();
        }
    }
}


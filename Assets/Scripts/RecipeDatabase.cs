using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RecipeDatabase
{
    public Recipe[] recipes = new Recipe[0];

    public void AddNew(Recipe recipe)
    {
        Recipe[] newArray = new Recipe[recipes.Length + 1];
        for(int i = 0; i < recipes.Length; i++)
        {
            newArray[i] = recipes[i];
        }
        newArray[recipes.Length] = recipe;
        recipes = newArray;
    }
}

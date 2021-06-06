using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RecipeDatabase
{
    public Recipe[] recipes;

    public void AddNew(Recipe recipe)
    {
        int length;
        if (recipes == null) length = 1;
        else length = recipes.Length + 1;
        Recipe[] newArray = new Recipe[length];
        for (int i = 0; i < length - 1; i++)
        {
            newArray[i] = recipes[i];
        }
        newArray[length-1] = recipe;
        recipes = newArray;
    }
}

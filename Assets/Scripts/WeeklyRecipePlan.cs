using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class WeeklyRecipePlan : MonoBehaviour
{
    [InspectorButton("SaveRecipes")]
    public bool saveRecipes;

    [Header("User settings")]
    [SerializeField] private RecipeType[] dailyTypes;
    [Header("References")]
    [SerializeField] private Button[] recipeModals;
    [SerializeField] private Button addNewButton;
    [SerializeField] private Button refreshButton;

    private Recipe[] weeklyRecipes;
    [SerializeField] private RecipeDatabase recipeDatabase;

    private int selectedModal = int.MaxValue; //MaxValue to signify none selected
    private const string recipeDataPath = "/StreamingAssets/recipes.json";

    private void Start()
    {
        //Get the recipes list from playerPrefs string
        string filePath = Application.dataPath + recipeDataPath;

        if (File.Exists(filePath))
        {
            string dataAsJson = File.ReadAllText(filePath);
            recipeDatabase = JsonUtility.FromJson<RecipeDatabase>(dataAsJson);
        }

        //Init the button onClicks
        for (int i = 0; i < 7; i++)
        {
            int index = i;
            recipeModals[i].onClick.AddListener(() => SelectRecipe(index));
        }
        addNewButton.onClick.AddListener(AddRecipe);
        refreshButton.onClick.AddListener(GenerateWeeklyList);

        GenerateWeeklyList();
    }
    public void GenerateWeeklyList()
    {
        //Get array of random recipes based on the daily types
        weeklyRecipes = new Recipe[7];
        for (int i = 0; i < 7; i++)
        {
            //Get the daily type
            RecipeType dailyType = dailyTypes[i];
            //If today is random pick a random type
            if (dailyType == RecipeType.Random)
            {
                dailyType = (RecipeType)Random.Range(0, (int)RecipeType.Random);
            }
            //Get the list of that type of recipe
            var recipesOfDailyType = recipeDatabase.recipes.Where(x => x.recipeType == dailyType);
            if (recipesOfDailyType.Count() == 0)
            {
                weeklyRecipes[i] = new Recipe(dailyType, "No recipes in this list", "No recipes of type: " + dailyType.ToString() + " found.", 0);
            }
            else
            {
                weeklyRecipes[i] = recipesOfDailyType.ElementAt(Random.Range(0, recipesOfDailyType.Count()));
            }

            //Update UI
            recipeModals[i].transform.GetChild(0).GetChild(0).GetComponent<TMP_Text>().text = weeklyRecipes[i].name;
            TMP_InputField description = recipeModals[i].transform.GetChild(0).GetChild(1).GetComponent<TMP_InputField>();
            description.text = weeklyRecipes[i].description;
            string cookTimeString = "Cook Time: " + weeklyRecipes[i].cookTime + " min.";
            recipeModals[i].transform.GetChild(0).GetChild(2).GetComponent<TMP_Text>().text = cookTimeString;
            recipeModals[i].transform.GetChild(1).GetComponent<Image>().sprite = Resources.Load<Sprite>("RecipeImages/" + weeklyRecipes[i].name);
            recipeModals[i].transform.GetChild(2).GetComponent<Image>().sprite = Resources.Load<Sprite>("RecipeTypeImages/" + weeklyRecipes[i].recipeType.ToString());
        }
    }
    public void SelectRecipe(int id)
    {
        if (selectedModal == id)
        {
            StartCoroutine(ResizeModal(recipeModals[selectedModal].gameObject, 0.2f, 0));
            selectedModal = int.MaxValue;
            return;
        }
        //Use anim to grow recipe box to fullscreen
        if (selectedModal != int.MaxValue)
        {
            StartCoroutine(ResizeModal(recipeModals[selectedModal].gameObject, 0.2f, 0));
        }
        selectedModal = id;
        StartCoroutine(ResizeModal(recipeModals[id].gameObject, 0.25f, 1200));

        //Allow edits
    }
    public void EditRecipe(int id)
    {

    }
    public void ShowRecipeAdder()
    {

    }
    public void AddRecipe()
    {


        SaveRecipes();
    }
    public void SaveRecipes()
    {
        string dataAsJson = JsonUtility.ToJson(recipeDatabase);

        string filePath = Application.dataPath + recipeDataPath;
        File.WriteAllText(filePath, dataAsJson);
    }

    private IEnumerator ResizeModal(GameObject modal, float time, float newSize)
    {
        LayoutElement layoutElement = modal.GetComponent<LayoutElement>();
        Scrollbar scrollbar = modal.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
        float oldSize = layoutElement.preferredHeight;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            layoutElement.preferredHeight = Mathf.Lerp(oldSize, newSize, t);
            scrollbar.value = 0;
            yield return null;
        }
    }
}

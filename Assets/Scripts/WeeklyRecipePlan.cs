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
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button preferencesButton;
    [SerializeField] private Button savePreferencesButton;
    [SerializeField] private GameObject[] dailyTypeSelectTabs;

    private Recipe[] weeklyRecipes;
    private RecipeDatabase recipeDatabase;

    private enum DaysOfTheWeek
    {
        Mon, Tue, Wed, Thu, Fri, Sat, Sun
    }

    private int selectedModal = int.MaxValue; //MaxValue to signify none selected

    private void Start()
    {
        PlayerPrefs.SetInt("FIRSTTIMEOPENING", 1);
        if (PlayerPrefs.GetInt("FIRSTTIMEOPENING", 1) == 1)
        {
            Debug.Log("First time startup, loading from resources");
            //Set first time opening to false
            PlayerPrefs.SetInt("FIRSTTIMEOPENING", 0);

            //Do your stuff here
            string dataAsJson = Resources.Load<TextAsset>("recipes").text;
            recipeDatabase = JsonUtility.FromJson<RecipeDatabase>(dataAsJson);
        }
        else
        {
            string dataAsJson = PlayerPrefs.GetString("Recipes");
            recipeDatabase = JsonUtility.FromJson<RecipeDatabase>(dataAsJson);
        }

        //Init the button onClicks
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < (int)RecipeType.Length; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(((RecipeType)i).ToString()));
        }
        for (int i = 0; i < 7; i++)
        {
            int index = i;
            recipeModals[i].onClick.AddListener(() => SelectRecipe(index));
            dailyTypeSelectTabs[i].transform.GetChild(0).GetComponent<TMP_Text>().text = ((DaysOfTheWeek)i).ToString();
            dailyTypeSelectTabs[i].transform.GetChild(1).GetComponent<TMP_Dropdown>().options = options;
            dailyTypeSelectTabs[i].transform.GetChild(1).GetComponent<TMP_Dropdown>().value = (int)dailyTypes[i];
        }
        refreshButton.onClick.AddListener(GenerateWeeklyList);
        preferencesButton.onClick.AddListener(ShowPreferences);
        savePreferencesButton.onClick.AddListener(SavePreferences);


        //load weeklyRecipes from playerPrefs
        int lastRefresh = PlayerPrefs.GetInt("LastRefresh");
        int nextWeek = lastRefresh + 604800;
        if (nextWeek > (int)System.DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            weeklyRecipes = new Recipe[7];
            for (int i = 0; i < 7; i++)
            {
                weeklyRecipes[i] = recipeDatabase.recipes.Where(x => x.ID == PlayerPrefs.GetInt("weeklyRecipes" + i)).First();
            }
        }
        else
        {
            GenerateWeeklyList();
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        //Save weeklyRecipes IDs in playerPrefs
        for (int i = 0; i < 7; i++)
        {
            PlayerPrefs.SetInt("weeklyRecipes" + i, weeklyRecipes[i].ID);
        }
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
        }
        PlayerPrefs.SetInt("LastRefresh", (int)System.DateTimeOffset.Now.ToUnixTimeSeconds());
        UpdateUI();
    }
    private void UpdateUI()
    {
        for (int i = 0; i < 7; i++)
        {
            //Update UI
            recipeModals[i].transform.GetChild(0).GetComponent<TMP_Text>().text = weeklyRecipes[i].name;
            TMP_Text description = recipeModals[i].transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<TMP_Text>();
            description.text = weeklyRecipes[i].description;
            description.ForceMeshUpdate();
            string cookTimeString = "Cook Time: " + weeklyRecipes[i].cookTime + " min.";
            recipeModals[i].transform.GetChild(2).GetComponent<TMP_Text>().text = cookTimeString;
            recipeModals[i].transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("RecipeImages/" + weeklyRecipes[i].name);
            recipeModals[i].transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("RecipeTypeImages/" + weeklyRecipes[i].recipeType.ToString());
            recipeModals[i].transform.GetChild(5).GetChild(0).GetComponent<TMP_Text>().text = ((DaysOfTheWeek)i).ToString();
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
    }
    public void ShowPreferences()
    {
        dailyTypeSelectTabs[0].transform.parent.gameObject.SetActive(true);
    }
    public void SavePreferences()
    {
        for (int i = 0; i < 7; i++)
        {
            dailyTypes[i] = (RecipeType)dailyTypeSelectTabs[i].transform.GetChild(1).GetComponent<TMP_Dropdown>().value;
        }
        dailyTypeSelectTabs[0].transform.parent.gameObject.SetActive(false);
        GenerateWeeklyList();
    }
    public void SaveRecipes()
    {
        string dataAsJson = JsonUtility.ToJson(recipeDatabase);

        PlayerPrefs.SetString("Recipes", dataAsJson);
    }

    private IEnumerator ResizeModal(GameObject modal, float time, float newSize)
    {
        LayoutElement layoutElement = modal.GetComponent<LayoutElement>();
        float oldSize = layoutElement.preferredHeight;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / time;
            layoutElement.preferredHeight = Mathf.Lerp(oldSize, newSize, t);
            yield return null;
        }
    }
}

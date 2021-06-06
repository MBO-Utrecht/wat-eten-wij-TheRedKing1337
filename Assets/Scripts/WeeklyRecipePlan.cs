using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Xml.Serialization;

public class WeeklyRecipePlan : MonoBehaviour, IListScollViewController
{
    [InspectorButton("SaveRecipes")]
    public bool saveRecipes;

    [Header("User settings")]
    [SerializeField] private RecipeType[] dailyTypes;
    [Header("References")]
    [SerializeField] private Button[] recipeModals;
    [SerializeField] private Button toggleRecipeListButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button preferencesButton;
    [SerializeField] private Button savePreferencesButton;
    [SerializeField] private Button addNewButton;
    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private GameObject[] dailyTypeSelectTabs;
    [SerializeField] private ListScrollView recipeScollView;

    [SerializeField] private GameObject recipeListCanvas;
    [SerializeField] private GameObject weeklyCanvas;

    private Recipe[] weeklyRecipes;
    private List<Recipe> presetRecipes = new List<Recipe>();
    private RecipeDatabase recipeDatabase;
    private List<Recipe> combinedList = new List<Recipe>();
    private List<Recipe> previewList = new List<Recipe>();

    private enum DaysOfTheWeek
    {
        Mon, Tue, Wed, Thu, Fri, Sat, Sun
    }

    private int selectedModal = int.MaxValue; //MaxValue to signify none selected

    private void Start()
    {
        presetRecipes = Resources.LoadAll<Recipe>("PresetRecipes").ToList();

        try
        {
            string recipesString = PlayerPrefs.GetString("Recipes");
            XmlSerializer xml = new XmlSerializer(typeof(RecipeDatabase));
            StringReader reader = new StringReader(recipesString);
            recipeDatabase = (RecipeDatabase)xml.Deserialize(reader);
        }
        catch
        {
            PlayerPrefs.DeleteKey("Recipes");
            recipeDatabase = new RecipeDatabase();
        }

        if (recipeDatabase.recipes == null) combinedList = presetRecipes;
        else combinedList = presetRecipes.Concat((recipeDatabase.recipes).ToList()).ToList();

        for(int i= combinedList.Count-1; i > -1; i--)
        {
            if(combinedList[i] == null)
            {
                combinedList.RemoveAt(i);
            }
        }



        //Load Daily preferences from PlayerPrefs
        int encodedValues = PlayerPrefs.GetInt("DailyPreferences", int.MaxValue);
        if (encodedValues != int.MaxValue)
        {
            for (int i = 0; i < 7; i++)
            {
                dailyTypes[i] = (RecipeType)((encodedValues >> (i * 4)) & 0xF);
            }
        }

        //Init the button onClicks
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < (int)RecipeType.Length; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(((RecipeType)i).ToString(), Resources.Load<Sprite>("RecipeTypeImages/" + ((RecipeType)i).ToString())));
        }
        recipeListCanvas.transform.GetChild(1).GetChild(1).GetComponent<TMP_Dropdown>().options = options;
        options.RemoveAt(options.Count - 1);
        options.Add(new TMP_Dropdown.OptionData("None"));
        sortDropdown.options = options;
        sortDropdown.value = (int)RecipeType.Random;
        sortDropdown.onValueChanged.AddListener(SortPreviewList);
        for (int i = 0; i < 7; i++)
        {
            int index = i;
            recipeModals[i].onClick.AddListener(() => SelectRecipe(index));
            dailyTypeSelectTabs[i].transform.GetChild(0).GetComponent<TMP_Text>().text = ((DaysOfTheWeek)i).ToString();
            dailyTypeSelectTabs[i].transform.GetChild(1).GetComponent<TMP_Dropdown>().options = options;
            dailyTypeSelectTabs[i].transform.GetChild(1).GetComponent<TMP_Dropdown>().value = (int)dailyTypes[i];
        }
        toggleRecipeListButton.onClick.AddListener(ToggleRecipeList);
        refreshButton.onClick.AddListener(GenerateWeeklyList);
        preferencesButton.onClick.AddListener(ShowPreferences);
        savePreferencesButton.onClick.AddListener(SavePreferences);
        addNewButton.onClick.AddListener(() => { recipeListCanvas.transform.GetChild(1).gameObject.SetActive(!recipeListCanvas.transform.GetChild(1).gameObject.activeSelf); });
        recipeListCanvas.transform.GetChild(1).GetChild(5).GetComponent<Button>().onClick.AddListener(SaveNewRecipe);

        //Init the recipe list and give its modals events
        previewList = combinedList;
        recipeScollView.InitElements(this, previewList.Count);

        recipeListCanvas.SetActive(false);
        weeklyCanvas.SetActive(true);
        toggleRecipeListButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Recipe List";

        //load weeklyRecipes from playerPrefs
        int lastRefresh = PlayerPrefs.GetInt("LastRefresh");
        int nextWeek = lastRefresh + 604800;
        if (nextWeek > (int)System.DateTimeOffset.Now.ToUnixTimeSeconds())
        {
            weeklyRecipes = new Recipe[7];
            for (int i = 0; i < 7; i++)
            {
                int dailyID = PlayerPrefs.GetInt("weeklyRecipes" + i);
                weeklyRecipes[i] = combinedList.Where(x => x.ID == dailyID).FirstOrDefault();
            }
        }
        else
        {
            GenerateWeeklyList();
        }

        UpdateUI();
    }

    private void ToggleRecipeList()
    {
        if (recipeListCanvas.activeSelf)
        {
            recipeListCanvas.SetActive(false);
            weeklyCanvas.SetActive(true);
            toggleRecipeListButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Recipe List";
            preferencesButton.gameObject.SetActive(true);
            refreshButton.gameObject.SetActive(true);
            addNewButton.gameObject.SetActive(false);
            sortDropdown.gameObject.SetActive(false);
        }
        else
        {
            recipeListCanvas.SetActive(true);
            weeklyCanvas.SetActive(false);
            toggleRecipeListButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "Weekly List";
            preferencesButton.gameObject.SetActive(false);
            refreshButton.gameObject.SetActive(false);
            addNewButton.gameObject.SetActive(true);
            sortDropdown.gameObject.SetActive(true);
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
            var recipesOfDailyType = combinedList.Where(x => x.recipeType == dailyType);
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
        //Save weeklyRecipes IDs in playerPrefs
        for (int i = 0; i < 7; i++)
        {
            PlayerPrefs.SetInt("weeklyRecipes" + i, weeklyRecipes[i].ID);
        }
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
            LayoutRebuilder.ForceRebuildLayoutImmediate(description.transform.parent.GetComponent<RectTransform>());
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
        dailyTypeSelectTabs[0].transform.parent.gameObject.SetActive(!dailyTypeSelectTabs[0].transform.parent.gameObject.activeSelf);
    }
    public void SavePreferences()
    {
        int encodedPreferences = 0;
        for (int i = 0; i < 7; i++)
        {
            int value = dailyTypeSelectTabs[i].transform.GetChild(1).GetComponent<TMP_Dropdown>().value;
            dailyTypes[i] = (RecipeType)value;
            encodedPreferences += value << 4 * i;
        }
        PlayerPrefs.SetInt("DailyPreferences", encodedPreferences);
        dailyTypeSelectTabs[0].transform.parent.gameObject.SetActive(false);
        GenerateWeeklyList();
    }
    public void SaveRecipes()
    {
        XmlSerializer xml = new XmlSerializer(typeof(RecipeDatabase));
        StringWriter writer = new StringWriter();
        xml.Serialize(writer, recipeDatabase);
        PlayerPrefs.SetString("Recipes", writer.ToString());
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

    public void FillWithInfo(GameObject toFill, int index)
    {
        toFill.transform.GetChild(0).GetComponent<TMP_Text>().text = previewList[index].name;
        TMP_Text description = toFill.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<TMP_Text>();
        description.text = previewList[index].description;
        LayoutRebuilder.ForceRebuildLayoutImmediate(description.transform.parent.GetComponent<RectTransform>());
        string cookTimeString = "Cook Time: " + previewList[index].cookTime + " min.";
        toFill.transform.GetChild(2).GetComponent<TMP_Text>().text = cookTimeString;
        toFill.transform.GetChild(3).GetComponent<Image>().sprite = Resources.Load<Sprite>("RecipeImages/" + previewList[index].name);
        toFill.transform.GetChild(4).GetComponent<Image>().sprite = Resources.Load<Sprite>("RecipeTypeImages/" + previewList[index].recipeType.ToString());
    }
    public void SaveNewRecipe()
    {
        Transform inputPanel = recipeListCanvas.transform.GetChild(1);
        string name = inputPanel.GetChild(0).GetComponent<TMP_InputField>().text;
        RecipeType recipeType = (RecipeType)inputPanel.GetChild(1).GetComponent<TMP_Dropdown>().value;
        string description = inputPanel.GetChild(2).GetComponent<TMP_InputField>().text;
        int cookTime = int.Parse(inputPanel.GetChild(3).GetComponent<TMP_InputField>().text);

        Recipe recipe = new Recipe(recipeType, name, description, cookTime);
        recipeDatabase.AddNew(recipe);
        combinedList.Add(recipe);
        SaveRecipes();
        SortPreviewList((int)RecipeType.Random);
        sortDropdown.value = (int)RecipeType.Random;

        inputPanel.gameObject.SetActive(false);
    }
    public void SortPreviewList(int previewType)
    {
        if (previewType == (int)RecipeType.Random) previewList = combinedList;
        else previewList = combinedList.Where(x => x.recipeType == (RecipeType)previewType).ToList();

        recipeScollView.InitElements(this, previewList.Count);
    }
}

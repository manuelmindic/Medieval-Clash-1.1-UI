using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BotSettings : MonoBehaviour
{

    public Slider simSlider;
    public TMP_Text simValueText;

    public TMP_Dropdown dropdownAlgOne;
    public TMP_Dropdown dropdownAlgTwo;

    public Toggle toggleMana;
    public Toggle toggleBuffDebuff;
    public Toggle toggleHidden;

    public void PlayBotVsBotGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void BackToStartScreen()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 2);
    } 

    void Start()
    {
        float savedSimValue = PlayerPrefs.GetFloat("SimulationsAmount", 1f);
        simSlider.value = savedSimValue;
        simValueText.text = "Amount of Simulations: " + simSlider.value.ToString();
        simSlider.onValueChanged.AddListener(UpdateAmountText);

        int _algorithmOne = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);
        dropdownAlgOne.value = _algorithmOne;
        dropdownAlgOne.onValueChanged.AddListener(HandleAlgoDataOne);

        int _algorithmTwo = PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1);
        dropdownAlgTwo.value = _algorithmTwo;
        dropdownAlgTwo.onValueChanged.AddListener(HandleAlgoDataTwo);

        // Load toggle states
        toggleMana.isOn = PlayerPrefs.GetInt("ToggleManaState", 0) == 1;
        toggleBuffDebuff.isOn = PlayerPrefs.GetInt("ToggleBuffDebuffState", 0) == 1;
        toggleHidden.isOn = PlayerPrefs.GetInt("Deck", 0) == 1;

        // Add listeners to save state when toggled
        toggleMana.onValueChanged.AddListener((value) => SaveToggleState("ToggleManaState", value));
        toggleBuffDebuff.onValueChanged.AddListener((value) => SaveToggleState("ToggleBuffDebuffState", value));
        toggleHidden.onValueChanged.AddListener((value) => SaveToggleState("Deck", value));
    }

    private void SaveToggleState(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void HandleAlgoDataOne(int value)
    {
        if (value == 1)
        {
            PlayerPrefs.SetInt("AlgorithmusEinsBotVsBot", value);
        }
        if (value == 2)
        {
            PlayerPrefs.SetInt("AlgorithmusEinsBotVsBot", value);
        }
        if (value == 3)
        {
            PlayerPrefs.SetInt("AlgorithmusEinsBotVsBot", value);
        }
    }

    public void HandleAlgoDataTwo(int value)
    {
        if (value == 1)
        {
            PlayerPrefs.SetInt("AlgorithmusZweiBotVsBot", value);
        }
        if (value == 2)
        {
            PlayerPrefs.SetInt("AlgorithmusZweiBotVsBot", value);
        }
        if (value == 3)
        {
            PlayerPrefs.SetInt("AlgorithmusZweiBotVsBot", value);
        }
    }

    void UpdateAmountText(float value)
    {
        simValueText.text = "Amount of Simulations: " + value.ToString();

        PlayerPrefs.SetFloat("SimulationsAmount", value);
    }
}

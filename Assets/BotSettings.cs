using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.Rendering.CameraUI;

public class BotSettings : MonoBehaviour
{

    public Slider simSlider;
    public TMP_Text simValueText;

    public TMP_Dropdown dropdownAlgOne;
    public TMP_Dropdown dropdownAlgTwo;

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

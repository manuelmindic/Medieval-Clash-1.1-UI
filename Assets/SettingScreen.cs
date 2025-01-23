using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SettingScreen : MonoBehaviour
{
    private string input;
    [SerializeField] private TMP_InputField inputField;
    public ReadJSON _readJSON;
    public TextAsset _jsontext;
    public RawImage profilePicture1Circle;
    public RawImage profilePicture2Circle;
    public RawImage profilePicture3Circle;
    public Button confirmButton;
    public TextMeshProUGUI output;
    [SerializeField] private Toggle deckToggle;
    [SerializeField] private TMP_Dropdown dropdown;

    void Start()
    {
        UpdateProfilePicture();
        InitUI();
        confirmButton.onClick.AddListener(OnConfirmButtonClick);
    }

    private void InitUI()
    {
        string _username = PlayerPrefs.GetString("Username", "");
        inputField.text = _username;

        int _deck = PlayerPrefs.GetInt("Deck", 0);
        deckToggle.isOn = (_deck == 1);
        deckToggle.onValueChanged.AddListener(HandeToggle);

        int _algorithm = PlayerPrefs.GetInt("Algorithmus", 1);
        dropdown.value = _algorithm;
        dropdown.onValueChanged.AddListener(HandleAlgoData);
    }

    // Update is called once per frame
    public void UpdateProfilePicture()
    {
        profilePicture1Circle.gameObject.SetActive(false);
        profilePicture2Circle.gameObject.SetActive(false);
        profilePicture3Circle.gameObject.SetActive(false);

        switch (PlayerPrefs.GetString("ProfilePicture", "profilePicture1"))
        {
            case "profilePicture1":
                profilePicture1Circle.gameObject.SetActive(true);
                break;
            case "profilePicture2":
                profilePicture2Circle.gameObject.SetActive(true);
                break;
            case "profilePicture3":
                profilePicture3Circle.gameObject.SetActive(true);
                break;
        }
    }
    public void ReadInput(string username)
    {
        input = username;
        PlayerPrefs.SetString("Username", username);
        _readJSON.AddRecord(username);
    }

    public void OnConfirmButtonClick()
    {
        ReadInput(inputField.text);
    }

    public void ChangeProfilePicture(string pictureName)
    {
        PlayerPrefs.SetString("ProfilePicture", pictureName);
        UpdateProfilePicture();
    }

    public void HandleAlgoData(int value)
    {
        if (value == 1)
        {
            PlayerPrefs.SetInt("Algorithmus", value);
            output.text = "The chosen Algorithm for the Bot is Minimax!";
        }
        if (value == 2)
        {
            PlayerPrefs.SetInt("Algorithmus", value);
            output.text = "The chosen Algorithm for the Bot is Minimax with Alpha-Beta-Pruning!";
        }
        if (value == 3)
        {
            PlayerPrefs.SetInt("Algorithmus", value);
            output.text = "The chosen Algorithm for the Bot is Minimax with Monte Carlo Tree Search!";
        }
    }

    public void HandeToggle(bool toggle)
    {
        PlayerPrefs.SetInt("Deck", toggle ? 1 : 0);
    }
}

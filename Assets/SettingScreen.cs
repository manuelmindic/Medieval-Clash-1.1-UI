using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SettingScreen : MonoBehaviour { 
    private string input;
    public ReadJSON _readJSON;
    public TextAsset _jsontext;
    public RawImage profilePicture1Circle;
    public RawImage profilePicture2Circle;
    public RawImage profilePicture3Circle;


    void Start()
    {
        UpdateProfilePicture();
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
        Debug.Log(username);
        _readJSON.AddRecord(username);
        
    }

    public void ChangeProfilePicture(string pictureName)
    {
        PlayerPrefs.SetString("ProfilePicture", pictureName);
        UpdateProfilePicture();
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SettingScreen : MonoBehaviour { 
    private string input;
    
    public void ReadInput(string username)
    {
        input = username;
        PlayerPrefs.SetString("Username", username);
        Debug.Log(input);
    }
}

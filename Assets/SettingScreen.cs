using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SettingScreen : MonoBehaviour { 
    private string input;
    public ReadJSON _readJSON;
    public TextAsset _jsontext;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ReadInput(string username)
    {
        input = username;
        PlayerPrefs.SetString("Username", username);
        Debug.Log(username);
        _readJSON.AddRecord(username);
        
    }
}

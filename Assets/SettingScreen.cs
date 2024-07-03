using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SettingScreen : MonoBehaviour { 
    private string input;
<<<<<<< Updated upstream
    
=======
    public ReadJSON _readJSON;
    public TextAsset _jsontext;
    //public Player _player;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
>>>>>>> Stashed changes
    public void ReadInput(string username)
    {
        input = username;
        PlayerPrefs.SetString("Username", username);
        Debug.Log(username);
        _readJSON.AddRecord(username);
        
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingScreen : MonoBehaviour { 
    private string input;
    //public Player _player;


    // Start is called before the first frame update
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
        Debug.Log(input);
    }
}

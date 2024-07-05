using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class ReadJSON : MonoBehaviour
{
    private string filePath;

    [System.Serializable]
    public class Record
    {
        public string Name;
        public int Rating;
    }

    [System.Serializable]
    public class RecordsList
    {
        public Record[] records;
    }

    public RecordsList myRecordList = new RecordsList();

    // Start is called before the first frame update
    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "Data", "UserData.json");
        myRecordList = JsonUtility.FromJson<RecordsList>(File.ReadAllText(filePath));
        myRecordList.records = myRecordList.records.OrderByDescending(record => record.Rating).ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddRecord(string name)
    {    
        if (!doesUserExist(name))
        {
            List<Record> recordsList = myRecordList.records.ToList();
            recordsList.Add(new Record { Name = name, Rating = 1000 }); // default rating of new users und so
            myRecordList.records = recordsList.OrderByDescending(record => record.Rating).ToArray();
            saveRecordToFile();
        }
    }

    private bool doesUserExist(string name)
    {
        foreach (var user in myRecordList.records)
        {
            if (user.Name == name)
                return true;
        }

        return false;
    }

    public Record GetRecordByName(string name)
    {
        return myRecordList.records.FirstOrDefault(record => record.Name == name);
    }

    public void UpdateRecord(string name, int editRating)
    {
        Debug.Log(name);
        Record record = GetRecordByName(name);

        if (record.Rating + editRating < 0) {  // + und - ist -
            record.Rating = 0;
        }
        else
        {
            record.Rating += editRating;
        }

        saveRecordToFile();
    }

    public void saveRecordToFile()
    {
        string jsonText = JsonUtility.ToJson(myRecordList, true);
        File.WriteAllText(filePath, jsonText);
        Debug.Log(Application.persistentDataPath);
        #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
        #endif
    }
}

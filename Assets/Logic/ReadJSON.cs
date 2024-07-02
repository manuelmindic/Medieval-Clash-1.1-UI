using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

public class ReadJSON : MonoBehaviour
{

    public TextAsset _jsontext;

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
        myRecordList = JsonUtility.FromJson<RecordsList>(_jsontext.text);
        myRecordList.records = myRecordList.records.OrderByDescending(record => record.Rating).ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void fillRecords(TextAsset jsontext)
    {
        myRecordList = JsonUtility.FromJson<RecordsList>(jsontext.text);
        myRecordList.records = myRecordList.records.OrderByDescending(record => record.Rating).ToArray();

    }

    public void AddRecord(string name)
    {
        List<Record> recordsList = myRecordList.records.ToList();
        recordsList.Add(new Record { Name = name, Rating = 1000 }); // default rating of new users und so
        myRecordList.records = recordsList.OrderByDescending(record => record.Rating).ToArray();
        saveRecordToFile();
    }

    public Record GetRecordByName(string name)
    {
        return myRecordList.records.FirstOrDefault(record => record.Name == name);
    }

    public void UpdateRecord(string name, int editRating)
    {
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
        File.WriteAllText("Assets/Data/UserData.json", jsonText);

        #if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
        #endif
    }
}

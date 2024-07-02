using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReadJSON : MonoBehaviour
{

    public TextAsset _jsontext;

    [System.Serializable]
    public class Record
    {
        public string Name;
        public string Rating;
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
}

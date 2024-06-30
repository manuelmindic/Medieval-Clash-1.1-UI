using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScoreBoard : MonoBehaviour
{
    private Transform entryContainer;
    private Transform entryTemplate;
    public TextAsset _jsontext;
    public ReadJSON readJSON;

    private void Awake()
    {
        readJSON.fillRecords(_jsontext);
        entryContainer = transform.Find("scoreboardContainer");
        entryTemplate = entryContainer.Find("scoreboardTemplate");

        entryTemplate.gameObject.SetActive(false);

        float templateHeight = 20f;

        int index = 0;
        int padding = 0;
        foreach (var item in readJSON.myRecordList.records)
        {
            Transform entryTransform = Instantiate(entryTemplate, entryContainer);
            RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();
            entryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * index);
            entryRectTransform.gameObject.SetActive(true);

            TextMeshProUGUI nameField = entryTransform.Find("NameField").GetComponent<TextMeshProUGUI>();
            nameField.text = item.Name;
            nameField.fontSize = 40;
            nameField.margin = new Vector4(0, padding, 0, 0);

            TextMeshProUGUI ratingField = entryTransform.Find("RatingField").GetComponent<TextMeshProUGUI>();
            ratingField.text = item.Rating.ToString();
            ratingField.fontSize = 40;
            ratingField.margin = new Vector4(0, padding, 0, 0);

            padding += 30;
            index++;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using static ReadJSON;

public class ScoreBoard : MonoBehaviour
{
    private Transform entryContainer;
    private Transform entryTemplate;
    public ReadJSON readJSON;

    private void Awake()
    {
        entryContainer = transform.Find("Viewport/scoreboardContainer");
        entryTemplate = entryContainer.Find("scoreboardTemplate");

        entryTemplate.gameObject.SetActive(false);

        float templateHeight = 100f; // Adjust based on your design

        int index = 0;
        foreach (var item in readJSON.myRecordList.records)
        {
            Transform entryTransform = Instantiate(entryTemplate, entryContainer);
            RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>();

            // Set the anchor and pivot points to middle-center
            entryRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            entryRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            entryRectTransform.pivot = new Vector2(0.5f, 0.5f);

            // Set the anchored position and size
            entryRectTransform.anchoredPosition = new Vector2(0, -templateHeight * index);
            entryRectTransform.sizeDelta = new Vector2(entryContainer.GetComponent<RectTransform>().rect.width, templateHeight);

            entryTransform.gameObject.SetActive(true);

            // Set entry data
            TextMeshProUGUI nameField = entryTransform.Find("NameField").GetComponent<TextMeshProUGUI>();
            nameField.text = item.Name;

            // Adjust the position of the NameField
            RectTransform nameFieldRect = nameField.GetComponent<RectTransform>();
            nameFieldRect.anchorMin = new Vector2(0, 0.5f);
            nameFieldRect.anchorMax = new Vector2(0, 0.5f);
            nameFieldRect.pivot = new Vector2(0, 0.5f);
            nameFieldRect.anchoredPosition = new Vector2(10, 0); // Adjust the x and y values as needed

            TextMeshProUGUI ratingField = entryTransform.Find("RatingField").GetComponent<TextMeshProUGUI>();
            ratingField.text = item.Rating.ToString();

            // Adjust the position of the RatingField
            RectTransform ratingFieldRect = ratingField.GetComponent<RectTransform>();
            ratingFieldRect.anchorMin = new Vector2(1, 0.5f);
            ratingFieldRect.anchorMax = new Vector2(1, 0.5f);
            ratingFieldRect.pivot = new Vector2(1, 0.5f);
            ratingFieldRect.anchoredPosition = new Vector2(-10, 0); // Adjust the x and y values as needed

            if (index == 0)
            {
                // Gold
                Color goldColor;
                ColorUtility.TryParseHtmlString("#FFD700", out goldColor);
                nameField.color = goldColor;
                nameField.fontStyle = FontStyles.Normal; // Thicker for gold
                ratingField.color = goldColor;
                ratingField.fontStyle = FontStyles.Normal;
                nameField.fontSize = 65;
                ratingField.fontSize = 65;
                nameField.text = "1. " + nameField.text;
            }
            else if (index == 1)
            {
                // Silver
                Color silverColor;
                ColorUtility.TryParseHtmlString("#C0C0C0", out silverColor);
                nameField.color = silverColor;
                nameField.fontStyle = FontStyles.Normal; // Normal for silver
                ratingField.color = silverColor;
                ratingField.fontStyle = FontStyles.Normal;
                nameField.fontSize = 60;
                ratingField.fontSize = 60;
                nameField.text = "2. " + nameField.text;
            }
            else if (index == 2)
            {
                // Bronze
                Color bronzeColor;
                ColorUtility.TryParseHtmlString("#CD7F32", out bronzeColor);
                nameField.color = bronzeColor;
                nameField.fontStyle = FontStyles.Normal; // Normal for bronze
                ratingField.color = bronzeColor;
                ratingField.fontStyle = FontStyles.Normal;
                nameField.fontSize = 55;
                ratingField.fontSize = 55;
                nameField.text = "3. " + nameField.text;
            }
            else
            {
                // Default color for other entries
                nameField.color = Color.white;
                nameField.fontStyle = FontStyles.Normal;
                ratingField.color = Color.white;
                ratingField.fontStyle = FontStyles.Normal;
                nameField.fontSize = 50;
                ratingField.fontSize = 50;
            }

            index++;
        }
    }
}
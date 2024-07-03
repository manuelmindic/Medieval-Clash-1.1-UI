using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class UIUpdater : MonoBehaviour
{
    public string _gameName;

    public Player _player;
    public Bot _bot;
    public Image botImage;
    public TMP_Text _gameNameText;
    public TMP_Text _usernameText;
    public TMP_Text wonOrLostText;
    public Transform[] cardSlots;
    public Transform[] placedCardSlots;
    public Button _submitButton;
    public Button _skipButton;
    public Button _discardButton;

    public Button userStats;
    public Button botStats;
    public Button backToStart;

    public ReadJSON readJSON;


    public void BotImage(string filename)
    {
        Sprite texture = Resources.Load<Sprite>(filename);
        Image image = botImage.GetComponent<Image>();
        image.sprite = texture;
    }
    public void UpdateUserStats()
    {
        userStats.GetComponentInChildren<TMP_Text>().text = "Name: " + _player.Name +
            "\n" + "HP: " + _player.HealthPoints + "\n" + "MP: " + _player.ManaPoints +
            "\n" + "GP: " + _player.Money + "\n";
        botStats.GetComponentInChildren<TMP_Text>().text = "Name: " + _bot.Name +
            "\n" + "HP: " + _bot.HealthPoints + "\n" + "MP: " + _bot.ManaPoints +
            "\n" + "GP: " + _bot.Money + "\n";
    }

    public void UpdateImagesFromCardSlots(string filename, int index)
    {
        Texture2D texture = Resources.Load<Texture2D>(filename);
        RawImage image = cardSlots[index].GetComponent<RawImage>();
        image.texture = texture;
    }

    public void UpdateImagesFromPlacedCardSlots(string filename, int index)
    {
        Texture2D texture = Resources.Load<Texture2D>(filename);
        RawImage image = placedCardSlots[index].GetComponent<RawImage>();
        image.texture = texture;
    }

    public void pickSelectedCardForUser(int index)
    {
        List<Card> duplicatedList = _player.UserDeck;

        Card selectedCard = _player.UserDeck[index];
        //Debug.Log(_player.UserDeck[index]);
        //_player.UserDeck.Remove(selectedCard); This happens when submitted
        //UpdateImagesFromCardSlots("backcard", index);
        //cardSlots[index].gameObject.SetActive(false);
        UpdateImagesFromPlacedCardSlots(selectedCard._imageFileName, 0);
        placedCardSlots[0].gameObject.SetActive(true);
        UpdateCardSlots();
        _submitButton.gameObject.SetActive(true);
        //_skipButton.gameObject.SetActive(true);
        _discardButton.gameObject.SetActive(true);
        //Happens when submitted
        cardSlots[index].gameObject.SetActive(false);
        ChangeAllCardSlotStates(false);
        Variables.Object(placedCardSlots[0]).Set("cardIndexInUserDeck", index);
    }

    public void HandleSubmitButtonClick()
    {
        int index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
        Card selectedCard = _player.UserDeck[index];
        cardSlots[index].gameObject.SetActive(true);

        Game.hasUserPickedCard = true;
        //_player.UserDeck.Remove(selectedCard); //VLLT BRAUCH MA DAS SCHAU MA MAL //EDIT: JA WIR BRAUCHEN ES //EDIT2: NEIN WIR BRAUCHEN ES DOCH NICHT
        //placedCardSlots[0].gameObject.SetActive(false);
        _submitButton.gameObject.SetActive(false);
        //_skipButton.gameObject.SetActive(false);
        _discardButton.gameObject.SetActive(false);
        //ChangeAllCardSlotStates(true);
        //UpdateCardSlots();
        //_placedCard = selectedCard;
        //_placedCardUser = _player;
    }

    public void HandleDiscardButtonClick()
    {
        int index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
        Card card = _player.UserDeck[index];
        /*if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Attack)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack), true);
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special), true);
        }
        if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Special)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack), true);
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special), true);
        }
        if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Defense)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense), true);
        }*/

        _player.UserDeck.RemoveAt(index);
        //cardSlots[index].gameObject.SetActive(true);
        UpdateImagesFromPlacedCardSlots("backcard", 0);
        placedCardSlots[0].gameObject.SetActive(false);
        _submitButton.gameObject.SetActive(false);
        _discardButton.gameObject.SetActive(false);



        if (card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack), true);
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special), true);
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense), false);
        }

        if (card.TypeOfCard == TypeOfCard.Defense)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack), false);
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special), false);
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense), true);
        }

        UpdateCardSlots();

        //Update cardslots
    }

    public void ChangeCardSlotStates(List<int> indexes, bool isEnabled)
    {
        foreach (var index in indexes)
        {
            RawImage image = cardSlots[index].GetComponent<RawImage>();
            EventTrigger eventTrigger = cardSlots[index].gameObject.GetComponent<EventTrigger>();
            if (!isEnabled)
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0.5f);
                eventTrigger.enabled = false;
            }
            else
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
                eventTrigger.enabled = true;
            }
        }
    }

    public void ChangeAllCardSlotStates(bool isEnabled)
    {
        for (int i = 0; i < _player.UserDeck.Count; i++)
        {
            RawImage image = cardSlots[i].GetComponent<RawImage>();
            EventTrigger eventTrigger = cardSlots[i].gameObject.GetComponent<EventTrigger>();
            if (!isEnabled)
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0.5f);
                eventTrigger.enabled = false;
            }
            else
            {
                image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
                eventTrigger.enabled = true;
            }
        }
    }
    public List<int> GetIndexesForCardType(TypeOfCard typeOfCard)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < _player.UserDeck.Count; i++)
        {
            if (_player.UserDeck[i].TypeOfCard == typeOfCard)
                result.Add(i);
        }

        return result;
    }

    public void removeSelectedCardForUser()
    {
        int index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
        placedCardSlots[0].gameObject.SetActive(false);
        cardSlots[index].gameObject.SetActive(true);
        _submitButton.gameObject.SetActive(false);
        //_skipButton.gameObject.SetActive(false);
        _discardButton.gameObject.SetActive(false);

        if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Attack)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack), true);
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special), true);
        }
        if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Special)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack), true);
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special), true);
        }
        if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Defense)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense), true);
        }
        //
    }

    public void UpdateCardSlots()
    {
        //SCUFFED LÖSUNG :SOB:
        if (_player.UserDeck.Count == 7)
        {
            cardSlots[_player.UserDeck.Count - 1].gameObject.SetActive(false);
        }
        else
        {
            cardSlots[_player.UserDeck.Count].gameObject.SetActive(false);
        }

        for (int i = 0; i < _player.UserDeck.Count; i++)
        {
            UpdateImagesFromCardSlots(_player.UserDeck[i].ImageFileName, i);
            //if (i + 1 == _player.UserDeck.Count) 
            cardSlots[i].gameObject.SetActive(true);
        }
    }


}

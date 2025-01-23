using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
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
    public Image _playerImage;
    public TMP_Text _gameNameText;
    public TMP_Text _usernameText;
    public TMP_Text wonOrLostText;
    public TMP_Text _turnText;
    public Transform[] cardSlots;
    public Transform[] botCardSlots;
    public Transform[] placedCardSlots;
    public Button _submitButton;
    public Button _skipButton;
    public Button _discardButton;

    public bool _isMoving = false;
    public bool _skipTurn = false;
    public Button userStats;
    public Button botStats;
    public Button backToStart;
    public ReadJSON readJSON;
    public RectTransform witchImage;
    public float speed;
    public Vector2 startPosition;
    public Vector2 endPosition;
    public Texture2D defaultCursor;
    public Texture2D discardCursor;

    private bool _isDiscardMode = false;

    public void MoveWitch()
    {
        _isMoving = true;
        RectTransform rectTransform = witchImage.GetComponent<RectTransform>();
        rectTransform.gameObject.SetActive(true);

        // Update the position
        rectTransform.anchoredPosition = Vector2.MoveTowards(rectTransform.anchoredPosition, endPosition, speed * Time.deltaTime);


        if (rectTransform.anchoredPosition == endPosition)
        {
            _isMoving = false;
            rectTransform.gameObject.SetActive(false);
            rectTransform.anchoredPosition = startPosition;
        }
        //image.gameObject.SetActive(false);
    }

    public void UpdateUserProfilePicture()
    {
        Sprite texture = Resources.Load<Sprite>(PlayerPrefs.GetString("ProfilePicture", "profilePicture1"));
        Image image = _playerImage.GetComponent<Image>();
        Debug.Log("hi");
        image.sprite = texture;
    }

    void Update()
    {
        if (_isMoving)
        {
            MoveWitch();
        }
    }

    public void BotImage(string filename)
    {
        Sprite texture = Resources.Load<Sprite>(filename);
        Image image = botImage.GetComponent<Image>();
        image.sprite = texture;
    }
    public void SetTurnText(int turn)
    {
        _turnText.SetText("Turn: " + turn);
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

    public void UpdateImagesFromCardSlots(string filename, int index, string role)
    {
        if (role == "player")
        {
            Texture2D texture = Resources.Load<Texture2D>(filename);
            RawImage image = cardSlots[index].GetComponent<RawImage>();
            image.texture = texture;
        }
        if (role == "bot")
        {
            Texture2D texture = Resources.Load<Texture2D>(filename);
            RawImage image = botCardSlots[index].GetComponent<RawImage>();
            image.texture = texture;
        }
    }

    public void UpdateImagesFromPlacedCardSlots(string filename, int index)
    {
        Texture2D texture = Resources.Load<Texture2D>(filename);
        RawImage image = placedCardSlots[index].GetComponent<RawImage>();
        image.texture = texture;
    }

    public void pickSelectedCardForUser(int index)
    {
        if (!_isDiscardMode)
        {
            Card selectedCard = _player.UserDeck[index];
            UpdateImagesFromPlacedCardSlots(selectedCard._imageFileName, 0);
            placedCardSlots[0].gameObject.SetActive(true);
            UpdateCardSlots("player");
            _submitButton.gameObject.SetActive(true);
            //_skipButton.gameObject.SetActive(true);
            _discardButton.gameObject.SetActive(true);
            //Happens when submitted
            cardSlots[index].gameObject.SetActive(false);
            ChangeAllCardSlotStates(false, "player");
            Variables.Object(placedCardSlots[0]).Set("cardIndexInUserDeck", index);
        }
        
        else
        {
            Card card = _player.UserDeck[index];
            _player.UserDeck.RemoveAt(index);

            /*if (card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special)
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
            }*/

            if (Game._placedCard == null)
            {
                ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack, "player"), true, "player");
                ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special, "player"), true, "player");
                ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense, "player"), false, "player");
            }
            else
            {
                ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack, "player"), false, "player");
                ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special, "player"), false, "player");
                ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense, "player"), true, "player");
            }
            _isDiscardMode = false;
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
            UpdateCardSlots("player");
            _discardButton.gameObject.SetActive(true);
            _skipButton.gameObject.SetActive(true);
        }
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
        _skipButton.gameObject.SetActive(false);
        _discardButton.gameObject.SetActive(false);
        //ChangeAllCardSlotStates(true);
        //UpdateCardSlots();
        //_placedCard = selectedCard;
        //_placedCardUser = _player;
    }

    public void HandleDiscardButtonClick()
    {
        ChangeAllCardSlotStates(true, "player");
        Cursor.SetCursor(discardCursor, Vector2.zero, CursorMode.Auto);
        _isDiscardMode = !_isDiscardMode;
        _submitButton.gameObject.SetActive(false);
        _skipButton.gameObject.SetActive(false);
        _discardButton.gameObject.SetActive(false);
        //Update cardslots
    }

    public void ChangeCardSlotStates(List<int> indexes, bool isEnabled, string role)
    {
        foreach (var index in indexes)
        {
            if (role == "player")
            {
                RawImage image = cardSlots[index].GetComponent<RawImage>();
                EventTrigger eventTrigger = cardSlots[index].gameObject.GetComponent<EventTrigger>(); // karte kann nicht gepicked werden
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
            if (role == "bot")
            {
                RawImage image = botCardSlots[index].GetComponent<RawImage>();
                EventTrigger eventTrigger = botCardSlots[index].gameObject.GetComponent<EventTrigger>(); // karte kann nicht gepicked werden
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
    }

    public void ChangeAllCardSlotStates(bool isEnabled, string role)
    {
        if (role == "player")
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
        if (role == "bot")
        {
            for (int i = 0; i < _bot.UserDeck.Count; i++)
            {
                RawImage image = botCardSlots[i].GetComponent<RawImage>();
                EventTrigger eventTrigger = botCardSlots[i].gameObject.GetComponent<EventTrigger>();
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
        
    }

    public void ChangeAllCardSlotStatesToFullDark(string role)
    {
        if (role == "player")
        {
            for (int i = 0; i < _player.UserDeck.Count; i++)
            {
                RawImage image = cardSlots[i].GetComponent<RawImage>();
                EventTrigger eventTrigger = cardSlots[i].gameObject.GetComponent<EventTrigger>();
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
                eventTrigger.enabled = false;

            }
        }
        if (role == "bot")
        {
            for (int i = 0; i < _bot.UserDeck.Count; i++)
            {
                RawImage image = botCardSlots[i].GetComponent<RawImage>();
                EventTrigger eventTrigger = botCardSlots[i].gameObject.GetComponent<EventTrigger>();
                image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
                eventTrigger.enabled = false;
            }
        }

    }

    public List<int> GetIndexesForCardType(TypeOfCard typeOfCard, string role)
    {
        List<int> result = new List<int>();
        if (role == "player")
        {
            for (int i = 0; i < _player.UserDeck.Count; i++)
            {
                if (_player.UserDeck[i].TypeOfCard == typeOfCard)
                    result.Add(i);
            }
        }
        if(role == "bot")
        {
            for (int i = 0; i < _bot.UserDeck.Count; i++)
            {
                if (_bot.UserDeck[i].TypeOfCard == typeOfCard)
                    result.Add(i);
            }
        }
        return result;
    }

    public void handleSkipButton()
    {
        UpdateImagesFromPlacedCardSlots("backcard", 0);
        placedCardSlots[0].gameObject.SetActive(true);
        ChangeAllCardSlotStates(false, "player");
        _skipTurn = true;
        Game.hasUserPickedCard = true;
        _skipButton.gameObject.SetActive(false);
        _discardButton.gameObject.SetActive(false);
        _submitButton.gameObject.SetActive(false);
    }

    public void handleSkipBot()
    {
        UpdateImagesFromPlacedCardSlots("backcard", 1);
        placedCardSlots[1].gameObject.SetActive(true);
        ChangeAllCardSlotStates(false, "bot");
        _skipTurn = true;
        //Game.hasUserPickedCard = true;
    }

    public void removeSelectedCardForUser()
    {
        int index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
        placedCardSlots[0].gameObject.SetActive(false);
        cardSlots[index].gameObject.SetActive(true);
        _submitButton.gameObject.SetActive(false);
        //_skipButton.gameObject.SetActive(false);
        _discardButton.gameObject.SetActive(true);

        if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Attack)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack, "player"), true, "player");
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special, "player"), true, "player");
        }
        if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Special)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Attack, "player"), true, "player");
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Special, "player"), true, "player");
        }
        if (_player.UserDeck[index].TypeOfCard == TypeOfCard.Defense)
        {
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense, "player"), true, "player");
        }
    }

    public void UpdateCardSlots(string role)
    {
        if (role == "player")
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
                UpdateImagesFromCardSlots(_player.UserDeck[i].ImageFileName, i, role);
                //if (i + 1 == _player.UserDeck.Count) 
                cardSlots[i].gameObject.SetActive(true);
            }
        }
        if (role == "bot")
        {
            //SCUFFED LÖSUNG :SOB:
            if (_bot.UserDeck.Count == 7)
            {
                botCardSlots[_bot.UserDeck.Count - 1].gameObject.SetActive(false);
            }
            else
            {
                botCardSlots[_bot.UserDeck.Count].gameObject.SetActive(false);
            }

            for (int i = 0; i < _bot.UserDeck.Count; i++)
            {
                UpdateImagesFromCardSlots(_bot.UserDeck[i].ImageFileName, i, role);
                //if (i + 1 == _player.UserDeck.Count) 
                botCardSlots[i].gameObject.SetActive(true);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine.Windows;
using UnityEngine.SceneManagement;




public class Game : MonoBehaviour
{
    public string _gameName;
    public GameDeck _deck;
    public Boolean _finished;

    public Player _player;
    public Bot _bot;

    public Card _placedCard;
    public User _placedCardUser;

    public TMP_Text _gameNameText;
    public TMP_Text _usernameText;
    public TMP_Text wonOrLostText;
    public Transform[] cardSlots;
    public Transform[] placedCardSlots;
    public Button _submitButton;
    private bool hasUserPickedCard = false;
    public Button userStats;
    public Button botStats;
    public Button backToStart;

    public ReadJSON readJSON;


    public Game(string gameName, Player player, Bot bot)
    {
        _gameName = gameName;
        _finished = false;
        _player = player;
        _bot = bot;
    }

    private void Start()
    {
        StartCoroutine(StartGame());
        //PlayGame();
    }

    IEnumerator StartGame()
    {
        _player.Name = PlayerPrefs.GetString("Username", "Player 1");
        UpdateUserStats();
        _gameNameText.SetText(_gameName);
        _usernameText.SetText(_player.Name);
        _deck.Shuffle();
        for (int i = 0; i < 5; i++)
        {
            Card drawnCard = _deck.DrawCard();
            _player.UserDeck.Add(drawnCard);
            UpdateImagesFromCardSlots(drawnCard.ImageFileName, _player.UserDeck.Count - 1);
            cardSlots[_player.UserDeck.Count - 1].gameObject.SetActive(true);
            _bot.UserDeck.Add(_deck.DrawCard());
        }

        while (!_finished)
        {
            //DisableCardSlot(0);
            //Bug: Wenn man 7 Karten hat crashed es :(
            //Bug: Manchmal sind die Karten die in den Slots angezeigt werden out of sync, also die Falsche Textur wird wahrscheinlich geladen
            ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense), false); //BEEP
            yield return StartCoroutine(CheckIfUserHasPlacedCard());
            int index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
            Debug.Log("User card: " + _player.UserDeck[index]);
            PlaceCard(_player, _player.UserDeck[index]);
            UpdateCardSlots();

            yield return StartCoroutine(WaitSeconds(3f));
            StopCoroutine(WaitSeconds(3f));

            Card botCard = getBotCard(_bot, TypeOfCard.Defense);
            Debug.Log("Bot Counter: " + botCard);
            UpdateImagesFromPlacedCardSlots(botCard.ImageFileName, 1);
            placedCardSlots[1].gameObject.SetActive(true);
            PlaceCounter(_bot, botCard);
            

            yield return StartCoroutine(WaitSeconds(5f));
            StopCoroutine(WaitSeconds(5f));

            ChangeAllCardSlotStates(true);
            UpdateUserStats();
            hasUserPickedCard = false;
            if (checkIfWon(_player, _bot))
                break;
            StopCoroutine(CheckIfUserHasPlacedCard());

            UpdateImagesFromPlacedCardSlots("backcard", 0);
            UpdateImagesFromPlacedCardSlots("backcard", 1);
            placedCardSlots[0].gameObject.SetActive(false);
            placedCardSlots[1].gameObject.SetActive(false);

            _player.UserDeck.Add(_deck.DrawCard());
            _bot.UserDeck.Add(_deck.DrawCard());


            ChangeAllCardSlotStates(true);
            //ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense), true); //BEEP
            UpdateCardSlots();



            //New Turn (BOT FIRST)
            yield return StartCoroutine(WaitSeconds(3f));
            StopCoroutine(WaitSeconds(3f));

            botCard = getBotCard(_bot, TypeOfCard.Attack);
            Debug.Log("Bot Card: " + botCard);
            PlaceCard(_bot, botCard);
            UpdateImagesFromPlacedCardSlots(botCard.ImageFileName, 1);
            placedCardSlots[1].gameObject.SetActive(true);
            index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");

            yield return StartCoroutine(CheckIfUserHasPlacedCard());
            Debug.Log("User Counter: " + _player.UserDeck[index]);
            PlaceCounter(_player, _player.UserDeck[index]);
            UpdateCardSlots();

            yield return StartCoroutine(WaitSeconds(5f));
            StopCoroutine(WaitSeconds(5f));

            UpdateUserStats();

            checkIfWon(_player, _bot);
            StopCoroutine(CheckIfUserHasPlacedCard());

            UpdateImagesFromPlacedCardSlots("backcard", 1);
            UpdateImagesFromPlacedCardSlots("backcard", 0);
            placedCardSlots[0].gameObject.SetActive(false);
            placedCardSlots[1].gameObject.SetActive(false);

            _player.UserDeck.Add(_deck.DrawCard());
            _bot.UserDeck.Add(_deck.DrawCard());

            UpdateCardSlots();

            hasUserPickedCard = false;

        }
        wonOrLostText.gameObject.SetActive(true);
        backToStart.gameObject.SetActive(true);
        Debug.Log("HEEEEEEEYYYYYY");
        //yield return StartCoroutine(CheckIfUserHasPlacedCard());
    }

    IEnumerator CheckIfUserHasPlacedCard()
    {
        yield return new WaitUntil(() => hasUserPickedCard == true);
        Debug.Log("Placed Card!");
    }

    IEnumerator WaitSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    private void UpdateUserStats()
    {
        userStats.GetComponentInChildren<TMP_Text>().text = "Name: " + _player.Name +
            "\n" + "HP: " + _player.HealthPoints + "\n" + "MP: " + _player.ManaPoints +
            "\n" + "GP: " + _player.Money + "\n";
        botStats.GetComponentInChildren<TMP_Text>().text = "Name: " + _bot.Name +
            "\n" + "HP: " + _bot.HealthPoints + "\n" + "MP: " + _bot.ManaPoints +
            "\n" + "GP: " + _bot.Money + "\n";
    }

    private void UpdateImagesFromCardSlots(string filename, int index)
    {
        Texture2D texture = Resources.Load<Texture2D>(filename);
        RawImage image = cardSlots[index].GetComponent<RawImage>();
        image.texture = texture;
    }

    private void UpdateImagesFromPlacedCardSlots(string filename, int index)
    {
        Texture2D texture = Resources.Load<Texture2D>(filename);
        RawImage image = placedCardSlots[index].GetComponent<RawImage>();
        image.texture = texture;
    }

    public void PlayGame()
    {
        //_deck = new GameDeck(assignCards());
        //_deck.Shuffle();

        for (int i = 0; i < 5; i++)
        {
            _player.UserDeck.Add(_deck.DrawCard());
            _bot.UserDeck.Add(_deck.DrawCard());
        }

        while (!_finished)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Player Cards: \n");
            printUserCards(_player);
            Console.WriteLine("----------------");

            Console.WriteLine("Pick your Card or `skip´: ");
            string input = Console.ReadLine(); // edit input so you only place attack or special, if placing defense say something like "try again" and let player pick new card

            if (input == "skip" && !_player.UserDeck.Any(c => c.TypeOfCard == TypeOfCard.Attack))
            {
                PlaceCard(_player, SkipAttack());
            }
            if (input == "skip" && _player.UserDeck.Any(c => c.TypeOfCard == TypeOfCard.Attack))
            {
                while (input == "skip")
                {
                    Console.WriteLine("You cannot skip this turn, you have suitable cards. Try again:");
                    input = Console.ReadLine();
                }
            }
            if (input != "skip")
            {
                Console.WriteLine("Your Card is:\n" + _player.UserDeck[Convert.ToInt32(input)].ToString());
                PlaceCard(_player, _player.UserDeck[Convert.ToInt32(input)]);
            }


            Console.ForegroundColor = ConsoleColor.Green;
            printPlacedView();

            Console.ForegroundColor = ConsoleColor.White;
            PlaceCounter(_bot, getBotCard(_bot, TypeOfCard.Defense));
            printUserStats(_bot);

            if (checkIfWon(_player, _bot))
            {
                break;
            }

            _player.UserDeck.Add(_deck.DrawCard());
            _bot.UserDeck.Add(_deck.DrawCard());

            PlaceCard(_bot, getBotCard(_bot, TypeOfCard.Attack));
            printPlacedView();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Player Cards: \n");
            printUserCards(_player);
            Console.WriteLine("----------------");
            Console.WriteLine("Pick your Defense Card or `skip´: ");
            string defenseInput = Console.ReadLine();
            if (defenseInput == "skip")
            {
                SkipDefense(_player);
            }
            else
            {
                PlaceCounter(_player, _player.UserDeck[Convert.ToInt32(defenseInput)]);
            }
            printUserStats(_player);

            _player.UserDeck.Add(_deck.DrawCard());
            _bot.UserDeck.Add(_deck.DrawCard());

            checkIfWon(_player, _bot);
        }
    }

    public bool checkIfWon(User user1, User user2)
    {
        if (user1.HealthPoints <= 0)
        {
            wonOrLostText.SetText(user2.Name + " won the game! You Lost!");
            _finished = true;
            return true;
        }
        if (user2.HealthPoints <= 0)
        {
            wonOrLostText.SetText(user1.Name + " won the game! Congrats!");
            _finished = true;
            return true;
        }
        return false;
    }

    public void BackToStart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }


    public Card getBotCard(User user, TypeOfCard typeOfCard)
    {
        Card pickedCard;
        if (typeOfCard.Equals(TypeOfCard.Attack) && !user.UserDeck.Any(c => c.TypeOfCard == typeOfCard)
            && !user.UserDeck.Any(c => c.TypeOfCard == TypeOfCard.Sell)
            && !user.UserDeck.Any(c => c.TypeOfCard == TypeOfCard.Special)
            && !user.UserDeck.Any(c => c.TypeOfCard == TypeOfCard.Buy))
        {
            Console.WriteLine("Manu skipped Attack");
            pickedCard = SkipAttack();
            return pickedCard;
        }
        if (typeOfCard.Equals(TypeOfCard.Defense) && !user.UserDeck.Any(c => c.TypeOfCard == typeOfCard))
        {
            Console.WriteLine("Manu mein decko skipped defense");
            pickedCard = SkipAttack();
            return pickedCard;
        }
        if (typeOfCard.Equals(TypeOfCard.Attack))
        {
            pickedCard = user.UserDeck.Where(item => item.TypeOfCard.Equals(TypeOfCard.Attack) || item.TypeOfCard.Equals(TypeOfCard.Special)).First();
            Console.WriteLine(pickedCard);
            return pickedCard;
        }

        // check if bot has this typeOfCard in Deck!!! -> !NullPointerException
        // boolean as skip to not play turn and draw a card
        pickedCard = user.UserDeck.Where(item => item.TypeOfCard.Equals(typeOfCard)).First();
        Console.WriteLine(pickedCard);
        return pickedCard;
    }

    public void printUserCards(User user)
    {
        int inc = 0;
        foreach (var card in user.UserDeck)
        {
            Console.WriteLine(inc + ". " + card.ToString());
            inc++;
        }
    }

    public void printPlacedView()
    {
        Console.WriteLine();
        Console.WriteLine("----- The Current Turn Stack ----");
        Console.WriteLine(_placedCard.ToString());
        printUserStats(_placedCardUser);
        Console.WriteLine("---- End Of Current Turn Stack ----");
        Console.WriteLine();
    }

    public void printUserStats(User user)
    {
        if (user.HealthPoints < 0)
        {
            user.HealthPoints = 0;
        }
        else if (user.ManaPoints < 0)
        {
            user.ManaPoints = 0;
        }
        else if (user.Money < 0)
        {
            user.Money = 0;
        }

        Debug.Log(user.Name + " current stats are:");
        Debug.Log("HP: " + user.HealthPoints);
        Debug.Log("MP: " + user.ManaPoints);
        Debug.Log("Money: " + user.Money);
    }

    public void printSelectedCard(int index)
    {
        Debug.Log(_player.UserDeck[index]);
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

        hasUserPickedCard = true;
        //_player.UserDeck.Remove(selectedCard); //VLLT BRAUCH MA DAS SCHAU MA MAL //EDIT: JA WIR BRAUCHEN ES 
        //placedCardSlots[0].gameObject.SetActive(false);
        _submitButton.gameObject.SetActive(false);
        //ChangeAllCardSlotStates(true);
        //UpdateCardSlots();
        //_placedCard = selectedCard;
        //_placedCardUser = _player;
    }

    private void ChangeCardSlotStates(List<int> indexes, bool isEnabled)
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

    private void ChangeAllCardSlotStates(bool isEnabled)
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

    private List<int> GetIndexesForCardType(TypeOfCard typeOfCard)
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
        cardSlots[_player.UserDeck.Count].gameObject.SetActive(false);
        for (int i = 0; i < _player.UserDeck.Count; i++)
        {
            UpdateImagesFromCardSlots(_player.UserDeck[i].ImageFileName, i);
            if (i + 1 == _player.UserDeck.Count) 
                cardSlots[i].gameObject.SetActive(true);
        }
    }

    public void LeaveGame()
    {

    }

    public void PlaceCard(User user, Card card)
    {
        //TODO in input das man nur attack oder special setzen kann
        user.UserDeck.Remove(card);

        if (_placedCard == null && _placedCardUser == null)
        {
            _placedCard = card;
            _placedCardUser = user;
        }


        if (card.TypeOfCard.Equals(TypeOfCard.Special))
        {
            Match match = Regex.Match(card.Name, @"(\D+)(\d+)");

            string prefix = match.Groups[1].Value;
            int number = int.Parse(match.Groups[2].Value);

            // SELL and BUY needs to be added
            if (prefix == "HP")
            {
                user.HealthPoints += number;
            }
            if (prefix == "MP")
            {
                user.ManaPoints += number;
            }
            if (prefix == "GP")
            {
                user.Money += number;
            }

            _placedCard = card;
            _placedCardUser = user;
        }
    }


    public void PlaceCounter(User user, Card card)
    {
        if (_placedCard != null && _placedCardUser != null && _placedCard.TypeOfCard.Equals(TypeOfCard.Attack))
        {
            user.UserDeck.Remove(card);
            if (_placedCard.Damage - card.Defense > 0)
            {
                user.HealthPoints -= (_placedCard.Damage - card.Defense);
            }
        }

        _placedCard = null;
        _placedCardUser = null;
    }

    public void RemoveCard(User user, Card card)
    {
        user.UserDeck.Remove(card);
    }

    public Card SkipAttack()
    {
        //Small warning when this happens, prob polish :)
        Card skipCard = new Card("Placeholder", "Skip", 0, TypeOfCard.Skip, 0, 0, 0);

        return skipCard;
    }

    public void SkipDefense(User user)
    {
        user.HealthPoints -= _placedCard.Damage;
        _placedCard = null;
        _placedCardUser = null;
    }



    private List<Card> assignCards()
    {
        List<Card> deck = new List<Card>
            {
                new Card("Placeholder", "Att1", 3, TypeOfCard.Attack, 5, 0, 0),
                new Card("Placeholder", "Att2", 7, TypeOfCard.Attack, 10, 0, 0),
                new Card("Placeholder", "Att3", 11, TypeOfCard.Attack, 15, 0, 0),
                new Card("Placeholder", "Att4", 6, TypeOfCard.Attack, 3, 0, 0),
                new Card("Placeholder", "Att5", 8, TypeOfCard.Attack, 7, 0, 0),
                new Card("Placeholder", "Att6", 13, TypeOfCard.Attack, 12, 0, 0),
                new Card("Placeholder", "Att7", 10, TypeOfCard.Attack, 9, 0, 0),
                new Card("Placeholder", "Att8", 4, TypeOfCard.Attack, 5, 0, 0),
                new Card("Placeholder", "Att9", 14, TypeOfCard.Attack, 2, 0, 0),
                new Card("Placeholder", "Att10", 9, TypeOfCard.Attack, 7, 0, 0),
                new Card("Placeholder", "Att11", 15, TypeOfCard.Attack, 13, 0, 0),
                new Card("Placeholder", "Att12", 5, TypeOfCard.Attack, 15, 0, 0),
                new Card("Placeholder", "Att13", 12, TypeOfCard.Attack, 11, 0, 0),
                new Card("Placeholder", "Att14", 30, TypeOfCard.Attack, 30, 0, 0), // Rare card
                new Card("Placeholder", "Def1", 2, TypeOfCard.Defense, 0, 3, 0),
                new Card("Placeholder", "Def2", 8, TypeOfCard.Defense, 0, 7, 0),
                new Card("Placeholder", "Def3", 14, TypeOfCard.Defense, 0, 11, 0),
                new Card("Placeholder", "Def4", 7, TypeOfCard.Defense, 0, 4, 0),
                new Card("Placeholder", "Def5", 9, TypeOfCard.Defense, 0, 8, 0),
                new Card("Placeholder", "Def6", 13, TypeOfCard.Defense, 0, 9, 0),
                new Card("Placeholder", "Def7", 12, TypeOfCard.Defense, 0, 15, 0),
                new Card("Placeholder", "Def8", 3, TypeOfCard.Defense, 0, 2, 0),
                new Card("Placeholder", "Def9", 11, TypeOfCard.Defense, 0, 14, 0),
                new Card("Placeholder", "Def10", 10, TypeOfCard.Defense, 0, 12, 0),
                new Card("Placeholder", "Def11", 15, TypeOfCard.Defense, 0, 13, 0),
                new Card("Placeholder", "Def12", 4, TypeOfCard.Defense, 0, 5, 0),
                new Card("Placeholder", "Def13", 15, TypeOfCard.Defense, 0, 15, 0),
                new Card("Placeholder", "Def14", 30, TypeOfCard.Defense, 0, 30, 0), // Rare card
                new Card("Placeholder", "MP15", 20, TypeOfCard.Special, 0, 0, 0),
                new Card("Placeholder", "MP10", 20, TypeOfCard.Special, 0, 0, 0),
                new Card("Placeholder", "MP20", 20, TypeOfCard.Special, 0, 0, 0),
                new Card("Placeholder", "HP10", 20, TypeOfCard.Special, 0, 0, 0),
                new Card("Placeholder", "HP15", 20, TypeOfCard.Special, 0, 0, 0),
                new Card("Placeholder", "HP20", 20, TypeOfCard.Special, 0, 0, 0),
                new Card("Placeholder", "GP10", 20, TypeOfCard.Special, 0, 0, 0),
                new Card("Placeholder", "GP30", 20, TypeOfCard.Special, 0, 0, 0)
            };

        return deck;
    }
}


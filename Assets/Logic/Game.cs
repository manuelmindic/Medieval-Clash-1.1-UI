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
using Unity.Mathematics;

public class Game : MonoBehaviour
{
    public string _gameName;
    public GameDeck _deck;
    public Boolean _finished;

    public Player _player;
    public Bot _bot;

    public static Card _placedCard;
    public static User _placedCardUser;
    public Button backButton;

    public Card _skipCard;
    public GameObject winLosePanel;
    public TMP_Text _gameNameText;
    public TMP_Text _usernameText;
    public TMP_Text wonOrLostText;
    public Transform[] cardSlots;
    public Transform[] placedCardSlots;
    public Button _submitButton;
    public Button _skipButton;
    public Button _discardButton;
    public static bool hasUserPickedCard = false;
    public Button userStats;
    public Button botStats;
    public Button backToStart;

    public AudioSource _audio;
    public UIUpdater _uiUpdater;
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
        int turn = 1;
        _player.Name = PlayerPrefs.GetString("Username", "Player 1");
        userStatsLogic(_player);
        _uiUpdater.UpdateUserStats();
        _gameNameText.SetText(_gameName);
        _usernameText.SetText(_player.Name);
        _uiUpdater.SetTurnText(turn);
        _uiUpdater.UpdateUserProfilePicture();
        _deck.Shuffle();
        for (int i = 0; i < 5; i++)
        {
            Card drawnCard = _deck.DrawCard();
            _player.UserDeck.Add(drawnCard);
            _uiUpdater.UpdateImagesFromCardSlots(drawnCard.ImageFileName, _player.UserDeck.Count - 1);
            drawnCard = _deck.DrawCard();
            cardSlots[_player.UserDeck.Count - 1].gameObject.SetActive(true);
            _bot.UserDeck.Add(drawnCard);
        }

        while (!_finished)
        {
            printDef(_bot);
            //DisableCardSlot(0);
            //Bug: Wenn man 7 Karten hat crashed es :(
            //Bug: Manchmal sind die Karten die in den Slots angezeigt werden out of sync, also die Falsche Textur wird wahrscheinlich geladen
            _discardButton.gameObject.SetActive(true);
            _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Defense), false); //BEEP
            yield return StartCoroutine(CheckIfUserHasPlacedCard());
            int index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
            Debug.Log("User card: " + _player.UserDeck[index]);
            if (_uiUpdater._skipTurn){
                PlaceCard(_player, _skipCard);
            }
            else
            {
                PlaceCard(_player, _player.UserDeck[index]);
            }
            _uiUpdater._skipTurn = false;
            _uiUpdater.UpdateCardSlots();
            _uiUpdater.BotImage("AngryBot");

            yield return StartCoroutine(WaitSeconds(2f));
            StopCoroutine(WaitSeconds(2f));

            _uiUpdater.BotImage("ThinkingBot");

            yield return StartCoroutine(WaitSeconds(2f));
            StopCoroutine(WaitSeconds(2f));

            Card botCard = getBotCard(_bot, TypeOfCard.Defense);
            Debug.Log("Bot Counter: " + botCard);
            _uiUpdater.UpdateImagesFromPlacedCardSlots(botCard.ImageFileName, 1);
            placedCardSlots[1].gameObject.SetActive(true);
            PlaceCounter(_bot, botCard);

            _uiUpdater.BotImage("DefaultBot");

            yield return StartCoroutine(WaitSeconds(5f));
            StopCoroutine(WaitSeconds(5f));

            _uiUpdater.ChangeAllCardSlotStates(true);
            userStatsLogic(_bot);
            _uiUpdater.UpdateUserStats();
            hasUserPickedCard = false;
            if (checkIfWon(_player, _bot))
                break;
            StopCoroutine(CheckIfUserHasPlacedCard());

            _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 0);
            _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 1);
            placedCardSlots[0].gameObject.SetActive(false);
            placedCardSlots[1].gameObject.SetActive(false);

            if(_player.UserDeck.Count != 7) 
                _player.UserDeck.Add(_deck.DrawCard());
            if (_bot.UserDeck.Count != 7)
                _bot.UserDeck.Add(_deck.DrawCard());

            turn++;
            _uiUpdater.SetTurnText(turn);
            RandomWitchTurn(turn);

            _uiUpdater.ChangeAllCardSlotStates(false);
            //ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense), true); //BEEP
            _uiUpdater.UpdateCardSlots();

            _uiUpdater.BotImage("ThinkingBot");
            //New Turn (BOT FIRST)
            printDef(_bot);
            yield return StartCoroutine(WaitSeconds(2f));
            StopCoroutine(WaitSeconds(2f));

            _discardButton.gameObject.SetActive(true);
            botCard = getBotCard(_bot, TypeOfCard.Attack);
            Debug.Log("Bot Card: " + botCard);
            PlaceCard(_bot, botCard);

            _uiUpdater.BotImage("DefaultBot");

            _uiUpdater.UpdateImagesFromPlacedCardSlots(botCard.ImageFileName, 1);
            placedCardSlots[1].gameObject.SetActive(true);
            _uiUpdater.ChangeAllCardSlotStates(true);
            _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Attack), false);
            _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Special), false);
            //index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");

            yield return StartCoroutine(CheckIfUserHasPlacedCard());
            index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
            Debug.Log("User Counter: " + _player.UserDeck[index]);
            if (_uiUpdater._skipTurn)
            {
                PlaceCounter(_player, _skipCard);
            }
            else
            {
                PlaceCounter(_player, _player.UserDeck[index]);
            }
            _uiUpdater._skipTurn = false;
            _uiUpdater.UpdateCardSlots();

            yield return StartCoroutine(WaitSeconds(5f));
            StopCoroutine(WaitSeconds(5f));


            _uiUpdater.ChangeAllCardSlotStates(true);
            userStatsLogic(_player);
            _uiUpdater.UpdateUserStats();

            checkIfWon(_player, _bot);
            StopCoroutine(CheckIfUserHasPlacedCard());

            _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 1);
            _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 0);
            placedCardSlots[0].gameObject.SetActive(false);
            placedCardSlots[1].gameObject.SetActive(false);

            if (_player.UserDeck.Count != 7)
                _player.UserDeck.Add(_deck.DrawCard());
            if (_bot.UserDeck.Count != 7)
                _bot.UserDeck.Add(_deck.DrawCard());

            turn++;
            _uiUpdater.SetTurnText(turn);
            RandomWitchTurn(turn);
            _uiUpdater.ChangeAllCardSlotStates(true);
            _uiUpdater.UpdateCardSlots();

            hasUserPickedCard = false;
        }
        wonOrLostText.gameObject.SetActive(true);
        backToStart.gameObject.SetActive(true);
        winLosePanel.gameObject.SetActive(true);
        backButton.gameObject.SetActive(false);
        _skipButton.gameObject.SetActive(false);
        _submitButton.gameObject.SetActive(false);
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
            userStatsLogic(_bot);

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
            userStatsLogic(_player);

            _player.UserDeck.Add(_deck.DrawCard());
            _bot.UserDeck.Add(_deck.DrawCard());

            checkIfWon(_player, _bot);
        }
    }

    public bool checkIfWon(User user1, User user2)
    {
        if (user1.HealthPoints <= 0)
        {
            _uiUpdater.BotImage("WinBot");
            wonOrLostText.SetText(user2.Name + " won the game! You Lost!");
            user1.Rating -= 25; //User Lost
            readJSON.UpdateRecord(user1.Name, -25);
            _finished = true;
            return true;
        }
        if (user2.HealthPoints <= 0)
        {
            _uiUpdater.BotImage("DefeatedBot");
            wonOrLostText.SetText(user1.Name + " won the game! Congrats!");
            user1.Rating += 25; //User Won
            readJSON.UpdateRecord(user1.Name, 25);
            _finished = true;
            return true;
        }
        return false;
    }

    public void BackToStart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public void printDef(User user)
    {
        foreach (var item in user.UserDeck)
        {
            Debug.Log(item.Defense);
        }
    }

    public Card getBotCard(User user, TypeOfCard typeOfCard)
    {
        Card _botcard = null;
        if (typeOfCard.Equals(TypeOfCard.Attack) && !user.UserDeck.Any(c => c.TypeOfCard == typeOfCard)
            && !user.UserDeck.Any(c => c.TypeOfCard == TypeOfCard.Sell)
            && !user.UserDeck.Any(c => c.TypeOfCard == TypeOfCard.Special)
            && !user.UserDeck.Any(c => c.TypeOfCard == TypeOfCard.Buy))
        {
            Console.WriteLine("Bot skipped Attack");
            _botcard = SkipAttack();
            return _botcard;
        }
        if (typeOfCard.Equals(TypeOfCard.Defense) && !user.UserDeck.Any(c => c.TypeOfCard == typeOfCard))
        {
            Console.WriteLine("Bot skipped Defense");
            _botcard = SkipDefense(user);
            return _botcard;
        }

        // Die Manu´sche Formel
        if (typeOfCard.Equals(TypeOfCard.Defense))
        {
            double _dmg = _placedCard.Damage;
            _botcard = user.UserDeck.Where(item => item.TypeOfCard.Equals(typeOfCard)).First();
            double _def = _botcard.Defense;
            double _koef = _dmg / _def;

            foreach (var item in user.UserDeck.Where(item => item.TypeOfCard.Equals(typeOfCard)))
            {
                Card _botcardTemp = item;
                double _defTemp = item.Defense;
                double _koefTemp = _dmg / _defTemp;

                double diff1 = Math.Abs(1.00 - _koef);
                double diff2 = Math.Abs(1.00 - _koefTemp);

                if (diff1 == diff2)
                {
                    continue;
                }
                if (diff1 < diff2)
                {
                    continue;
                }
                if (diff1 > diff2)
                {
                    _def = _botcardTemp.Defense;
                    _botcard = _botcardTemp;
                    _koef = _koefTemp;
                }
            }
        }

        int randomNumber = UnityEngine.Random.Range(1, 4);

        if (typeOfCard.Equals(TypeOfCard.Attack))
        {
            if (randomNumber <= 2)
            {
                if (user.UserDeck.Any(item => item.TypeOfCard.Equals(TypeOfCard.Attack)))
                {
                    _botcard = user.UserDeck.Where(item => item.TypeOfCard.Equals(TypeOfCard.Attack)).OrderByDescending(item => item.Damage).First();
                }
                else if (user.UserDeck.Any(item => item.TypeOfCard.Equals(TypeOfCard.Special)))
                {
                    _botcard = user.UserDeck.Where(item => item.TypeOfCard.Equals(TypeOfCard.Special)).First();
                }
            }
            else
            {
                if (user.UserDeck.Any(item => item.TypeOfCard.Equals(TypeOfCard.Special)))
                {
                    _botcard = user.UserDeck.Where(item => item.TypeOfCard.Equals(TypeOfCard.Special)).First();
                }
                else if (user.UserDeck.Any(item => item.TypeOfCard.Equals(TypeOfCard.Attack)))
                {
                    _botcard = user.UserDeck.Where(item => item.TypeOfCard.Equals(TypeOfCard.Attack)).OrderByDescending(item => item.Damage).First();
                }
            }
        }

        return _botcard;
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
        userStatsLogic(_placedCardUser);
        Console.WriteLine("---- End Of Current Turn Stack ----");
        Console.WriteLine();
    }

    public void userStatsLogic(User user)
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
        return _skipCard;
    }

    public Card SkipDefense(User user)
    {
        user.HealthPoints -= _placedCard.Damage;
       
        _placedCard = null;
        _placedCardUser = null;
        return _skipCard;
    }

    public void RandomWitchTurn(int turn)
    {
        int probability = UnityEngine.Random.Range(0, 101);

        if (25 < turn && turn <= 40)
        {
            if(probability <= 10)
            {
                WitchMP();
                _audio.Play();
                _uiUpdater.MoveWitch();
                _uiUpdater.UpdateUserStats();
            }
            return;
        }

        if (40 < turn && turn <= 60)
        {
            if (probability <= 20)
            {
                WitchMP();
                _audio.Play();
                _uiUpdater.MoveWitch();
                _uiUpdater.UpdateUserStats();
            }
            return;
        }

        if (turn > 60)
        {
            if (probability <= 30)
            {
                WitchMP();
                _audio.Play();
                _uiUpdater.MoveWitch();
                _uiUpdater.UpdateUserStats();
            }
            return;
        }
        
    }

    public void WitchMP()
    {
        int probability = UnityEngine.Random.Range(0, 101);

        if (probability <= 10) {

            _player.ManaPoints -= 20;
        }

        if (probability > 10 && probability <= 20)
        {
            _bot.ManaPoints -= 10;
        }

        if (probability > 20 && probability <= 30 )
        {
            _player.ManaPoints -= 10;
        }

        if (probability > 30 && probability <= 50)
        {
            _bot.ManaPoints -= 5;
        }

        if (probability > 50 && probability <= 90)
        {
            _player.ManaPoints += 5;
        }

        if (probability > 90 && probability <= 100)
        {
            _player.ManaPoints += 10;
        }
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


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
using TMPro;
using UnityEngine.Networking;
using System.IO;




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
    public Transform[] cardSlots;
    public Transform[] placedCardSlots;


    public Game(string gameName, Player player, Bot bot)
    {
        _gameName = gameName;
        _finished = false;
        _player = player;
        _bot = bot;
    }

    private void Start()
    {
        _gameNameText.SetText(_gameName);
        _deck.Shuffle();
        for (int i = 0; i < 5; i++)
        {
            Card drawnCard = _deck.DrawCard();
            _player.UserDeck.Add(drawnCard);
            UpdateImagesFromCardSlots(drawnCard.ImageFileName, _player.UserDeck.Count - 1);
            cardSlots[_player.UserDeck.Count - 1].gameObject.SetActive(true);
            _bot.UserDeck.Add(_deck.DrawCard());
        }
        //PlayGame();
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
            Console.WriteLine(user2 + " won the game! Congrats!");
            _finished = true;
            return true;
        }
        if (user2.HealthPoints <= 0)
        {
            Console.WriteLine(user1 + " won the game! Congrats!");
            _finished = true;
            return true;
        }
        return false;
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
        Debug.Log(_player.UserDeck[index]);
        _player.UserDeck.Remove(selectedCard);
        UpdateImagesFromCardSlots("backcard", index);
        //cardSlots[index].gameObject.SetActive(false);
        UpdateImagesFromPlacedCardSlots(selectedCard._imageFileName, 0);
        placedCardSlots[0].gameObject.SetActive(true);
        UpdateCardSlots();

        _placedCard = selectedCard;
        _placedCardUser = _player;
        
    }

    public void UpdateCardSlots()
    {
        cardSlots[_player.UserDeck.Count].gameObject.SetActive(false);
        for (int i = 0; i < _player.UserDeck.Count; i++)
        {
            UpdateImagesFromCardSlots(_player.UserDeck[i].ImageFileName, i);
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


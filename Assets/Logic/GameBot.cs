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
using static UnityEditor.Experimental.GraphView.GraphView;
using Random = System.Random;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using static Game;
using UnityEngine.Rendering;

public class GameBot : MonoBehaviour
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
    public Transform[] botCardSlots;
    public Transform[] placedCardSlots;

    public int last_bot_damage;
    public int last_player_damage;

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

    public int _depth = 4;

    public GameBot(string gameName, Player player, Bot bot)
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
        _player.Name = "Bot 2";
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
            _uiUpdater.UpdateImagesFromCardSlots(drawnCard.ImageFileName, _player.UserDeck.Count - 1, "player");
            cardSlots[_player.UserDeck.Count - 1].gameObject.SetActive(true);
            drawnCard = _deck.DrawCard();
            _bot.UserDeck.Add(drawnCard);
            _uiUpdater.UpdateImagesFromCardSlots(drawnCard.ImageFileName, _bot.UserDeck.Count - 1, "bot");
            botCardSlots[_bot.UserDeck.Count - 1].gameObject.SetActive(true);
        }

        while (!_finished)
        {
            printDef(_bot);
            //DisableCardSlot(0);
            //Bug: Wenn man 7 Karten hat crashed es :(
            //Bug: Manchmal sind die Karten die in den Slots angezeigt werden out of sync, also die Falsche Textur wird wahrscheinlich geladen
            _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Defense, "player"), false, "player");


            if (PlayerPrefs.GetInt("Deck", 1) == 1)
            {
                _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Attack, "bot"), false, "bot");
                _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Special, "bot"), false, "bot");
            }
            else if (PlayerPrefs.GetInt("Deck", 1) == 0)
            {
                _uiUpdater.ChangeAllCardSlotStatesToFullDark("bot");
            }


            //yield return StartCoroutine(CheckIfUserHasPlacedCard());
            //int index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
            //Debug.Log("User card: " + _player.UserDeck[index]);

            Card bot2Card = null;
            int Bot2algorithmus = PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1);
            if ((Bot2algorithmus == 1))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                bot2Card = BotDecideMove(gameState, "bot_attack", 4); // 4. player attack wichtig da
            }
            if ((Bot2algorithmus == 2))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                bot2Card = BotDecideMoveAlphaBeta(gameState, "bot_attack", 4); // 4. player attack wichtig da
            }
            if (Bot2algorithmus == 3)
            {
                bot2Card = new MCTS().MCTSFindBestMove(_player, _bot, placedCard: null, isBotCountering: false, botTurn: true);
            }

            if (bot2Card != null)
            {
                PlaceCard(_player, bot2Card);
                _uiUpdater.UpdateImagesFromPlacedCardSlots(bot2Card.ImageFileName, 0);
            }
            else if (bot2Card == null)
            {
                PlaceCard(_player, _skipCard);
                _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 0);
            }

            placedCardSlots[0].gameObject.SetActive(true);
            _uiUpdater._skipTurn = false;
            _uiUpdater.UpdateCardSlots("player");
            _uiUpdater.BotImage("AngryBot");

            yield return StartCoroutine(WaitSeconds(2f));
            StopCoroutine(WaitSeconds(2f));

            _uiUpdater.BotImage("ThinkingBot");

            yield return StartCoroutine(WaitSeconds(2f));
            StopCoroutine(WaitSeconds(2f));

            // Card botCard = getBotCard(_bot, TypeOfCard.Defense); // algo
            //Debug.Log("Bot Counter: " + botCard);

            Card botCard = null;

            int algorithmus = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);

            if ((algorithmus == 1))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, _placedCard.Damage, 0);
                botCard = BotDecideMove(gameState, "bot_counter", 3); // attack nicht wichtig da
            }
            if ((algorithmus == 2))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, _placedCard.Damage, 0);
                botCard = BotDecideMoveAlphaBeta(gameState, "bot_counter", 3); // attack nicht wichtig da
            }
            if (algorithmus == 3)
            {
                botCard = new MCTS().MCTSFindBestMove(_bot, _player, _placedCard, isBotCountering: true, botTurn: true);
            }

            if (botCard != null)
            {
                PlaceCounter(_bot, botCard);
                _uiUpdater.UpdateImagesFromPlacedCardSlots(botCard.ImageFileName, 1);
            }
            else if (botCard == null)
            {
                PlaceCounter(_bot, _skipCard);
                _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 1);
            }

            placedCardSlots[1].gameObject.SetActive(true);
            _uiUpdater.BotImage("DefaultBot");
            _uiUpdater.UpdateCardSlots("bot");

            yield return StartCoroutine(WaitSeconds(5f));
            StopCoroutine(WaitSeconds(5f));

            _uiUpdater.ChangeAllCardSlotStates(true, "player");
            _uiUpdater.ChangeAllCardSlotStates(true, "bot");
            userStatsLogic(_bot);
            _uiUpdater.UpdateUserStats();
            hasUserPickedCard = false;
            if (checkIfWon(_player, _bot))
                break;
            //StopCoroutine(CheckIfUserHasPlacedCard());

            _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 0);
            _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 1);
            placedCardSlots[0].gameObject.SetActive(false);
            placedCardSlots[1].gameObject.SetActive(false);

            if (_player.UserDeck.Count != 7)
                _player.UserDeck.Add(_deck.DrawCard());
            if (_bot.UserDeck.Count != 7)
                _bot.UserDeck.Add(_deck.DrawCard());

            turn++;
            _uiUpdater.SetTurnText(turn);
            RandomWitchTurn(turn);

            _uiUpdater.ChangeAllCardSlotStates(false, "player");
            //ChangeCardSlotStates(GetIndexesForCardType(TypeOfCard.Defense), true); //BEEP
            _uiUpdater.UpdateCardSlots("player");
            _uiUpdater.UpdateCardSlots("bot");
            _uiUpdater.BotImage("ThinkingBot");


            //New Turn (BOT FIRST)
            printDef(_bot);
            _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Defense, "bot"), false, "bot");
            
            if (PlayerPrefs.GetInt("Deck", 1) == 0)
            {
                _uiUpdater.ChangeAllCardSlotStatesToFullDark("bot");
            }
            
            yield return StartCoroutine(WaitSeconds(2f));
            StopCoroutine(WaitSeconds(2f));

            //botCard = getBotCard(_bot, TypeOfCard.Attack); // ALGO
            //Debug.Log("Bot Card: " + botCard);

            algorithmus = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);
            if ((algorithmus == 1))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, 0, 0);
                botCard = BotDecideMove(gameState, "bot_attack", 4); // 4. player attack wichtig da
            }
            if ((algorithmus == 2))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, 0, 0);
                botCard = BotDecideMoveAlphaBeta(gameState, "bot_attack", 4); // 4. player attack wichtig da
            }
            if (algorithmus == 3)
            {
                botCard = new MCTS().MCTSFindBestMove(_bot, _player, placedCard: null, isBotCountering: false, botTurn: true);
            }

            if (botCard != null)
            {
                PlaceCard(_bot, botCard);
                _uiUpdater.UpdateImagesFromPlacedCardSlots(botCard.ImageFileName, 1);
            }
            else if (botCard == null)
            {
                PlaceCard(_bot, _skipCard);
                _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 1);
            }

            _uiUpdater.UpdateCardSlots("bot");

            _uiUpdater.BotImage("DefaultBot");
            placedCardSlots[1].gameObject.SetActive(true);
            _uiUpdater.ChangeAllCardSlotStates(true, "player");
            _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Attack, "player"), false, "player");
            _uiUpdater.ChangeCardSlotStates(_uiUpdater.GetIndexesForCardType(TypeOfCard.Special, "player"), false, "player");
            //index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");

            
            //yield return StartCoroutine(CheckIfUserHasPlacedCard());
            //index = (int)Variables.Object(placedCardSlots[0]).Get("cardIndexInUserDeck");
            //Debug.Log("User Counter: " + _player.UserDeck[index]);

            if ((Bot2algorithmus == 1))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0 , 0);
                // damage
                bot2Card = BotDecideMove(gameState, "bot_counter", 3); // attack nicht wichtig da
            }
            if ((Bot2algorithmus == 2))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                // damage
                bot2Card = BotDecideMoveAlphaBeta(gameState, "bot_counter", 3); // attack nicht wichtig da
            }
            if (Bot2algorithmus == 3)
            {
                bot2Card = new MCTS().MCTSFindBestMove(_player, _bot, placedCard: null, isBotCountering: true, botTurn: true);
                // damage
            }

            if (bot2Card != null)
            {
                PlaceCounter(_player, bot2Card);
                _uiUpdater.UpdateImagesFromPlacedCardSlots(bot2Card.ImageFileName, 0);
            }
            else if (bot2Card == null)
            {
                PlaceCounter(_player, _skipCard);
                _uiUpdater.UpdateImagesFromPlacedCardSlots("backcard", 0);
            }

            placedCardSlots[0].gameObject.SetActive(true);
            _uiUpdater._skipTurn = false;
            _uiUpdater.UpdateCardSlots("player");

            yield return StartCoroutine(WaitSeconds(5f));
            StopCoroutine(WaitSeconds(5f));


            _uiUpdater.ChangeAllCardSlotStates(true, "player");
            userStatsLogic(_player);
            _uiUpdater.UpdateUserStats();

            checkIfWon(_player, _bot);
            //StopCoroutine(CheckIfUserHasPlacedCard());

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
            _uiUpdater.ChangeAllCardSlotStates(true, "player");
            _uiUpdater.ChangeAllCardSlotStates(true, "bot");
            _uiUpdater.UpdateCardSlots("player");
            _uiUpdater.UpdateCardSlots("bot");
            hasUserPickedCard = false;
        }
        wonOrLostText.gameObject.SetActive(true);
        backToStart.gameObject.SetActive(true);
        winLosePanel.gameObject.SetActive(true);
        backButton.gameObject.SetActive(false);
        //_skipButton.gameObject.SetActive(false);
        //_submitButton.gameObject.SetActive(false);
        //yield return StartCoroutine(CheckIfUserHasPlacedCard());
    }

    /*IEnumerator CheckIfUserHasPlacedCard()
    {
        yield return new WaitUntil(() => hasUserPickedCard == true);
        Debug.Log("Placed Card!");
    }*/

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
            if (probability <= 10)
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

        if (probability <= 10)
        {

            _player.ManaPoints -= 20;
        }

        if (probability > 10 && probability <= 20)
        {
            _bot.ManaPoints -= 10;
        }

        if (probability > 20 && probability <= 30)
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

    // für die Alg
    public class GameState
    {
        public int botHealth;
        public int playerHealth;
        public int botMana;
        public int playerMana;
        public int botMoney;
        public int playerMoney;
        public List<Card> botHand;
        public List<Card> playerHand;
        public int last_bot_damage;
        public int last_player_damage;

        public GameState(int botHealth, int playerHealth, int botMana, int playerMana, int botMoney, int playerMoney, List<Card> botHand, List<Card> playerHand, int last_bot_damage, int last_player_damage)
        {
            this.botHealth = botHealth;
            this.playerHealth = playerHealth;
            this.botMana = botMana;
            this.playerMana = playerMana;
            this.botMoney = botMoney;
            this.playerMoney = playerMoney;
            this.botHand = botHand;
            this.playerHand = playerHand;
            this.last_bot_damage = last_bot_damage;
            this.last_player_damage = last_player_damage;
        }

        public GameState Clone()
        {
            return new GameState(this.botHealth, this.playerHealth, this.botMana, this.playerMana, this.botMoney, this.playerMoney, new List<Card>(this.botHand), new List<Card>(this.playerHand), this.last_bot_damage, this.last_player_damage);
        }

    }

    // Minimax v1
    private Card BotDecideMove(GameState state, string phase, int depth)
    {
        int bestScore = int.MinValue;
        Card bestCard = null;

        List<Card> cards = new List<Card>(state.botHand);
        bool hasValidMove = false;

        foreach (var card in cards)
        {
            if (!IsValidMove(card, phase == "bot_counter" ? true : false)) continue;

            hasValidMove = true;

            GameState newGamestate = state.Clone();
            ApplyPlay(newGamestate, "bot", card);

            // STATE
            //int tempBotHP = bot.HealthPoints;
            //int tempPlayerHP = player.HealthPoints;
            //int tempBotMP = bot.ManaPoints;
            //int tempPlayerMP = player.ManaPoints;
            //int tempBotGP = bot.Money;
            //int tempPlayerGP = player.Money;
            //int copy_last_player_damage = last_player_damage;
            //int copy_last_bot_damage = last_bot_damage;
            //List<Card> tempBotList = bot.UserDeck;
            //List<Card> tempPlayerList = player.UserDeck;

            // Wenn "gesetzt" wird keine doppelten gesetzten Karten
            if (newGamestate.last_bot_damage > 0 && phase == "bot_counter")
            {
                newGamestate.botHand.Remove(card);
            }
            else if(phase == "bot_attack")
            {
                newGamestate.botHand.Remove(card);
            }

            if (phase == "bot_counter") {
                newGamestate = ResolveState(newGamestate, "bot", card);
            }

            int score = Minimaxv1(newGamestate, depth - 1, NextPhase(phase));

            if (score >= bestScore)
            {
                bestCard = card;
                bestScore = score;
            } 
        }

        if (!hasValidMove)
        {
            return null;
        }

        return bestCard;
    }

    private int Minimaxv1(GameState state, int depth, string phase)
    {
        if(depth == 0 || IsSimOver(state.botHealth, state.playerHealth)) {
            return EvalV1(state);
        }

        // Player attack (Minimizer)
        if (phase == "player_attack")
        {
            int minEval = int.MaxValue;
            List<Card> cards = new List<Card>(state.playerHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, false)) continue;

                GameState newPlayerAttackState = state.Clone();
                ApplyPlay(newPlayerAttackState, "player", card);
                newPlayerAttackState.playerHand.Remove(card);
                int eval = Minimaxv1(newPlayerAttackState, depth - 1, "bot_counter");
                minEval = Math.Min(minEval, eval);
            }
            return minEval;
        }
        // Bot Counter (Maximizer)
        else if (phase == "bot_counter")
        {
            int maxEval = int.MinValue;
            List<Card> cards = new List<Card>(state.botHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, true)) continue;
                GameState newBotCounterState = state.Clone(); 
                ApplyPlay(newBotCounterState, "bot",card);

                if (newBotCounterState.last_bot_damage > 0 && phase == "bot_counter")
                {
                    newBotCounterState.botHand.Remove(card);
                }

                if (phase == "bot_counter")
                {
                    newBotCounterState = ResolveState(newBotCounterState, "bot", card);
                }

                int eval = Minimaxv1(newBotCounterState, depth - 1, "bot_attack");
                maxEval = Math.Max(maxEval, eval);
            }
            return maxEval;
        }
        // Bots attack (Maximizer)
        if (phase == "bot_attack")
        {
            int maxEval = int.MinValue;
            List<Card> cards = new List<Card> (state.botHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, false)) continue;
                GameState newBotAttackState = state.Clone(); 
                ApplyPlay(newBotAttackState, "bot", card);
                newBotAttackState.botHand.Remove(card);
                int eval = Minimaxv1(newBotAttackState, depth - 1, "player_counter");
                maxEval = Math.Max(maxEval, eval);
            }
            return maxEval;
        }
        // Player Counter (Minimizer)
        else if (phase == "player_counter")
        {
            int minEval = int.MaxValue;

            List<Card> cards = new List<Card>(state.playerHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, true)) continue;
                GameState newPlayerCounterState = state.Clone(); 
                ApplyPlay(newPlayerCounterState, "player", card);

                if (newPlayerCounterState.last_player_damage > 0 && phase == "player_counter")
                {
                    newPlayerCounterState.playerHand.Remove(card);
                }

                if (phase == "player_counter")
                {
                    newPlayerCounterState = ResolveState(newPlayerCounterState, "player", card);
                }

                int eval = Minimaxv1(newPlayerCounterState, depth - 1, "player_attack");
                minEval = Math.Min(minEval, eval);
            }
            return minEval;
        }

        return 0;
    }

    private int EvalV1(GameState state)
    {
        return state.botHealth - state.playerHealth;
    }


    private GameState ResolveState(GameState state, string role, Card card)
    {
        if (role == "player")
        {
            state.playerHealth -= state.last_player_damage - card.Defense;
            state.last_player_damage = 0;
        }
        else if (role == "bot")
        {
            state.botHealth -= state.last_bot_damage - card.Defense;
            state.last_bot_damage = 0;
        }

        return state;
    }


    private GameState ApplyPlay(GameState state, string turn, Card card)
    {
        if (turn == "player")
        {
            if (card.TypeOfCard == TypeOfCard.Attack)
            {
                state.last_bot_damage = card.Damage;
            }
            else if (card.TypeOfCard == TypeOfCard.Special)
            {
                Match match = Regex.Match(card.Name, @"(\D+)(\d+)");

                string prefix = match.Groups[1].Value;
                int number = int.Parse(match.Groups[2].Value);

                if (prefix == "HP")
                {
                    state.playerHealth += number;
                }
                if (prefix == "MP")
                {
                    state.playerMana += number;
                }
                if (prefix == "GP")
                {
                    state.playerMoney += number;
                }
            }
            else if(card.TypeOfCard == TypeOfCard.Defense)
            {
                state.last_player_damage = Math.Max(0, state.last_player_damage);
            }
        }
        else if(turn == "bot")
        {
            if (card.TypeOfCard == TypeOfCard.Attack)
            {
                state.last_player_damage = card.Damage;
            }
            else if (card.TypeOfCard == TypeOfCard.Special)
            {
                Match match = Regex.Match(card.Name, @"(\D+)(\d+)");

                string prefix = match.Groups[1].Value;
                int number = int.Parse(match.Groups[2].Value);

                if (prefix == "HP")
                {
                    state.botHealth += number;
                }
                if (prefix == "MP")
                {
                    state.botMana += number;
                }
                if (prefix == "GP")
                {
                    state.botMoney += number;
                }
            }
            else if (card.TypeOfCard == TypeOfCard.Defense)
            {
                state.last_bot_damage = Math.Max(0, state.last_bot_damage);
            }
        }
        return state;
    }

    public string NextPhase(string phase)
    {
        if (phase == "player_attack") return "bot_counter";
        if (phase == "bot_counter") return "bot_attack";
        if (phase == "bot_attack") return "player_counter";
        if (phase == "player_counter") return "player_attack";
        return phase;
    }


    private bool IsSimOver(int botHealth, int playerHealth)
    {
        return botHealth <= 0 || playerHealth <= 0;
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

    private bool IsValidMove(Card card, bool isBotCountering)
    {
        if (isBotCountering)
            return card.TypeOfCard == TypeOfCard.Defense;
        else
            return card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special;
    }


    // Minimax Alpha-Beta-Pruning
    private Card BotDecideMoveAlphaBeta(GameState state, string phase, int depth)
    {
        int bestScore = int.MinValue;
        Card bestCard = null;

        List<Card> cards = new List<Card>(state.botHand);
        bool hasValidMove = false;

        foreach (var card in cards)
        {
            if (!IsValidMove(card, phase == "bot_counter")) continue;

            GameState newState = state.Clone();
            ApplyPlay(newState, "bot", card);

            hasValidMove = true;

            if (phase == "bot_counter" && newState.last_bot_damage > 0)
            {
                newState.botHand.Remove(card);
            }
            else if (phase == "bot_attack")
            {
                newState.botHand.Remove(card);
            }

            if (phase == "bot_counter")
            {
                newState = ResolveState(newState, "bot", card);
            }

            int score = MinimaxAB(newState, depth - 1, NextPhase(phase), int.MinValue, int.MaxValue);

            if (score >= bestScore)
            {
                bestScore = score;
                bestCard = card;
            }
        }

        if (!hasValidMove)
        {
            return null;
        }

        return bestCard;
    }


    private int MinimaxAB(GameState state, int depth, string phase, int alpha, int beta)
    {
        if (depth == 0 || IsSimOver(state.botHealth, state.playerHealth))
        {
            return EvalV1(state);
        }

        if (phase == "player_attack") // Minimizer
        {
            int minEval = int.MaxValue;
            List<Card> cards = new List<Card>(state.playerHand);

            foreach (var card in cards)
            {
                if (!IsValidMove(card, false)) continue;

                GameState newState = state.Clone();
                ApplyPlay(newState, "player", card);
                newState.playerHand.Remove(card);

                int eval = MinimaxAB(newState, depth - 1, "bot_counter", alpha, beta);

                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                if (beta <= alpha)
                {
                    Debug.Log($"Pruning at depth {depth}, phase {phase}: alpha = {alpha}, beta = {beta}");
                    break; // Pruning
                }
            }
            return minEval;
        }
        else if (phase == "bot_counter") // Maximizer
        {
            int maxEval = int.MinValue;
            List<Card> cards = new List<Card>(state.botHand);

            foreach (var card in cards)
            {
                if (!IsValidMove(card, true)) continue;

                GameState newState = state.Clone();
                ApplyPlay(newState, "bot", card);

                if (newState.last_bot_damage > 0 && phase == "bot_counter")
                {
                    newState.botHand.Remove(card);
                }
                newState = ResolveState(newState, "bot", card);

                int eval = MinimaxAB(newState, depth - 1, "bot_attack", alpha, beta);

                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                {
                    Debug.Log($"Pruning at depth {depth}, phase {phase}: alpha = {alpha}, beta = {beta}");
                    break;
                }
            }
            return maxEval;
        }
        else if (phase == "bot_attack") // Maximizer
        {
            int maxEval = int.MinValue;
            List<Card> cards = new List<Card>(state.botHand);

            foreach (var card in cards)
            {
                if (!IsValidMove(card, false)) continue;

                GameState newState = state.Clone();
                ApplyPlay(newState, "bot", card);
                newState.botHand.Remove(card);

                int eval = MinimaxAB(newState, depth - 1, "player_counter", alpha, beta);

                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                {
                    Debug.Log($"Pruning at depth {depth}, phase {phase}: alpha = {alpha}, beta = {beta}");
                    break; 
                }
            }
            return maxEval;
        }
        else if (phase == "player_counter") // Minimizer
        {
            int minEval = int.MaxValue;
            List<Card> cards = new List<Card>(state.playerHand);

            foreach (var card in cards)
            {
                if (!IsValidMove(card, true)) continue;

                GameState newState = state.Clone();
                ApplyPlay(newState, "player", card);

                if (newState.last_player_damage > 0 && phase == "player_counter")
                {
                    newState.playerHand.Remove(card);
                }
                newState = ResolveState(newState, "player", card);

                int eval = MinimaxAB(newState, depth - 1, "player_attack", alpha, beta);

                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                if (beta <= alpha)
                {
                    Debug.Log($"Pruning at depth {depth}, phase {phase}: alpha = {alpha}, beta = {beta}");
                    break;
                }
            }
            return minEval;
        }

        return 0;
    }


    // MCTS

    public class MCTSNode
    {
        public User Bot { get; set; }
        public User Player { get; set; }
        public Card Move { get; set; }
        public double Wins { get; set; }
        public int Visits { get; set; }
        public List<MCTSNode> Children { get; set; }
        public MCTSNode Parent { get; set; }

        public MCTSNode(User bot, User player, Card move = null, MCTSNode parent = null)
        {
            Bot = bot;
            Player = player;
            Move = move;
            Wins = 0;
            Visits = 0;
            Children = new List<MCTSNode>();
            Parent = parent;
        }

        // Get valid moves for a given user
        public List<Card> GetValidMoves(User user, bool isBotTurn, bool isBotCountering)
        {
            return user.UserDeck.Where(card => MCTS.IsValidMove(card, isBotTurn, isBotCountering)).ToList();
        }
    }

    //MCTS

    public class MCTS
    {
        private const int simulationCount = 1000; // statisch bearbeitet
        private Random _random = new Random();

        public Card MCTSFindBestMove(User bot, User player, Card placedCard, bool isBotCountering, bool botTurn)
        {
            var rootNode = new MCTSNode(bot, player, placedCard);

            for (int i = 0; i < simulationCount; i++)
            {
                Log($"iteration {i}");
                var selectedNode = MCTSSelectNode(rootNode);
                var expandedNode = MCTSExpandNode(selectedNode, isBotCountering, botTurn);
                var result = MCTSSimulate(expandedNode, isBotCountering, botTurn, placedCard);
                MCTSBackpropagate(expandedNode, result);
            }

            return rootNode.Children.OrderByDescending(child => child.Visits).FirstOrDefault()?.Move; // höhsten iterationen sind favorable
        }
        private void Log(string message)
        {
            Debug.Log($"[LOG] {DateTime.Now:HH:mm:ss} - {message}");
        }

        // Selection Phase - Blattknoten suchen/bewerten
        private MCTSNode MCTSSelectNode(MCTSNode node)
        {
            Log($"Selecting node with the {node.Children.Count} children...");
            while (node.Children.Any())
            {
                node = MCTSUCTSelect(node);
            }
            Log("Selection phase complete.");
            return node;
        }

        // Expansion Phase - Mögliche Moves und Child Knoten werden geadded
        private MCTSNode MCTSExpandNode(MCTSNode node, bool isCountering, bool botTurn)
        {
            var currentPlayer = botTurn ? node.Bot : node.Player;

            List<Card> validMoves;

            if (botTurn) {
                if (isCountering)
                {
                    validMoves = node.GetValidMoves(node.Bot, botTurn, isCountering);  
                }
                else
                {
                    validMoves = node.GetValidMoves(node.Bot, botTurn, isCountering); 
                }
            }
            else
            {
                if (isCountering)
                {
                    validMoves = node.GetValidMoves(node.Player, botTurn, isCountering);
                }
                else
                {
                    
                    validMoves = node.GetValidMoves(node.Player, botTurn, isCountering);
                }
            }

            // Child Knoten
            foreach (var move in validMoves)
            {
                var child = new MCTSNode(CopyUser(node.Bot), CopyUser(node.Player), move, node);
                node.Children.Add(child);
            }

            return node.Children.Any() ? node.Children[_random.Next(node.Children.Count)] : node;
        }

        private double MCTSSimulate(MCTSNode node, bool isBotCountering, bool botTurn, Card opponentCard)
        {
            var simBot = CopyUser(node.Bot);
            var simPlayer = CopyUser(node.Player);
            bool isBotTurn = botTurn;
            Card damageCard = opponentCard;

            Random rng = new Random();

            while (!IsGameOver(simBot.HealthPoints, simPlayer.HealthPoints))
            {
                var currentUser = isBotTurn ? simBot : simPlayer;
                var validMoves = node.GetValidMoves(currentUser, isBotTurn, isBotCountering);

                if (!validMoves.Any()) break;

                // höhste UCT oder random
                var move = rng.NextDouble() < 0.7
                            ? validMoves.OrderByDescending(mv =>
                            {
                                var childNode = node.Children.FirstOrDefault(n => n.Move == mv);
                                return childNode != null ? MCTSUCTValue(childNode, node.Visits) : double.MinValue;
                            }).First()
                            : validMoves[rng.Next(validMoves.Count)];

                if (isBotTurn)
                {
                    if (isBotCountering)
                    {
                        SimulateMove(simBot, simPlayer, move, isBotTurn, damageCard); // Bot counters player's attack
                        isBotCountering = false;
                        isBotTurn = true;
                    }
                    else
                    {
                        SimulateMove(simBot, simPlayer, move, isBotTurn, null); // Bot attacks
                        isBotCountering = true;
                        isBotTurn = false;
                    }
                }
                else
                {
                    if (isBotCountering)
                    {
                        SimulateMove(simPlayer, simBot, move, isBotTurn, damageCard); // Player counters bot's attack
                        isBotCountering = false;
                        isBotTurn = false;
                    }
                    else
                    {
                        SimulateMove(simPlayer, simBot, move, isBotTurn, null); // Player attacks
                        isBotCountering = true;
                        isBotTurn = true;
                    }
                }

                damageCard = move;
            }

            return EvaluateGameStateMCTS(simBot, simPlayer);
        }



        // Backpropagation Phase - Update die Knoten
        private void MCTSBackpropagate(MCTSNode node, double result)
        {
            while (node != null)
            {
                node.Visits++;
                node.Wins += result;
                node = node.Parent;
            }
        }

        // UCT
        private MCTSNode MCTSUCTSelect(MCTSNode node)
        {
            return node.Children.OrderByDescending(child => MCTSUCTValue(child, node.Visits)).First();
        }

        // UCT value für Knoten
        private double MCTSUCTValue(MCTSNode node, int totalVisits)
        {
            if (node.Visits == 0)
            {
                return double.MaxValue;
            }
            return (node.Wins / node.Visits) + Math.Sqrt(2 * Math.Log(totalVisits) / node.Visits);
        }

        private User CopyUser(User user)
        {
            if (user is Bot bot)
            {
                return CopyBot(bot);
            }
            else if (user is Player player)
            {
                return CopyPlayer(player);
            }
            else
            {
                var gameObject = new GameObject("UserCopy");
                var newUser = gameObject.AddComponent<User>();
                newUser.Name = user.Name;
                newUser.Rating = user.Rating;
                newUser.HealthPoints = user.HealthPoints;
                newUser.ManaPoints = user.ManaPoints;
                newUser.Money = user.Money;
                newUser.UserDeck = user.UserDeck.ToList();
                return newUser;
            }
        }

        private Bot CopyBot(Bot bot)
        {
            var gameObject = new GameObject("BotCopy");
            var newBot = gameObject.AddComponent<Bot>();
            newBot.Name = bot.Name;
            newBot.Rating = bot.Rating;
            newBot.HealthPoints = bot.HealthPoints;
            newBot.ManaPoints = bot.ManaPoints;
            newBot.Money = bot.Money;
            newBot.UserDeck = bot.UserDeck.ToList();
            return newBot;
        }

        private Player CopyPlayer(Player player)
        {
            var gameObject = new GameObject("PlayerCopy");
            var newPlayer = gameObject.AddComponent<Player>();
            newPlayer.Name = player.Name;
            newPlayer.Rating = player.Rating;
            newPlayer.HealthPoints = player.HealthPoints;
            newPlayer.ManaPoints = player.ManaPoints;
            newPlayer.Money = player.Money;
            newPlayer.UserDeck = player.UserDeck.ToList();
            return newPlayer;
        }


        private void SimulateMove(User bot, User player, Card card, bool isBotTurn, Card opponentCard)
        {
            User actingUser = isBotTurn ? bot : player;
            User targetUser = isBotTurn ? player : bot;

            switch (card.TypeOfCard)
            {
                case TypeOfCard.Attack:
                    targetUser.HealthPoints -= card.Damage;
                    break;

                case TypeOfCard.Defense:
                    if (opponentCard != null && opponentCard.TypeOfCard == TypeOfCard.Attack)
                    {
                        actingUser.HealthPoints -= Math.Max(0, opponentCard.Damage - card.Defense);
                    }
                    break;

                case TypeOfCard.Special:
                    if (card.Name.StartsWith("HP"))
                    {
                        var healed = int.Parse(card.Name.Substring(2));
                        actingUser.HealthPoints += healed;
                    }
                    break;
            }

            actingUser.UserDeck.Remove(card);

            Debug.Log($"{(isBotTurn ? "Bot" : "Player")} used {card.TypeOfCard} card: {card.Name}");
        }



        private bool IsGameOver(int botHP, int playerHP)
        {
            return botHP <= 0 || playerHP <= 0;
        }

        private double EvaluateGameStateMCTS(User bot, User player)
        {
            double score = 0.0;
            score += (bot.HealthPoints - player.HealthPoints) * 1.0;
            score += (bot.UserDeck.Count - player.UserDeck.Count) * 0.5;
            score += (CountCardsOfTypeMCTS(bot, TypeOfCard.Special) - CountCardsOfTypeMCTS(player, TypeOfCard.Special)) * 0.75;
            score += (CountCardsOfTypeMCTS(bot, TypeOfCard.Defense) - CountCardsOfTypeMCTS(player, TypeOfCard.Defense)) * 0.3;
            return score;
        }

        private int CountCardsOfTypeMCTS(User user, TypeOfCard type)
        {
            return user.UserDeck.Count(card => card.TypeOfCard == type);
        }

        public static bool IsValidMove(Card card, bool isBotTurn, bool isBotCountering)
        {
            if (isBotCountering)
            {
                return card.TypeOfCard == TypeOfCard.Defense;
            }
            else if (isBotTurn && isBotCountering == false)
            {
                return card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special;
            }
            else
            {
                return card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special;
            }
        }
    }
}
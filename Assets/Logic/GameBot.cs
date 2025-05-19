using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameBot : MonoBehaviour
{
    public Player playerTemplate;
    public Bot botTemplate;
    public GameDeck deckTemplate;

    public string _gameName;
    public GameDeck _deck;
    public Boolean _finished;

    public Player _player;
    public Bot _bot;

    public static Card _placedCard;
    public static User _placedCardUser;
    
    public Card _skipCard;
    public TMP_Text _gameNameText;

    public int last_bot_damage;
    public int last_player_damage;

    public static bool hasUserPickedCard = false;
    public Button userStats;
    public Button botStats;
    public Button backToStart;

    public int _depth = 4;

    private List<MetricsState> allGameResults;

    private void Start()
    {
        allGameResults = new List<MetricsState>();
        float numSimulations = PlayerPrefs.GetFloat("SimulationsAmount", 1f);
        StartCoroutine(SimulateMultipleGames(numSimulations));
    }

    IEnumerator SimulateMultipleGames(float count)
    {
        for (int i = 1; i <= count; i++)
        {
            _gameName = $"Game #{i}";
            _gameNameText.SetText(_gameName);

            // Clone player, bot, and deck
            _player = Instantiate(playerTemplate);
            _bot = Instantiate(botTemplate);
            _deck = Instantiate(deckTemplate);
            _finished = false;

            yield return StartCoroutine(StartGame());

            // Cleanup
            Destroy(_player.gameObject);
            Destroy(_bot.gameObject);
            Destroy(_deck.gameObject);

            yield return new WaitForSeconds(1f);
        }

        Debug.Log("All simulations completed.");
        SaveAllResultsToFile();
    }

    IEnumerator StartGame()
    {
        int turn = 1;
        _player.Name = "alg1";
        _bot.Name = "alg2";

        TextMeshProUGUI userText = userStats.GetComponentInChildren<TextMeshProUGUI>();
        userText.text = _player.Name;

        TextMeshProUGUI botText = botStats.GetComponentInChildren<TextMeshProUGUI>();
        botText.text = _bot.Name;

        userStatsLogic(_player);
        _gameNameText.SetText(_gameName);
        
        _deck.Shuffle();
        
        for (int i = 0; i < 5; i++)
        {
            Card drawnCard = _deck.DrawCard();
            _player.UserDeck.Add(drawnCard);
            drawnCard = _deck.DrawCard();
            _bot.UserDeck.Add(drawnCard);
        }

        printUserCards(_player, turn);
        printUserCards(_bot, turn);

        while (!_finished)
        {
            if (PlayerPrefs.GetInt("Deck", 1) == 1)
            {
                // write hidden
            }
            else
            {
                // write not hidden
            }

            Card bot2Card = null;
            int Bot2algorithmus = PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1);
            if ((Bot2algorithmus == 1))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                bot2Card = new Minimax().BotDecideMove(gameState, "bot_attack", 4); // 4. player attack wichtig da
            }
            if ((Bot2algorithmus == 2))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                bot2Card = new Minimax().BotDecideMoveAlphaBeta(gameState, "bot_attack", 4); // 4. player attack wichtig da
            }
            if (Bot2algorithmus == 3)
            {
                bot2Card = new MCTS().MCTSFindBestMove(_player, _bot, placedCard: null, isBotCountering: false, botTurn: true);
            }

            if (bot2Card != null)
            {
                PlaceCard(_player, bot2Card);
            }
            else if (bot2Card == null)
            {
                PlaceCard(_player, _skipCard);
            }
            printPlacedView();

            Card botCard = null;

            int algorithmus = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);

            if ((algorithmus == 1))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, _placedCard.Damage, 0);
                botCard = new Minimax().BotDecideMove(gameState, "bot_counter", 3); // attack nicht wichtig da
            }
            if ((algorithmus == 2))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, _placedCard.Damage, 0);
                botCard = new Minimax().BotDecideMoveAlphaBeta(gameState, "bot_counter", 3); // attack nicht wichtig da
            }
            if (algorithmus == 3)
            {
                botCard = new MCTS().MCTSFindBestMove(_bot, _player, _placedCard, isBotCountering: true, botTurn: true);
            }

            if (botCard != null)
            {
                PlaceCounter(_bot, botCard);
                printPlacedViewCounter(botCard);
            }
            else if (botCard == null)
            {
                PlaceCounter(_bot, _skipCard);
                printPlacedViewCounter(_skipCard);
            }

            userStatsLogic(_bot);
            hasUserPickedCard = false;
            if (checkIfWon(_player, _bot, turn))
                yield break;

            if (_player.UserDeck.Count != 7)
                _player.UserDeck.Add(_deck.DrawCard());
            if (_bot.UserDeck.Count != 7)
                _bot.UserDeck.Add(_deck.DrawCard());

            turn++;

            printUserCards(_player, turn);
            printUserCards(_bot, turn);

            //New Turn (BOT FIRST)

            algorithmus = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);
            if ((algorithmus == 1))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, 0, 0);
                botCard = new Minimax().BotDecideMove(gameState, "bot_attack", 4); // 4. player attack wichtig da
            }
            if ((algorithmus == 2))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, 0, 0);
                botCard = new Minimax().BotDecideMoveAlphaBeta(gameState, "bot_attack", 4); // 4. player attack wichtig da
            }
            if (algorithmus == 3)
            {
                botCard = new MCTS().MCTSFindBestMove(_bot, _player, placedCard: null, isBotCountering: false, botTurn: true);
            }

            if (botCard != null)
            {
                PlaceCard(_bot, botCard);
            }
            else if (botCard == null)
            {
                PlaceCard(_bot, _skipCard);
            }

            printPlacedView();

            if ((Bot2algorithmus == 1))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0 , 0);
                // damage
                bot2Card = new Minimax().BotDecideMove(gameState, "bot_counter", 3); // attack nicht wichtig da
            }
            if ((Bot2algorithmus == 2))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                // damage
                bot2Card = new Minimax().BotDecideMoveAlphaBeta(gameState, "bot_counter", 3); // attack nicht wichtig da
            }
            if (Bot2algorithmus == 3)
            {
                bot2Card = new MCTS().MCTSFindBestMove(_player, _bot, placedCard: null, isBotCountering: true, botTurn: true);
                // damage
            }

            if (bot2Card != null)
            {
                PlaceCounter(_player, bot2Card);
                printPlacedViewCounter(bot2Card);
            }
            else if (bot2Card == null)
            {
                PlaceCounter(_player, _skipCard);
                printPlacedViewCounter(_skipCard);
            }

            userStatsLogic(_player);

            if (checkIfWon(_player, _bot, turn))
                yield break;


            if (_player.UserDeck.Count != 7)
                _player.UserDeck.Add(_deck.DrawCard());
            if (_bot.UserDeck.Count != 7)
                _bot.UserDeck.Add(_deck.DrawCard());

            turn++;
            hasUserPickedCard = false;

            printUserCards(_player, turn);
            printUserCards(_bot, turn);
        }
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



    IEnumerator WaitSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }

    public bool checkIfWon(User user1, User user2, int turn)
    {

        if (user1.HealthPoints <= 0)
        {
            _finished = true;
            MetricsState metricsState = new MetricsState();
            metricsState.turns = turn;
            metricsState.winner = user2.Name;
            metricsState.loser = user1.Name;
            allGameResults.Add(metricsState);
            return true;
        }
        if (user2.HealthPoints <= 0)
        {
            _finished = true;
            MetricsState metricsState = new MetricsState();
            metricsState.turns = turn;
            metricsState.winner = user1.Name;
            metricsState.loser = user2.Name;
            allGameResults.Add(metricsState);
            return true;
        }
        return false;
    }

    [System.Serializable]
    public class MetricsState
    {
        public int turns;
        public string winner;
        public string loser;
    }

    public void SaveAllResultsToFile()
    {
        string json = JsonUtility.ToJson(new MetricsWrapper { results = allGameResults }, true);

        string folderPath = @"D:\simresults";
        string filePath = Path.Combine(folderPath, "gamestate.json");

        if (!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);

        System.IO.File.WriteAllText(filePath, json);
        Debug.Log("All simulation results saved to: " + filePath);
    }

    [System.Serializable]
    public class MetricsWrapper
    {
        public List<MetricsState> results;
    }

    public void BackToStart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }

    public void printUserCards(User user, int turn)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"[Turn {turn}] --- {user.Name}'s Deck ---");

        int inc = 0;
        foreach (var card in user.UserDeck)
        {
            sb.AppendLine($"{inc++}. {card}");
        }

        sb.AppendLine("---------------------------");
        Debug.Log(sb.ToString());
    }

    public void printPlacedView()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("----- The Current Attack ----");
        sb.AppendLine(_placedCard.ToString());
        sb.AppendLine("---- End Of Current Attack ----");
        Debug.Log(sb.ToString());
    }

    public void printPlacedViewCounter(Card card)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("----- The Current Counter ----");
        sb.AppendLine(card.ToString());
        sb.AppendLine("---- End Of Current Counter ----");
        Debug.Log(sb.ToString());
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

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(user.Name + " current stats are:");
        sb.AppendLine("HP: " + user.HealthPoints);
        sb.AppendLine("MP: " + user.ManaPoints);
        sb.AppendLine("Money: " + user.Money);
        Debug.Log(sb.ToString());
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

}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public TMP_Text _hiddenField;
    public TMP_Text _winsOne;
    public TMP_Text _winsTwo;

    private int p1_win = 0;
    private int p2_win = 0;

    private List<MetricsState> allGameResults;

    private int staleTurnCount = 0;
    private int lastPlayerHP;
    private int lastBotHP;
    private int lastPlayerCards;
    private int lastBotCards;
    private int lastPlayerMana;
    private int lastBotMana;

    private void Start()
    {
        allGameResults = new List<MetricsState>();
        float numSimulations = PlayerPrefs.GetFloat("SimulationsAmount", 1f);
        StartCoroutine(SimulateMultipleGames(numSimulations));
    }

    private SimUser ConvertToSimUser(User user)
    {
        return new SimUser
        {
            Name = user.Name,
            Rating = user.Rating,
            HealthPoints = user.HealthPoints,
            ManaPoints = user.ManaPoints,
            Money = user.Money,
            UserDeck = user.UserDeck.Select(c => ConvertToSimCard(c)).ToList(),
            ActiveBuffs = user.ActiveBuffs.Select(b => new Buff(b.Type, b.Value, b.Duration, b.IsDebuff)).ToList()
        };
    }

    private SimCard ConvertToSimCard(Card card)
    {
        return new SimCard
        {
            Name = card.Name,
            TypeOfCard = card.TypeOfCard,
            Damage = card.Damage,
            Defense = card.Defense,
            ManaCost = card.ManaCost,
            Duration = card.Duration,
            EffectValue = card.EffectValue
        };
    }

    IEnumerator SimulateMultipleGames(float count)
    {
        p1_win = 0;
        p2_win = 0;
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

    private Card MatchSimCardToRealCard(SimCard simCard, User user)
    {
        return user.UserDeck.FirstOrDefault(c => c.Name == simCard.Name);
    }

    IEnumerator StartGame()
    {
        int turn = 1;

        int PlayerName = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);
        string gamename = _gameNameText.text;

        switch (PlayerName)
        {
            case 1: _player.Name = "Minimax"; break;
            case 2: _player.Name = "Minimax Alpha-Beta"; break;
            case 3: _player.Name = "MCTS"; break;
            default: _player.Name = "Unknown"; break;
        }

        int BotName = PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1);

        switch (BotName)
        {
            case 1: _bot.Name = "Minimax"; break;
            case 2: _bot.Name = "Minimax Alpha-Beta"; break;
            case 3: _bot.Name = "MCTS"; break;
            default: _bot.Name = "Unknown"; break;
        }

        TextMeshProUGUI userText = userStats.GetComponentInChildren<TextMeshProUGUI>();
        userText.text = _player.Name;

        TextMeshProUGUI botText = botStats.GetComponentInChildren<TextMeshProUGUI>();
        botText.text = _bot.Name;

        userStatsLogic(_player);
        _gameNameText.SetText(_gameName);
        
        _deck.Shuffle();

        for (int i = 0; i < 5; i++)
        {
            try
            {
                _player.UserDeck.Add(_deck.DrawCard());
                _bot.UserDeck.Add(_deck.DrawCard());
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError("Failed to draw card: " + ex.Message);
            }
        }

        printUserCards(_player, turn);
        printUserCards(_bot, turn);

        _player.MaxMana = 20;
        _bot.MaxMana = 20;
        _player.ManaPoints = 10;
        _bot.ManaPoints = 10;

        while (!_finished)
        {
            CombatUtils.ApplyStartOfTurnBuffs(_player, _bot);
            CombatUtils.ApplyStartOfTurnBuffs(_bot, _player);
            _player.ManaPoints = Math.Min(_player.ManaPoints + 1, _player.MaxMana);
            _bot.ManaPoints = Math.Min(_bot.ManaPoints + 1, _bot.MaxMana);
            TextMeshProUGUI hiddenText = _hiddenField.GetComponentInChildren<TextMeshProUGUI>();
            if (PlayerPrefs.GetInt("Deck", 1) == 1)
            {
                hiddenText.text = "Hidden: yes";
            }
            else
            {
                hiddenText.text = "Hidden: no";
            }

            Card bot1Card = null;
            SimCard bot1SimCard = null;
            int Bot1algorithmus = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);
            if ((Bot1algorithmus == 1))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                bot1Card = new Minimax().BotDecideMove(gameState, Phase.BotAttack, 4); // 4. player attack wichtig da
            }
            if ((Bot1algorithmus == 2))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                bot1Card = new Minimax().BotDecideMoveAlphaBeta(gameState, Phase.BotAttack, 4); // 4. player attack wichtig da
            }
            if (Bot1algorithmus == 3)
            {
                var simPlayer = ConvertToSimUser(_player);
                var simBot = ConvertToSimUser(_bot);
                bot1SimCard = new MCTS().MCTSFindBestMove(simPlayer, simBot, null, false, true);
                bot1Card = bot1SimCard != null ? MatchSimCardToRealCard(bot1SimCard, _player) : _skipCard;
            }
            if (bot1Card != null)
            {
                PlaceCard(_player, bot1Card);
            }
            else if (bot1Card == null)
            {
                PlaceCard(_player, _skipCard);
            }
            printPlacedView();

            Card bot2Card = null;
            SimCard bot2SimCard = null;
            int Bot2algorithmus = PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1);

            if ((Bot2algorithmus == 1))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, _placedCard.Damage, 0);
                bot2Card = new Minimax().BotDecideMove(gameState, Phase.BotCounter, 3); // attack nicht wichtig da
            }
            if ((Bot2algorithmus == 2))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, _placedCard.Damage, 0);
                bot2Card = new Minimax().BotDecideMoveAlphaBeta(gameState, Phase.BotCounter, 3); // attack nicht wichtig da
            }
            if (Bot2algorithmus == 3)
            {
                var simPlayer = ConvertToSimUser(_player);
                var simBot = ConvertToSimUser(_bot);
                var placedSimCard = ConvertToSimCard(_placedCard);
                bot2SimCard = new MCTS().MCTSFindBestMove(simBot, simPlayer, placedSimCard, true, true);
                bot2Card = bot2SimCard != null ? MatchSimCardToRealCard(bot2SimCard, _bot) : _skipCard;
            }
            if (bot2Card != null)
            {
                PlaceCounter(_bot, bot2Card);
                printPlacedViewCounter(bot2Card);
            }
            else if (bot2Card == null)
            {
                PlaceCounter(_bot, _skipCard);
                printPlacedViewCounter(_skipCard);
            }

            userStatsLogic(_bot);
            hasUserPickedCard = false;
            if (checkIfWon(_player, _bot, turn, gamename))
                yield break;

            try
            {
                if (_player.UserDeck.Count != 7)
                    _player.UserDeck.Add(_deck.DrawCard());
                if (_bot.UserDeck.Count != 7)
                    _bot.UserDeck.Add(_deck.DrawCard());
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError("Failed to draw card: " + ex.Message);
            }

            turn++;

            printUserCards(_player, turn);
            printUserCards(_bot, turn);

            //New Turn (BOT FIRST)
            CombatUtils.ApplyStartOfTurnBuffs(_player, _bot);
            CombatUtils.ApplyStartOfTurnBuffs(_bot, _player);

            SimCard bot3SimCard = null;
            Bot2algorithmus = PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1);
            if ((Bot2algorithmus == 1))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, 0, 0);
                bot2Card = new Minimax().BotDecideMove(gameState, Phase.BotAttack, 4); // 4. player attack wichtig da
            }
            if ((Bot2algorithmus == 2))
            {
                GameState gameState = new GameState(_bot._healthPoints, _player.HealthPoints, _bot.ManaPoints, _player.ManaPoints, _bot.Money, _player.Money, _bot.UserDeck, _player.UserDeck, 0, 0);
                bot2Card = new Minimax().BotDecideMoveAlphaBeta(gameState, Phase.BotAttack, 4); // 4. player attack wichtig da
            }
            if (Bot2algorithmus == 3)
            {
                var simBot = ConvertToSimUser(_bot);
                var simPlayer = ConvertToSimUser(_player);
                bot3SimCard = new MCTS().MCTSFindBestMove(simBot, simPlayer, null, false, true);
                bot2Card = bot3SimCard != null ? MatchSimCardToRealCard(bot3SimCard, _bot) : _skipCard;
            }
            if (bot2Card != null)
            {
                PlaceCard(_bot, bot2Card);
            }
            else if (bot2Card == null)
            {
                PlaceCard(_bot, _skipCard);
            }

            printPlacedView();

            Bot1algorithmus = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);
            SimCard bot4SimCard = null;
            if ((Bot1algorithmus == 1))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0 , 0);
                bot1Card = new Minimax().BotDecideMove(gameState, Phase.BotCounter, 3); // attack nicht wichtig da
            }
            if ((Bot1algorithmus == 2))
            {
                GameState gameState = new GameState(_player.HealthPoints, _bot._healthPoints, _player.ManaPoints, _bot.ManaPoints, _player.Money, _bot.Money, _player.UserDeck, _bot.UserDeck, 0, 0);
                bot1Card = new Minimax().BotDecideMoveAlphaBeta(gameState, Phase.BotCounter, 3); // attack nicht wichtig da
            }
            if (Bot1algorithmus == 3)
            {
                var simPlayer = ConvertToSimUser(_player);
                var simBot = ConvertToSimUser(_bot);
                var placedSimCard = ConvertToSimCard(_placedCard);
                bot4SimCard = new MCTS().MCTSFindBestMove(simPlayer, simBot, placedSimCard, true, true);
                bot1Card = bot4SimCard != null ? MatchSimCardToRealCard(bot4SimCard, _player) : _skipCard;
            }

            if (bot1Card != null)
            {
                PlaceCounter(_player, bot1Card);
                printPlacedViewCounter(bot1Card);
            }
            else if (bot1Card == null)
            {
                PlaceCounter(_player, _skipCard);
                printPlacedViewCounter(_skipCard);
            }

            userStatsLogic(_player);

            if (checkIfWon(_player, _bot, turn, gamename))
                yield break;


            try
            {
                if (_player.UserDeck.Count != 7)
                    _player.UserDeck.Add(_deck.DrawCard());
                if (_bot.UserDeck.Count != 7)
                    _bot.UserDeck.Add(_deck.DrawCard());
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError("Failed to draw card: " + ex.Message);
            }

            turn++;
            hasUserPickedCard = false;

            bool progressHappened =
                _player.HealthPoints < lastPlayerHP ||
                _bot.HealthPoints < lastBotHP ||
                _player.UserDeck.Count != lastPlayerCards ||
                _bot.UserDeck.Count != lastBotCards ||
                _player.ManaPoints != lastPlayerMana ||
                _bot.ManaPoints != lastBotMana;

            if (progressHappened)
            {
                staleTurnCount = 0;
            }
            else
            {
                staleTurnCount++;
            }

            // check for stale
            lastPlayerHP = _player.HealthPoints;
            lastBotHP = _bot.HealthPoints;
            lastPlayerCards = _player.UserDeck.Count;
            lastBotCards = _bot.UserDeck.Count;
            lastPlayerMana = _player.ManaPoints;
            lastBotMana = _bot.ManaPoints;

            if (staleTurnCount >= 10)
            {
                Debug.Log("Deadlock erkannt – Spiel wird als Unentschieden gewertet.");
                MetricsState metricsState = new MetricsState
                {
                    gamename = _gameName,
                    turns = turn,
                    winner = "Draw",
                    loser = "Draw"
                };
                allGameResults.Add(metricsState);
                _finished = true;
                yield break;
            }

            printUserCards(_player, turn);
            printUserCards(_bot, turn);
        }
    }

    public void PlaceCard(User user, Card card)
    {
        if (user.ManaPoints < card.ManaCost)
        {
            Debug.Log("Not enough mana for this card.");
            return;
        }

        user.ManaPoints -= card.ManaCost;
        user.UserDeck.Remove(card);

        if (_placedCard == null && _placedCardUser == null)
        {
            _placedCard = card;
            _placedCardUser = user;
        }

        if (card.TypeOfCard == TypeOfCard.Special)
        {
            ApplySpecialCard(user, card);
        }

        if (card.TypeOfCard == TypeOfCard.Buff || card.TypeOfCard == TypeOfCard.Debuff)
        {
            bool isDebuff = card.TypeOfCard == TypeOfCard.Debuff;
            User target = isDebuff ? (user == _player ? _bot : _player) : user;
            Buff parsedBuff = ParseBuffFromCardName(card.Name);
            if (parsedBuff != null)
            {
                parsedBuff.IsDebuff = isDebuff;
                target.ActiveBuffs.Add(parsedBuff);
                Debug.Log($"{card.Name} applied to {target.Name}: {parsedBuff.Type}, {parsedBuff.Value}, {parsedBuff.Duration} turns");
            }
        }
    }

    private void ApplySpecialCard(User user, Card card)
    {
        Match match = Regex.Match(card.Name, @"(HP|MP|GP)(\\d+)");
        if (!match.Success) return;

        string prefix = match.Groups[1].Value;
        int number = int.Parse(match.Groups[2].Value);

        switch (prefix)
        {
            case "HP": user.HealthPoints += number; break;
            case "MP": user.ManaPoints += number; break;
            case "GP": user.Money += number; break;
        }
    }

    private Buff ParseBuffFromCardName(string name)
    {
        Match match = Regex.Match(name, @"(DOT|HOT|DEF|ATK)[+-](\\d+)D(\\d+)");
        if (!match.Success) return null;

        BuffType type = match.Groups[1].Value switch
        {
            "DOT" => BuffType.DamageOverTime,
            "HOT" => BuffType.HealOverTime,
            "DEF" => BuffType.DefenseBoost,
            "ATK" => BuffType.AttackBoost,
            _ => throw new Exception("Unknown Buff")
        };

        int value = int.Parse(match.Groups[2].Value);
        int duration = int.Parse(match.Groups[3].Value);

        bool isDebuff = name.StartsWith("DOT") || name.StartsWith("ATK-");
        return new Buff(type, value, duration, isDebuff);
    }

    public bool checkIfWon(User user1, User user2, int turn, string gamename)
    {
        TextMeshProUGUI onewin = _winsOne.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI twowin = _winsTwo.GetComponentInChildren<TextMeshProUGUI>();
        
        if (user1.HealthPoints <= 0)
        {
            _finished = true;
            MetricsState metricsState = new MetricsState();
            metricsState.gamename = gamename;
            metricsState.turns = turn;
            metricsState.winner = user2.Name;
            metricsState.loser = user1.Name;
            allGameResults.Add(metricsState);
            p2_win++;
            twowin.text = $"Wins: {p2_win}";
            return true;
        }
        if (user2.HealthPoints <= 0)
        {
            _finished = true;
            MetricsState metricsState = new MetricsState();
            metricsState.gamename = gamename;
            metricsState.turns = turn;
            metricsState.winner = user1.Name;
            metricsState.loser = user2.Name;
            allGameResults.Add(metricsState);
            p1_win++;
            onewin.text = $"Wins: {p1_win}";
            return true;
        }
        return false;
    }

    [System.Serializable]
    public class MetricsState
    {
        public string gamename;
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
        sb.AppendLine("Active Buffs:");
        foreach (var buff in user.ActiveBuffs)
            sb.AppendLine($"{buff.Type} ({(buff.IsDebuff ? "Debuff" : "Buff")}): {buff.Value} for {buff.Duration} turns");
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
        if (_placedCard != null && _placedCardUser != null && _placedCard.TypeOfCard == TypeOfCard.Attack)
        {
            if (user.ManaPoints < card.ManaCost)
            {
                Debug.Log("Not enough mana for this card.");
                return;
            }

            user.ManaPoints -= card.ManaCost;
            user.UserDeck.Remove(card);

            int attack = CombatUtils.GetEffectiveAttack(_placedCard.Damage, _placedCardUser.ActiveBuffs);
            int defense = CombatUtils.GetEffectiveDefense(card.Defense, user.ActiveBuffs);
            int netDamage = CombatUtils.CalculateNetDamage(attack, defense);
            user.HealthPoints -= netDamage;
        }

        _placedCard = null;
        _placedCardUser = null;
    }
}
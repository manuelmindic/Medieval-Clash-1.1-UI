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

    private List<Card> _remainingDeckForBot;
    private List<Card> _remainingDeckForPlayer;
    private List<Card> _playedCards = new List<Card>();

    private double playerTotalTime = 0;
    private int playerMoveCount = 0;

    private double botTotalTime = 0;
    private int botMoveCount = 0;

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
        return new SimCard(card);
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
            _deck.FilterBuffDebuffCards();

            playerTotalTime = 0;
            playerMoveCount = 0;
            botTotalTime = 0;
            botMoveCount = 0;

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
        return user.UserDeck.FirstOrDefault(c =>
            c.Name == simCard.Name &&
            c.TypeOfCard == simCard.TypeOfCard &&
            c.ManaCost == simCard.ManaCost &&
            c.Damage == simCard.Damage &&
            c.Defense == simCard.Defense);
    }

    IEnumerator StartGame()
    {
        int turn = 1;
        string gamename = _gameNameText.text;
        SetBotNames();

        _gameNameText.SetText(_gameName);
        
        _deck.Shuffle();
        DealInitialCards();

        if (GameSettings.UseHiddenDecks)
        {
            _remainingDeckForBot = new List<Card>(_deck.Deck);
            _remainingDeckForPlayer = new List<Card>(_deck.Deck);
            _remainingDeckForBot.RemoveAll(card => _bot.UserDeck.Contains(card));
            _remainingDeckForBot.RemoveAll(card => _playedCards.Contains(card));

            _remainingDeckForPlayer.RemoveAll(card => _player.UserDeck.Contains(card));
            _remainingDeckForPlayer.RemoveAll(card => _playedCards.Contains(card));
        }

        InitStats();

        userStatsLogic(_player);
        userStatsLogic(_bot);

        printUserCards(_player, turn);
        printUserCards(_bot, turn);

        while (!_finished)
        {
            ApplyTurnStartBuffs();

            SetHiddenText();

            yield return StartCoroutine(PlayTurn(_player, _bot, PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1), PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1), turn));
            if (_finished) yield break;
            turn++;
            DrawNewCards();

            yield return StartCoroutine(PlayTurn(_bot, _player, PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1), PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1), turn));
            if (_finished) yield break;
            turn++;
            DrawNewCards();
            UpdateMana();
            TrackStaleProgress(turn);
        }
    }

    void SetBotNames()
    {
        int PlayerName = PlayerPrefs.GetInt("AlgorithmusEinsBotVsBot", 1);
        _player.Name = PlayerName == 1 ? "Minimax" : PlayerName == 2 ? "Minimax with Alpha-Beta" : "MCTS";

        int BotName = PlayerPrefs.GetInt("AlgorithmusZweiBotVsBot", 1);
        _bot.Name = BotName == 1 ? "Minimax" : BotName == 2 ? "Minimax with Alpha-Beta" : "MCTS";

        userStats.GetComponentInChildren<TextMeshProUGUI>().text = _player.Name;
        botStats.GetComponentInChildren<TextMeshProUGUI>().text = _bot.Name;
    }

    void DealInitialCards()
    {
        for (int i = 0; i < 5; i++)
        {
            _player.UserDeck.Add(_deck.DrawCard());
            _bot.UserDeck.Add(_deck.DrawCard());
        }
    }

    void InitStats()
    {
        _player.MaxMana = 50;
        _bot.MaxMana = 50;
        _player.ManaPoints = 25;
        _bot.ManaPoints = 25;
    }

    void ApplyTurnStartBuffs()
    {
        CombatUtils.ApplyStartOfTurnBuffs(_player.ActiveBuffs, ref _player._healthPoints);
        CombatUtils.ApplyStartOfTurnBuffs(_bot.ActiveBuffs, ref _bot._healthPoints);
    }

    void UpdateMana()
    {
        if (GameSettings.UseManaSystem)
        {
            _player.ManaPoints = Math.Min(_player.ManaPoints + 2, _player.MaxMana);
            _bot.ManaPoints = Math.Min(_bot.ManaPoints + 2, _bot.MaxMana);
        }
    }

    void SetHiddenText()
    {
        _hiddenField.GetComponentInChildren<TextMeshProUGUI>().text =
            $"Hidden Cards: {(GameSettings.UseHiddenDecks ? "yes" : "no")} " +
            $"| Mana System: {(GameSettings.UseManaSystem ? "yes" : "no")} " +
            $"| Buff/Debuff Cards: {(GameSettings.UseBuffDebuffCards ? "yes" : "no")}";
    }

    IEnumerator PlayTurn(User attacker, User defender, int attackerAlgo, int defenderAlgo, int turn)
    {
        Card attackCard = null;
        Card counterCard = null;

        if (attackerAlgo == 3)
        {
            yield return StartCoroutine(DecideCardMCTS(attacker, defender, false, true, Phase.BotAttack, card => attackCard = card));
        }
        else
        {
            attackCard = DecideCard(attacker, defender, attackerAlgo, false, true, Phase.BotAttack, 4);
        }

        PlaceCard(attacker, attackCard);
        printPlacedView();

        if (defenderAlgo == 3)
        {
            yield return StartCoroutine(DecideCardMCTS(defender, attacker, true, true, Phase.BotCounter, card => counterCard = card));
        }
        else
        {
            counterCard = DecideCard(defender, attacker, defenderAlgo, true, true, Phase.BotCounter, 3);
        }

        PlaceCounter(defender, counterCard);
        printPlacedViewCounter(counterCard);

        userStatsLogic(defender);
        userStatsLogic(attacker);
        hasUserPickedCard = false;

        if (checkIfWon(attacker, defender, turn, _gameName))
        {
            _finished = true;
            yield break;
        }

        printUserCards(attacker, turn);
        printUserCards(defender, turn);
    }

    Card DecideCard(User self, User enemy, int algo, bool isCountering, bool isBotTurn, Phase phase, int depth)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        Card resultCard = null;
        SimCard sim = null;

        GameState state = new GameState(
             self.HealthPoints,
             enemy.HealthPoints,
             self.ManaPoints,
             enemy.ManaPoints,
             self.Money,
             enemy.Money,
             self.UserDeck,
             GameSettings.UseHiddenDecks ? new List<Card>() : enemy.UserDeck,
             isCountering ? _placedCard.Damage : 0,
             0,
             self.ActiveBuffs,
             enemy.ActiveBuffs,
             (self == _player) ? _remainingDeckForPlayer : _remainingDeckForBot
             );

        if (algo == 1)
            sim = new Minimax().BotDecideMove(state, phase, depth);
        else if (algo == 2)
            sim = new Minimax().BotDecideMoveAlphaBeta(state, phase, depth);

        stopwatch.Stop();
        AddDecisionTime(self, stopwatch.Elapsed.TotalMilliseconds);


        if (sim != null)
        {
            Card match = MatchSimCardToRealCard(sim, self);
            resultCard = match ?? _skipCard;
        }
        else
        {
            Debug.LogWarning($"[{self.Name}] AI returned null SimCard, using _skipCard");
            resultCard = _skipCard;
        }

        return resultCard;
    }

    IEnumerator DecideCardMCTS(User self, User enemy, bool isCountering, bool isBotTurn, Phase phase, Action<Card> onComplete)
    {
        GameState state = new GameState(
             self.HealthPoints,
             enemy.HealthPoints,
             self.ManaPoints,
             enemy.ManaPoints,
             self.Money,
             enemy.Money,
             self.UserDeck,
             GameSettings.UseHiddenDecks ? new List<Card>() : enemy.UserDeck,
             isCountering ? _placedCard.Damage : 0,
             0,
             self.ActiveBuffs,
             enemy.ActiveBuffs,
             (self == _player) ? _remainingDeckForPlayer : _remainingDeckForBot
             );

        SimCard sim = null;
        bool finished = false;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var mctsRoutine = StartCoroutine(new MCTS().MCTSFindBestMoveCoroutine(state, phase, (SimCard result) =>
        {
            sim = result;
            finished = true;
        }));

        float timeout = 5f;
        float elapsed = 0f;

        while (!finished && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        stopwatch.Stop();
        AddDecisionTime(self, stopwatch.Elapsed.TotalMilliseconds);

        if (!finished)
        {
            Debug.LogWarning($"MCTS timeout for {self.Name}, using _skipCard fallback.");
            StopCoroutine(mctsRoutine);
            onComplete(_skipCard);
        }
        else
        {
            Card match = sim != null ? MatchSimCardToRealCard(sim, self) : null;
            onComplete(match ?? _skipCard);
            Debug.Log($"[MCTS Decision] For {self.Name}: {match?.Name ?? _skipCard.Name}");
        }
    }

    void AddDecisionTime(User user, double timeMs)
    {
        if (user == _player)
        {
            playerTotalTime += timeMs;
            playerMoveCount++;
        }
        else if (user == _bot)
        {
            botTotalTime += timeMs;
            botMoveCount++;
        }
    }

    void DrawNewCards()
    {
        try
        {
            if (_player.UserDeck.Count < 7)
            {
                Card drawn = _deck.DrawCard();
                _player.UserDeck.Add(drawn);
                if (GameSettings.UseHiddenDecks)
                {
                    _remainingDeckForPlayer.Remove(drawn);
                }
            }
            if (_bot.UserDeck.Count < 7)
            {
                Card drawn = _deck.DrawCard();
                _bot.UserDeck.Add(drawn);
                if (GameSettings.UseHiddenDecks)
                {
                    _remainingDeckForBot.Remove(drawn);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Draw failed: " + ex.Message);
        }
    }

    void TrackStaleProgress(int turn)
    {
        bool progressHappened =
            _player.HealthPoints < lastPlayerHP ||
            _bot.HealthPoints < lastBotHP ||
            _player.UserDeck.Count != lastPlayerCards ||
            _bot.UserDeck.Count != lastBotCards ||
            _player.ManaPoints != lastPlayerMana ||
            _bot.ManaPoints != lastBotMana;

        if (!progressHappened && ++staleTurnCount >= 10)
        {
            _finished = true;
            Debug.Log("Deadlock erkannt – Spiel wird als Unentschieden gewertet.");
            allGameResults.Add(new MetricsState { gamename = _gameName, turns = turn, winner = "Draw", loser = "Draw" });
        }
        else if (progressHappened) staleTurnCount = 0;

        lastPlayerHP = _player.HealthPoints;
        lastBotHP = _bot.HealthPoints;
        lastPlayerCards = _player.UserDeck.Count;
        lastBotCards = _bot.UserDeck.Count;
        lastPlayerMana = _player.ManaPoints;
        lastBotMana = _bot.ManaPoints;
    }

    public void PlaceCard(User user, Card card)
    {
        if (GameSettings.UseManaSystem && user.ManaPoints < card.ManaCost)
        {
            Debug.Log("Not enough mana for this card.");
            return;
        }

        if (GameSettings.UseManaSystem)
        {
            user.ManaPoints -= card.ManaCost;
        }

        user.UserDeck.Remove(card);
        
        if (GameSettings.UseHiddenDecks)
        {
            _playedCards.Add(card);

            if (user == _player)
                _remainingDeckForBot.Remove(card);
            else if (user == _bot)
                _remainingDeckForPlayer.Remove(card);
        }

        if (_placedCard == null && _placedCardUser == null)
        {
            _placedCard = card;
            _placedCardUser = user;
        }

        if (card.TypeOfCard == TypeOfCard.Special)
        {
            ApplySpecialCard(user, card);
        }

        if ((card.TypeOfCard == TypeOfCard.Buff || card.TypeOfCard == TypeOfCard.Debuff) && GameSettings.UseBuffDebuffCards)
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
        Match match = Regex.Match(card.Name, @"(HP|MP|GP)(\d+)");
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
        Match match = Regex.Match(name, @"(DOT|HOT|DEF|ATK)[+-]?(\d+)D(\d+)");
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
        if (user1.HealthPoints <= 0)
        {
            _finished = true;
            var metricsState = new MetricsState
            {
                gamename = gamename,
                turns = turn,
                winner = user2.Name,
                winnerAvgTime = user2 == _player ? playerTotalTime / Math.Max(1, playerMoveCount) : botTotalTime / Math.Max(1, botMoveCount),
                loser = user1.Name,
                loserAvgTime = user1 == _player ? playerTotalTime / Math.Max(1, playerMoveCount) : botTotalTime / Math.Max(1, botMoveCount)
            };
            allGameResults.Add(metricsState);

            if (user2 == _player)
            {
                p1_win++;
                _winsOne.text = $"Wins: {p1_win}";
            }
            else if (user2 == _bot)
            {
                p2_win++;
                _winsTwo.text = $"Wins: {p2_win}";
            }

            return true;
        }

        if (user2.HealthPoints <= 0)
        {
            _finished = true;
            var metricsState = new MetricsState
            {
                gamename = gamename,
                turns = turn,
                winner = user1.Name,
                winnerAvgTime = user1 == _player ? playerTotalTime / Math.Max(1, playerMoveCount) : botTotalTime / Math.Max(1, botMoveCount),
                loser = user2.Name,
                loserAvgTime = user2 == _player ? playerTotalTime / Math.Max(1, playerMoveCount) : botTotalTime / Math.Max(1, botMoveCount)
            };
            allGameResults.Add(metricsState);

            if (user1 == _player)
            {
                p1_win++;
                _winsOne.text = $"Wins: {p1_win}";
            }
            else if (user1 == _bot)
            {
                p2_win++;
                _winsTwo.text = $"Wins: {p2_win}";
            }

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
        public double winnerAvgTime;
        public string loser;
        public double loserAvgTime;
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
            if (GameSettings.UseManaSystem && user.ManaPoints < card.ManaCost)
            {
                Debug.Log("Not enough mana for this card.");
                return;
            }

            if (GameSettings.UseManaSystem)
            {
                user.ManaPoints -= card.ManaCost;
            }

            user.UserDeck.Remove(card);

            if (GameSettings.UseHiddenDecks)
            {
                _playedCards.Add(card);

                if (user == _player)
                    _remainingDeckForBot.Remove(card);
                else if (user == _bot)
                    _remainingDeckForPlayer.Remove(card);
            }

            int attack = CombatUtils.GetEffectiveAttack(_placedCard.Damage, _placedCardUser.ActiveBuffs);
            int defense = CombatUtils.GetEffectiveDefense(card.Defense, user.ActiveBuffs);
            int netDamage = CombatUtils.CalculateNetDamage(attack, defense);
            user.HealthPoints -= netDamage;
        }
        else
        {
            Debug.LogWarning($"No attack to counter for {user.Name} with {card?.Name ?? "null"}");
        }

        _placedCard = null;
        _placedCardUser = null;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;

public class GameState
{
    public int playerHealth;
    public int botHealth;
    public int playerMana;
    public int botMana;
    public int playerMoney;
    public int botMoney;

    public List<SimCard> playerHand;
    public List<SimCard> botHand;

    public int last_player_damage;
    public int last_bot_damage;

    public List<Buff> playerBuffs;
    public List<Buff> botBuffs;

    public List<HistoryEntry> History { get; set; } = new List<HistoryEntry>();

    public List<SimCard> remainingDeckForEval;

    public GameState()
    {
        botHand = new List<SimCard>();
        playerHand = new List<SimCard>();
        botBuffs = new List<Buff>();
        playerBuffs = new List<Buff>();
    }

    public GameState(int botHealth, int playerHealth, int botMana, int playerMana, int botMoney, int playerMoney,
        List<Card> botCards, List<Card> playerCards, int lastBotDmg, int lastPlayerDmg,
        List<Buff> botBuffs, List<Buff> playerBuffs,
        List<Card> remainingDeckForEval = null)
    {
        this.botHealth = botHealth;
        this.playerHealth = playerHealth;
        this.botMana = botMana;
        this.playerMana = playerMana;
        this.botMoney = botMoney;
        this.playerMoney = playerMoney;
        this.last_bot_damage = lastBotDmg;
        this.last_player_damage = lastPlayerDmg;
        this.botHand = botCards.Select(c => new SimCard(c)).ToList();
        this.playerHand = playerCards.Select(c => new SimCard(c)).ToList();
        this.botBuffs = botBuffs.Select(b => b.Clone()).ToList();
        this.playerBuffs = playerBuffs.Select(b => b.Clone()).ToList();
        this.remainingDeckForEval = remainingDeckForEval?.Select(c => new SimCard(c)).ToList() ?? new List<SimCard>();
    }


    public GameState Clone()
    {
        return new GameState
        {
            botHealth = this.botHealth,
            playerHealth = this.playerHealth,
            botMana = this.botMana,
            playerMana = this.playerMana,
            botMoney = this.botMoney,
            playerMoney = this.playerMoney,
            last_bot_damage = this.last_bot_damage,
            last_player_damage = this.last_player_damage,
            botHand = this.botHand.Select(card => card.Clone()).ToList(),
            playerHand = this.playerHand.Select(card => card.Clone()).ToList(),
            botBuffs = this.botBuffs.Select(b => b.Clone()).ToList(),
            playerBuffs = this.playerBuffs.Select(b => b.Clone()).ToList(),
            History = this.History?.Select(h => h.Clone()).ToList() ?? new List<HistoryEntry>(),
            remainingDeckForEval = this.remainingDeckForEval?.Select(c => c.Clone()).ToList() ?? new List<SimCard>()
        };
    }

    public class HistoryEntry
    {
        public int BotHealth;
        public int PlayerHealth;
        public int BotMana;
        public int PlayerMana;
        public int BotCards;
        public int PlayerCards;

        public HistoryEntry() { }

        public HistoryEntry(GameState state)
        {
            BotHealth = state.botHealth;
            PlayerHealth = state.playerHealth;
            BotMana = state.botMana;
            PlayerMana = state.playerMana;
            BotCards = state.botHand.Count;
            PlayerCards = state.playerHand.Count;
        }

        public HistoryEntry Clone()
        {
            return new HistoryEntry
            {
                BotHealth = this.BotHealth,
                PlayerHealth = this.PlayerHealth,
                BotMana = this.BotMana,
                PlayerMana = this.PlayerMana,
                BotCards = this.BotCards,
                PlayerCards = this.PlayerCards
            };
        }
    }

    public GameState DeepCopy()
    {
        return Clone();
    }

    public void AdvanceTurn()
    {
        if (GameSettings.UseManaSystem)
        {
            botMana = Math.Min(botMana + 2, 50);
            playerMana = Math.Min(playerMana + 2, 50);
        }

        ApplyBuffs(botBuffs, ref botHealth);
        ApplyBuffs(playerBuffs, ref playerHealth);
    }

    private void ApplyBuffs(List<Buff> buffs, ref int health)
    {
        if (!GameSettings.UseBuffDebuffCards) return;

        var expired = new List<Buff>();
        foreach (var buff in buffs)
        {
            if (buff.Type == BuffType.DamageOverTime) health -= buff.Value;
            else if (buff.Type == BuffType.HealOverTime) health += buff.Value;
            buff.Tick();
            if (buff.IsExpired) expired.Add(buff);
        }
        foreach (var buff in expired)
            buffs.Remove(buff);
    }

    public int GetEffectiveBotDefense(int baseDefense)
    {
        int bonus = GameSettings.UseBuffDebuffCards
            ? botBuffs.Where(b => b.Type == BuffType.DefenseBoost).Sum(b => b.Value)
            : 0;
        return baseDefense + bonus;
    }

    public int GetEffectivePlayerDefense(int baseDefense)
    {
        int bonus = GameSettings.UseBuffDebuffCards
            ? playerBuffs.Where(b => b.Type == BuffType.DefenseBoost).Sum(b => b.Value)
            : 0;
        return baseDefense + bonus;
    }

    public void ApplyBuffCard(SimCard card, bool isBotTurn)
    {
        if (!GameSettings.UseBuffDebuffCards) return;

        Buff buff = ParseBuffFromCardName(card.Name);
        if (buff == null) return;

        buff.IsDebuff = card.TypeOfCard == TypeOfCard.Debuff;
        var target = (buff.IsDebuff ^ isBotTurn) ? playerBuffs : botBuffs;
        target.Add(buff);
    }

    private Buff ParseBuffFromCardName(string name)
    {
        var match = Regex.Match(name, @"(DOT|HOT|DEF|ATK)[+-]?(\d+)D(\d+)");
        if (!match.Success) return null;

        var type = match.Groups[1].Value switch
        {
            "DOT" => BuffType.DamageOverTime,
            "HOT" => BuffType.HealOverTime,
            "DEF" => BuffType.DefenseBoost,
            "ATK" => BuffType.AttackBoost,
            _ => throw new Exception("Unknown buff type")
        };

        return new Buff(
            type,
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value),
            false
        );
    }
}
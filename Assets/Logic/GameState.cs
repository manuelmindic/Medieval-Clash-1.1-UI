using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class GameState
{
    public int botHealth;
    public int playerHealth;
    public int botMana;
    public int playerMana;
    public int botMoney;
    public int playerMoney;

    public List<SimCard> botHand;
    public List<SimCard> playerHand;

    public int last_bot_damage;
    public int last_player_damage;

    public List<Buff> botBuffs;
    public List<Buff> playerBuffs;

    public List<GameState> History = new List<GameState>();

    public GameState()
    {
        botHand = new List<SimCard>();
        playerHand = new List<SimCard>();
        botBuffs = new List<Buff>();
        playerBuffs = new List<Buff>();
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
            History = this.History.Select(h => h.DeepCopy()).ToList()
        };
    }

    public GameState DeepCopy()
    {
        return Clone();
    }

    public void AdvanceTurn()
    {
        botMana = Math.Min(botMana + 1, 20);
        playerMana = Math.Min(playerMana + 1, 20);

        ApplyBuffs(botBuffs, ref botHealth);
        ApplyBuffs(playerBuffs, ref playerHealth);
    }

    private void ApplyBuffs(List<Buff> buffs, ref int health)
    {
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
        int bonus = botBuffs.Where(b => b.Type == BuffType.DefenseBoost).Sum(b => b.Value);
        return baseDefense + bonus;
    }

    public int GetEffectivePlayerDefense(int baseDefense)
    {
        int bonus = playerBuffs.Where(b => b.Type == BuffType.DefenseBoost).Sum(b => b.Value);
        return baseDefense + bonus;
    }

    public void ApplyBuffCard(SimCard card, bool isBotTurn)
    {
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

        return new Buff
        {
            Type = type,
            Value = int.Parse(match.Groups[2].Value),
            Duration = int.Parse(match.Groups[3].Value),
            IsDebuff = false
        };
    }
}
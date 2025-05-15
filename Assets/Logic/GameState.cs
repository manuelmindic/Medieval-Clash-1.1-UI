using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

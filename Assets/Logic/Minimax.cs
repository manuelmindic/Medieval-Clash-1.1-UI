using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.MPE;
using UnityEngine;

public enum Phase
{
    PlayerAttack,
    BotCounter,
    BotAttack,
    PlayerCounter
}

public class Minimax
{
    // Minimax v1
    public SimCard BotDecideMove(GameState state, Phase phase, int depth)
    {
        int bestScore = int.MinValue;
        SimCard bestCard = null;
        List<SimCard> cards = new List<SimCard>(state.botHand);
        
        foreach (var card in cards)
        {
            if (!IsValidMove(card, phase == Phase.BotCounter ? true : false, state.botMana)) continue;

            GameState newState = state.Clone();
            ApplyPlay(newState, "bot", card);  // true = player perspective

            if (phase == Phase.BotCounter && newState.last_bot_damage > 0)
                newState.botHand.Remove(card);
            else if (phase == Phase.BotAttack)
                newState.botHand.Remove(card);

            if (phase == Phase.BotCounter)
                newState = ResolveState(newState, "bot", card);

            int score = Minimaxv1(newState, depth - 1, NextPhase(phase));
            if (score >= bestScore)
            {
                bestScore = score;
                bestCard = card;
            }
        }

        bool hasDefenseCards = state.botHand.Any(c => c.TypeOfCard == TypeOfCard.Defense);
        if (!hasDefenseCards)
        {
            Debug.LogWarning($"Bot has NO defense cards!");
        }

        return bestCard;
    }

    private SimCard SamplePlayerMove(GameState state, Phase phase)
    {
        if (!GameSettings.UseHiddenDecks)
        {
            var hand = state.playerHand;
            var manaPoints = state.playerMana;
            var valid = hand
                .Where(c => IsValidMove(c, phase == Phase.PlayerCounter, manaPoints))
                .ToList();

            return valid.Any() ? valid[UnityEngine.Random.Range(0, valid.Count)].Clone() : null;
        }

        var deck = state.remainingDeckForEval;
        var mana = state.playerMana;

        if (deck == null || !deck.Any()) return null;

        var validTypes = new List<TypeOfCard>();
        if (phase == Phase.PlayerAttack)
        {
            validTypes.Add(TypeOfCard.Attack);
            if (GameSettings.UseBuffDebuffCards)
            {
                validTypes.Add(TypeOfCard.Buff);
                validTypes.Add(TypeOfCard.Debuff);
            }
            validTypes.Add(TypeOfCard.Special);
        }
        else if (phase == Phase.PlayerCounter)
        {
            validTypes.Add(TypeOfCard.Defense);
        }

        var filteredDeck = deck
            .Where(c => validTypes.Contains(c.TypeOfCard) && (!GameSettings.UseManaSystem || c.ManaCost <= mana))
            .ToList();

        if (!filteredDeck.Any()) return null;

        var typeCounts = filteredDeck.GroupBy(c => c.TypeOfCard)
            .ToDictionary(g => g.Key, g => g.Count());

        int totalCount = typeCounts.Values.Sum();

        var weightedCards = new List<SimCard>();
        foreach (var card in filteredDeck)
        {
            int weight = typeCounts[card.TypeOfCard];
            for (int i = 0; i < weight; i++)
                weightedCards.Add(card);
        }

        var sampled = weightedCards[UnityEngine.Random.Range(0, weightedCards.Count)];
        return sampled.Clone();
    }

    private int Minimaxv1(GameState state, int depth, Phase phase)
    {
        if (depth == 0 || IsSimOver(state.botHealth, state.playerHealth))
            return EvalV1(state);

        // Minimizer: Simulate player moves with sampling
        if (phase == Phase.PlayerAttack || phase == Phase.PlayerCounter)
        {
            int minEval = int.MaxValue;

            // Sample multiple times for probabilistic evaluation
            const int samples = 3;
            for (int i = 0; i < samples; i++)
            {
                var sampledCard = SamplePlayerMove(state, phase);
                if (sampledCard == null) continue;

                GameState newState = state.Clone();
                ApplyPlay(newState, "player", sampledCard);

                if (phase == Phase.PlayerCounter)
                    newState = ResolveState(newState, "player", sampledCard);

                int eval = Minimaxv1(newState, depth - 1, NextPhase(phase));
                minEval = Math.Min(minEval, eval);
            }

            return minEval;
        }
        // Maximizer
        if (phase == Phase.BotAttack || phase == Phase.BotCounter)
        {
            int maxEval = int.MinValue;
            List<SimCard> cards = new List<SimCard>(state.botHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, phase == Phase.BotAttack ? false : true, state.botMana)) continue;

                GameState newState = state.Clone();
                ApplyPlay(newState, "bot", card);

                if (phase == Phase.BotCounter)
                {
                    if(newState.last_bot_damage > 0){
                        newState.botHand.Remove(card);
                    }
                    newState = ResolveState(newState, "bot", card);

                }
                else
                {
                    newState.botHand.Remove(card);
                }

                int eval = Minimaxv1(newState, depth - 1, NextPhase(phase));
                maxEval = Math.Max(maxEval, eval);
            }
            return maxEval;
        }
        return 0;
    }

    private int EvalV1(GameState state)
    {
        int buffScore = 0;
        int manaScore = 0;
        if (GameSettings.UseManaSystem)
        {
            buffScore = state.botBuffs.Sum(b => b.IsDebuff ? -b.Value : b.Value) -
                state.playerBuffs.Sum(b => b.IsDebuff ? -b.Value : b.Value);
        }

        if (GameSettings.UseManaSystem)
        {
            manaScore = state.botMana - state.playerMana;
        }

        return (state.botHealth - state.playerHealth) +
               manaScore +
               buffScore;
    }


    private GameState ResolveState(GameState state, string isPlayer, SimCard card)
    {
        if (isPlayer == "player")
        {
            state.playerHealth -= Math.Max(0, state.last_player_damage - state.GetEffectivePlayerDefense(card.Defense));
            state.last_player_damage = 0;
        }
        else if (isPlayer == "bot")
        {
            state.botHealth -= Math.Max(0, state.last_bot_damage - state.GetEffectiveBotDefense(card.Defense));
            state.last_bot_damage = 0;
        } 
        return state;
    }

    private void ApplySpecial(ref int hp, ref int mp, ref int gp, SimCard card)
    {
        Match match = Regex.Match(card.Name, @"(HP|MP|GP)(\d+)");
        if (!match.Success) return;

        string type = match.Groups[1].Value;
        int val = int.Parse(match.Groups[2].Value);
        if (type == "HP") hp += val;
        else if (type == "MP") mp += val;
        else if (type == "GP") gp += val;
    }

    private GameState ApplyPlay(GameState state, string isPlayer, SimCard card)
    {
        if (isPlayer == "player")
        {
            if (GameSettings.UseManaSystem)
            {
                state.playerMana -= card.ManaCost;
            }
            if (card.TypeOfCard == TypeOfCard.Attack)
                state.last_bot_damage = card.Damage;
            else if (card.TypeOfCard == TypeOfCard.Special)
                ApplySpecial(ref state.playerHealth, ref state.playerMana, ref state.playerMoney, card);
            else if ((card.TypeOfCard == TypeOfCard.Buff || card.TypeOfCard == TypeOfCard.Debuff) && GameSettings.UseBuffDebuffCards)
                state.ApplyBuffCard(card, true);
            else if (card.TypeOfCard == TypeOfCard.Defense)
                state.last_player_damage = Math.Max(0, state.last_player_damage);
        }
        else if(isPlayer == "bot")
        {
            if (GameSettings.UseManaSystem)
            {
                state.botMana -= card.ManaCost;
            }
            if (card.TypeOfCard == TypeOfCard.Attack)
                state.last_player_damage = card.Damage;
            else if (card.TypeOfCard == TypeOfCard.Special)
                ApplySpecial(ref state.botHealth, ref state.botMana, ref state.botMoney, card);
            else if ((card.TypeOfCard == TypeOfCard.Buff || card.TypeOfCard == TypeOfCard.Debuff) && GameSettings.UseBuffDebuffCards)
                state.ApplyBuffCard(card, false);
            else if (card.TypeOfCard == TypeOfCard.Defense)
                state.last_bot_damage = Math.Max(0, state.last_bot_damage);
        }
        return state;
    }

    public Phase NextPhase(Phase phase) => phase switch
    {
        Phase.PlayerAttack => Phase.BotCounter,
        Phase.BotCounter => Phase.BotAttack,
        Phase.BotAttack => Phase.PlayerCounter,
        Phase.PlayerCounter => Phase.PlayerAttack,
        _ => phase
    };

    private bool IsSimOver(int botHealth, int playerHealth)
    {
        return botHealth <= 0 || playerHealth <= 0;
    }

    private bool IsValidMove(SimCard card, bool isCounter, int mana)
    {
        if (GameSettings.UseManaSystem && card.ManaCost > mana) return false;

        return isCounter
            ? card.TypeOfCard == TypeOfCard.Defense
            : card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special || card.TypeOfCard == TypeOfCard.Buff || card.TypeOfCard == TypeOfCard.Debuff;
    }

    // Minimax Alpha-Beta-Pruning
    public SimCard BotDecideMoveAlphaBeta(GameState state, Phase phase, int depth)
    {
        int bestScore = int.MinValue;
        SimCard bestCard = null;

        List<SimCard> cards = new List<SimCard>(state.botHand);
        foreach (var card in cards)
        {
            if (!IsValidMove(card, phase == Phase.BotCounter, state.botMana)) continue;

            GameState newState = state.Clone();
            ApplyPlay(newState, "bot", card);

            if (phase == Phase.BotCounter && newState.last_bot_damage > 0)
                newState.botHand.Remove(card);
            else if (phase == Phase.BotAttack)
                newState.botHand.Remove(card);

            if (phase == Phase.BotCounter)
                newState = ResolveState(newState, "bot", card);

            int score = MinimaxAB(newState, depth - 1, NextPhase(phase), int.MinValue, int.MaxValue);

            if (score >= bestScore)
            {
                bestScore = score;
                bestCard = card;
            }
        }

        bool hasDefenseCards = state.botHand.Any(c => c.TypeOfCard == TypeOfCard.Defense);
        if (!hasDefenseCards)
        {
            Debug.LogWarning($"Bot has NO defense cards!");
        }

        return bestCard;
    }

    private int MinimaxAB(GameState state, int depth, Phase phase, int alpha, int beta)
    {
        if (depth == 0 || IsSimOver(state.botHealth, state.playerHealth))
            return EvalV1(state);

        // Minimizer
        if (phase == Phase.PlayerAttack || phase == Phase.PlayerCounter)
        {
            int minEval = int.MaxValue;

            const int samples = 3;
            for (int i = 0; i < samples; i++)
            {
                var sampledCard = SamplePlayerMove(state, phase);
                if (sampledCard == null) continue;

                GameState newState = state.Clone();
                ApplyPlay(newState, "player", sampledCard);

                if (phase == Phase.PlayerCounter)
                    newState = ResolveState(newState, "player", sampledCard);

                int eval = MinimaxAB(newState, depth - 1, NextPhase(phase), alpha, beta);
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
        // Maximizer
        if (phase == Phase.BotAttack || phase == Phase.BotCounter) {
            
            int maxEval = int.MinValue;
            List<SimCard> cards = new List<SimCard>(state.botHand);

            foreach (var card in cards)
            {
                if (!IsValidMove(card, phase == Phase.BotAttack ? false : true, state.botMana)) continue;

                GameState newState = state.Clone();
                ApplyPlay(newState, "bot", card);

                if (phase == Phase.BotCounter) {
                
                    if(newState.last_bot_damage > 0)
                    {
                
                        newState.botHand.Remove(card);
                    
                    }

                    newState = ResolveState(newState, "bot", card);
                
                } 
                else {
                    
                    newState.botHand.Remove(card);
                
                }
                
                int eval = MinimaxAB(newState, depth - 1, NextPhase(phase), alpha, beta);
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
        return 0;
    }
}

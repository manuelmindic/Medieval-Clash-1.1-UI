using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System;
using static Game;

public class Minimax
{
    // Minimax v1
    public Card BotDecideMove(GameState state, string phase, int depth)
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
            else if (phase == "bot_attack")
            {
                newGamestate.botHand.Remove(card);
            }

            if (phase == "bot_counter")
            {
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
        if (depth == 0 || IsSimOver(state.botHealth, state.playerHealth))
        {
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
                ApplyPlay(newBotCounterState, "bot", card);

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
            List<Card> cards = new List<Card>(state.botHand);
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
            else if (card.TypeOfCard == TypeOfCard.Defense)
            {
                state.last_player_damage = Math.Max(0, state.last_player_damage);
            }
        }
        else if (turn == "bot")
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

    private bool IsValidMove(Card card, bool isBotCountering)
    {
        if (isBotCountering)
            return card.TypeOfCard == TypeOfCard.Defense;
        else
            return card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special;
    }


    // Minimax Alpha-Beta-Pruning
    public Card BotDecideMoveAlphaBeta(GameState state, string phase, int depth)
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
}

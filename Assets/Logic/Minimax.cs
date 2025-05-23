using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

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
    public Card BotDecideMove(GameState state, Phase phase, int depth)
    {
        int bestScore = int.MinValue;
        Card bestCard = null;

        List<Card> cards = new List<Card>(state.botHand);
        bool hasValidMove = false;
        int currentMana = state.botMana;

        foreach (var card in cards)
        {
            if (!IsValidMove(card, phase == Phase.BotCounter, currentMana)) continue;
            hasValidMove = true;

            GameState newGamestate = state.Clone();
            ApplyPlay(newGamestate, "bot", card);

            if (phase == Phase.BotCounter && newGamestate.last_bot_damage > 0)
                newGamestate.botHand.Remove(card);
            else if (phase == Phase.BotAttack)
                newGamestate.botHand.Remove(card);

            if (phase == Phase.BotCounter)
                newGamestate = ResolveState(newGamestate, "bot", card);

            int score = Minimaxv1(newGamestate, depth - 1, NextPhase(phase));

            if (score >= bestScore)
            {
                bestCard = card;
                bestScore = score;
            }
        }

        return hasValidMove ? bestCard : null;
    }

    private int Minimaxv1(GameState state, int depth, Phase phase)
    {
        if (depth == 0 || IsSimOver(state.botHealth, state.playerHealth))
            return EvalV1(state);

        List<Card> cards;
        GameState newState;

        // Minimizer
        if (phase == Phase.PlayerAttack || phase == Phase.PlayerCounter)
        {
            int minEval = int.MaxValue;
            cards = new List<Card>(state.playerHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, phase == Phase.PlayerCounter, state.playerMana)) continue;

                newState = state.Clone();
                ApplyPlay(newState, "player", card);

                if (phase == Phase.PlayerCounter && newState.last_player_damage > 0)
                    newState.playerHand.Remove(card);
                else if (phase == Phase.PlayerAttack)
                    newState.playerHand.Remove(card);

                if (phase == Phase.PlayerCounter)
                    newState = ResolveState(newState, "player", card);

                int eval = Minimaxv1(newState, depth - 1, NextPhase(phase));
                minEval = Math.Min(minEval, eval);
            }
            return minEval;
        }
        // Maximizer
        else
        {
            int maxEval = int.MinValue;
            cards = new List<Card>(state.botHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, phase == Phase.BotCounter, state.botMana)) continue;

                newState = state.Clone();
                ApplyPlay(newState, "bot", card);

                if (phase == Phase.BotCounter && newState.last_bot_damage > 0)
                    newState.botHand.Remove(card);
                else if (phase == Phase.BotAttack)
                    newState.botHand.Remove(card);

                if (phase == Phase.BotCounter)
                    newState = ResolveState(newState, "bot", card);

                int eval = Minimaxv1(newState, depth - 1, NextPhase(phase));
                maxEval = Math.Max(maxEval, eval);
            }
            return maxEval;
        }
    }

    private int EvalV1(GameState state)
    {
        return state.botHealth - state.playerHealth;
    }

    private GameState ResolveState(GameState state, string role, Card card)
    {
        int damage = role == "player" ? state.last_player_damage : state.last_bot_damage;
        int defense = card.Defense;
        int netDamage = Math.Max(0, damage - defense);

        if (role == "player")
        {
            state.playerHealth = Math.Max(0, state.playerHealth - netDamage);
            state.last_player_damage = 0;
        }
        else
        {
            state.botHealth = Math.Max(0, state.botHealth - netDamage);
            state.last_bot_damage = 0;
        }

        return state;
    }

    private void ApplySpecial(ref int hp, ref int mp, ref int gp, Card card)
    {
        Match match = Regex.Match(card.Name, @"(\D+)(\d+)");
        if (!match.Success) return;

        string prefix = match.Groups[1].Value;
        int value = int.Parse(match.Groups[2].Value);

        switch (prefix)
        {
            case "HP": hp += value; break;
            case "MP": mp += value; break;
            case "GP": gp += value; break;
        }
    }

    private GameState ApplyPlay(GameState state, string turn, Card card)
    {
        if (turn == "player")
        {
            if (card.TypeOfCard == TypeOfCard.Attack)
                state.last_bot_damage = card.Damage;
            else if (card.TypeOfCard == TypeOfCard.Special)
                ApplySpecial(ref state.playerHealth, ref state.playerMana, ref state.playerMoney, card);
        }
        else if (turn == "bot")
        {
            if (card.TypeOfCard == TypeOfCard.Attack)
                state.last_player_damage = card.Damage;
            else if (card.TypeOfCard == TypeOfCard.Special)
                ApplySpecial(ref state.botHealth, ref state.botMana, ref state.botMoney, card);
        }
        return state;
    }

    public Phase NextPhase(Phase phase)
    {
        return phase switch
        {
            Phase.PlayerAttack => Phase.BotCounter,
            Phase.BotCounter => Phase.BotAttack,
            Phase.BotAttack => Phase.PlayerCounter,
            Phase.PlayerCounter => Phase.PlayerAttack,
            _ => phase
        };
    }

    private bool IsSimOver(int botHealth, int playerHealth)
    {
        return botHealth <= 0 || playerHealth <= 0;
    }

    private bool IsValidMove(Card card, bool isCountering, int mana)
    {
        if (card.ManaCost > mana) return false;

        return isCountering
            ? card.TypeOfCard == TypeOfCard.Defense
            : card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special;
    }


    // Minimax Alpha-Beta-Pruning
    public Card BotDecideMoveAlphaBeta(GameState state, Phase phase, int depth)
    {
        int bestScore = int.MinValue;
        Card bestCard = null;

        List<Card> cards = new List<Card>(state.botHand);
        bool hasValidMove = false;
        int currentMana = state.botMana;

        foreach (var card in cards)
        {
            if (!IsValidMove(card, phase == Phase.BotCounter, currentMana)) continue;
            hasValidMove = true;

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

        return hasValidMove ? bestCard : null;
    }

    private int MinimaxAB(GameState state, int depth, Phase phase, int alpha, int beta)
    {
        if (depth == 0 || IsSimOver(state.botHealth, state.playerHealth))
            return EvalV1(state);

        List<Card> cards;
        GameState newState;

        // Minimizer
        if (phase == Phase.PlayerAttack || phase == Phase.PlayerCounter)
        {
            int minEval = int.MaxValue;
            cards = new List<Card>(state.playerHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, phase == Phase.PlayerCounter, state.playerMana)) continue;

                newState = state.Clone();
                ApplyPlay(newState, "player", card);

                if (phase == Phase.PlayerCounter && newState.last_player_damage > 0)
                    newState.playerHand.Remove(card);
                else if (phase == Phase.PlayerAttack)
                    newState.playerHand.Remove(card);

                if (phase == Phase.PlayerCounter)
                    newState = ResolveState(newState, "player", card);

                int eval = MinimaxAB(newState, depth - 1, NextPhase(phase), alpha, beta);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                if (beta <= alpha) break;
            }
            return minEval;
        }
        // Maximizer
        else
        {
            int maxEval = int.MinValue;
            cards = new List<Card>(state.botHand);
            foreach (var card in cards)
            {
                if (!IsValidMove(card, phase == Phase.BotCounter, state.botMana)) continue;

                newState = state.Clone();
                ApplyPlay(newState, "bot", card);

                if (phase == Phase.BotCounter && newState.last_bot_damage > 0)
                    newState.botHand.Remove(card);
                else if (phase == Phase.BotAttack)
                    newState.botHand.Remove(card);

                if (phase == Phase.BotCounter)
                    newState = ResolveState(newState, "bot", card);

                int eval = MinimaxAB(newState, depth - 1, NextPhase(phase), alpha, beta);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha) break;
            }
            return maxEval;
        }
    }
}

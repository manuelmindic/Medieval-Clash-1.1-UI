using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static GameState;

public class MCTS
{
    private const int simulationCount = 2000;
    private const int batchSize = 500;
    private static System.Random _random = new System.Random();

    public IEnumerator MCTSFindBestMoveCoroutine(GameState rootState, Phase currentPhase, Action<SimCard> onComplete)
    {
        var rootNode = new MCTSNode(rootState, null, currentPhase);
        int iterations = 0;

        while (iterations < simulationCount)
        {
            for (int i = 0; i < batchSize && iterations < simulationCount; i++, iterations++)
            {
                var selectedNode = MCTSSelectNode(rootNode);
                var expandedNode = MCTSExpandNode(selectedNode);
                var result = MCTSSimulate(expandedNode);
                MCTSBackpropagate(expandedNode, result);
            }

            if (iterations % batchSize == 0)
            {
                Debug.Log($"MCTS progress: {iterations}/{simulationCount} simulations");
                Debug.Log($"Card progress: {rootState.remainingDeckForEval.Count}");
                yield return null;
            }
        }

        yield return null;

        var bestMove = rootNode.Children.OrderByDescending(child => child.Visits).FirstOrDefault()?.Move;
        onComplete?.Invoke(bestMove);
    }

    private MCTSNode MCTSSelectNode(MCTSNode node)
    {
        while (node.Children.Any())
            node = MCTSUCTSelect(node);
        return node;
    }

    private MCTSNode MCTSExpandNode(MCTSNode node)
    {
        var validMoves = node.GetValidMoves();
        int childCount = 0;

        foreach (var move in validMoves)
        {
            if (childCount >= 20) break;

            var clonedState = node.State.Clone();
            SimulateMove(clonedState, node.Phase, move, node.Move);

            var nextPhase = NextPhase(node.Phase);
            node.Children.Add(new MCTSNode(clonedState, move, nextPhase, node));

            childCount++;
        }

        return node.Children.Any() ? node.Children[_random.Next(node.Children.Count)] : node;
    }

    private double MCTSSimulate(MCTSNode node)
    {
        GameState sim = node.State.Clone();
        Phase phase = node.Phase;
        SimCard lastAttackCard = node.Move;
        int turns = 0;

        var sampledOpponentHand = SampleFullOpponentHand(sim, phase, node.State.remainingDeckForEval);

        while (!IsGameOver(sim.botHealth, sim.playerHealth) && turns++ < 100)
        {
            var currentHand = (phase == Phase.BotAttack || phase == Phase.BotCounter) ? sim.botHand : sampledOpponentHand;
            var currentMana = (phase == Phase.BotAttack || phase == Phase.BotCounter) ? sim.botMana : sim.playerMana;

            var validMoves = currentHand
                .Where(card => MCTSNode.IsValidMove(card, phase) && (!GameSettings.UseManaSystem || card.ManaCost <= currentMana))
                .ToList();

            if (!validMoves.Any()) break;

            var move = validMoves[_random.Next(validMoves.Count)];

            SimulateMove(sim, phase, move, lastAttackCard);

            if (turns % 5 == 0)
            {
                sim.History.Add(new HistoryEntry(sim));
                if (sim.History.Count > 10)
                    sim.History.RemoveAt(0);
            }

            if (phase == Phase.BotAttack || phase == Phase.PlayerAttack)
            {
                lastAttackCard = move;
            }
            else if (phase == Phase.BotCounter || phase == Phase.PlayerCounter)
            {
                lastAttackCard = null;
            }

            phase = NextPhase(phase);
        }

        return EvaluateGameStateMCTS(sim);
    }

    private List<SimCard> SampleFullOpponentHand(GameState state, Phase phase, List<SimCard> possibleDeck)
    {
        if (!GameSettings.UseHiddenDecks)
        {
            return state.playerHand.Select(c => c.Clone()).ToList();
        }

        var mana = state.playerMana;
        int maxHandSize = 5;

        var validTypes = new List<TypeOfCard> { TypeOfCard.Attack, TypeOfCard.Special };
        if (GameSettings.UseBuffDebuffCards)
        {
            validTypes.Add(TypeOfCard.Buff);
            validTypes.Add(TypeOfCard.Debuff);
        }
        validTypes.Add(TypeOfCard.Defense);

        var filtered = possibleDeck
            .Where(c => validTypes.Contains(c.TypeOfCard) && (!GameSettings.UseManaSystem || c.ManaCost <= mana))
            .ToList();

        if (!filtered.Any()) return new List<SimCard>();

        var typeFrequencies = filtered
            .GroupBy(c => c.TypeOfCard)
            .ToDictionary(g => g.Key, g => g.Count());

        int total = typeFrequencies.Values.Sum();

        var sampledHand = new List<SimCard>();
        for (int i = 0; i < Math.Min(maxHandSize, filtered.Count); i++)
        {
            double rand = _random.NextDouble() * total;
            double cumulative = 0;
            TypeOfCard selectedType = TypeOfCard.Attack;

            foreach (var kvp in typeFrequencies)
            {
                cumulative += kvp.Value;
                if (rand <= cumulative)
                {
                    selectedType = kvp.Key;
                    break;
                }
            }

            var candidates = filtered.Where(c => c.TypeOfCard == selectedType).ToList();
            var card = candidates[_random.Next(candidates.Count)];
            sampledHand.Add(card.Clone());
        }

        return sampledHand;
    }

    private static SimCard SampleOpponentCardForPhase(GameState state, Phase phase, List<SimCard> possibleDeck)
    {
        var mana = state.playerMana;

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

        var filtered = possibleDeck
            .Where(c => validTypes.Contains(c.TypeOfCard) && (!GameSettings.UseManaSystem || c.ManaCost <= mana))
            .ToList();

        if (!filtered.Any()) return null;

        var sampled = filtered[_random.Next(filtered.Count)];
        return new SimCard(sampled);
    }

    private void MCTSBackpropagate(MCTSNode node, double result)
    {
        while (node != null)
        {
            node.Visits++;
            node.Wins += result;
            node = node.Parent;
        }
    }

    private MCTSNode MCTSUCTSelect(MCTSNode node)
    {
        return node.Children.OrderByDescending(child => MCTSUCTValue(child, node.Visits)).First();
    }

    private double MCTSUCTValue(MCTSNode node, int totalVisits)
    {
        if (node.Visits == 0) return double.MaxValue;
        return (node.Wins / node.Visits) + Math.Sqrt(2 * Math.Log(totalVisits) / node.Visits);
    }

    private void SimulateMove(GameState state, Phase phase, SimCard card, SimCard lastAttackCard)
    {
        bool isBot = phase == Phase.BotAttack || phase == Phase.BotCounter;

        if (isBot)
        {
            if (GameSettings.UseManaSystem)
            {
                state.botMana -= card.ManaCost;
            }
            state.botHand.RemoveAll(c => c.Equals(card));
        }
        else
        {
            if (GameSettings.UseManaSystem)
            {
                state.playerMana -= card.ManaCost;
            }
            state.playerHand.RemoveAll(c => c.Equals(card));
        }

        switch (card.TypeOfCard)
        {
            case TypeOfCard.Attack:
                if (isBot)
                    state.last_player_damage = card.Damage;
                else
                    state.last_bot_damage = card.Damage;
                break;

            case TypeOfCard.Defense:
                int incoming = lastAttackCard?.Damage ?? 0;
                int defense = isBot ? state.GetEffectiveBotDefense(card.Defense) : state.GetEffectivePlayerDefense(card.Defense);
                int damage = CombatUtils.CalculateNetDamage(incoming, defense);

                if (isBot)
                {
                    state.botHealth -= damage;
                    state.last_player_damage = 0;
                }
                else
                {
                    state.playerHealth -= damage;
                    state.last_bot_damage = 0;
                }
                break;

            case TypeOfCard.Special:
                var match = Regex.Match(card.Name, @"(HP|MP|GP)(\d+)");
                if (!match.Success) return;

                int value = int.Parse(match.Groups[2].Value);
                string type = match.Groups[1].Value;

                if (isBot)
                {
                    if (type == "HP") state.botHealth += value;
                    else if (type == "MP") state.botMana += value;
                    else if (type == "GP") state.botMoney += value;
                }
                else
                {
                    if (type == "HP") state.playerHealth += value;
                    else if (type == "MP") state.playerMana += value;
                    else if (type == "GP") state.playerMoney += value;
                }
                break;

            case TypeOfCard.Buff:
            case TypeOfCard.Debuff:
                state.ApplyBuffCard(card, isBot);
                break;
        }
    }

    private double EvaluateGameStateMCTS(GameState state)
    {
        double score = 0;

        score += (state.botHealth - state.playerHealth) * 1.0;
        score += (state.botMana - state.playerMana) * 0.2;

        score += (state.botHand.Count - state.playerHand.Count) * 0.3;

        if (GameSettings.UseBuffDebuffCards)
        {
            score += state.botBuffs.Sum(b => b.IsDebuff ? -b.Value : b.Value) * 0.5;
            score -= state.playerBuffs.Sum(b => b.IsDebuff ? -b.Value : b.Value) * 0.5;
        }

        if (state.History.Count > 1)
        {
            var first = state.History[0];
            var last = state.History[^1];
            score += (last.BotHealth - first.BotHealth) * 0.3;
            score += (last.BotMana - first.BotMana) * 0.2;
            score += (last.BotCards - first.BotCards) * 0.1;
        }

        return score;
    }

    private bool IsGameOver(int botHP, int playerHP)
    {
        return botHP <= 0 || playerHP <= 0;
    }

    private Phase NextPhase(Phase current)
    {
        return current switch
        {
            Phase.PlayerAttack => Phase.BotCounter,
            Phase.BotCounter => Phase.BotAttack,
            Phase.BotAttack => Phase.PlayerCounter,
            Phase.PlayerCounter => Phase.PlayerAttack,
            _ => throw new Exception("Unknown Phase")
        };
    }

    private int CountCardsOfType(SimUser user, TypeOfCard type)
    {
        return user.UserDeck.Count(c => c.TypeOfCard == type);
    }

    public class MCTSNode
    {
        public GameState State;
        public SimCard Move;
        public double Wins;
        public int Visits;
        public List<MCTSNode> Children = new List<MCTSNode>();
        public MCTSNode Parent;
        public Phase Phase;

        public MCTSNode(GameState state, SimCard move, Phase phase, MCTSNode parent = null)
        {
            State = state;
            Move = move;
            Phase = phase;
            Parent = parent;
        }

        public List<SimCard> GetValidMoves()
        {
            bool isBot = Phase == Phase.BotAttack || Phase == Phase.BotCounter;
            bool isCountering = Phase == Phase.BotCounter || Phase == Phase.PlayerCounter;

            var hand = isBot ? State.botHand : State.playerHand;
            int mana = isBot ? State.botMana : State.playerMana;

            return hand.Where(card => IsValidMove(card, Phase) && (!GameSettings.UseManaSystem || card.ManaCost <= mana)).ToList();
        }

        public static bool IsValidMove(SimCard card, Phase phase)
        {
            bool isCounter = phase == Phase.BotCounter || phase == Phase.PlayerCounter;
            return card.ManaCost >= 0 &&
                   (isCounter ? card.TypeOfCard == TypeOfCard.Defense :
                                card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special || card.TypeOfCard == TypeOfCard.Buff || card.TypeOfCard == TypeOfCard.Debuff);
        }
    }
}
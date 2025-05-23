using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

//MCTS
public class MCTS
{
    private const int simulationCount = 1500;
    private Random _random = new Random();

    public SimCard MCTSFindBestMove(GameState rootState, bool isBotCountering, bool botTurn)
    {
        var rootNode = new MCTSNode(rootState);

        for (int i = 0; i < simulationCount; i++)
        {
            var selectedNode = MCTSSelectNode(rootNode);
            var expandedNode = MCTSExpandNode(selectedNode, isBotCountering, botTurn);
            var result = MCTSSimulate(expandedNode, isBotCountering, botTurn);
            MCTSBackpropagate(expandedNode, result);
        }

        return rootNode.Children.OrderByDescending(child => child.Visits).FirstOrDefault()?.Move;
    }

    // Selection Phase - Blattknoten suchen/bewerten
    private MCTSNode MCTSSelectNode(MCTSNode node)
    {
        while (node.Children.Any())
        {
            node = MCTSUCTSelect(node);
        }
        return node;
    }

    // Expansion Phase - Mögliche Moves und Child Knoten werden geadded
    private MCTSNode MCTSExpandNode(MCTSNode node, bool isCountering, bool botTurn)
    {
        var validMoves = node.GetValidMoves(botTurn, isCountering);
        foreach (var move in validMoves)
        {
            var clonedState = node.State.Clone();
            SimulateMove(clonedState, botTurn, move, isCountering, null);
            clonedState.History.Add(clonedState.DeepCopy()); // Log full snapshot
            node.Children.Add(new MCTSNode(clonedState, move, node));
        }

        return node.Children.Any() ? node.Children[_random.Next(node.Children.Count)] : node;
    }

    private double MCTSSimulate(MCTSNode node, bool isBotCountering, bool botTurn)
    {
        GameState sim = node.State.Clone();
        bool isBotTurn = botTurn;
        SimCard lastCard = node.Move;
        int turns = 0;

        while (!IsGameOver(sim.botHealth, sim.playerHealth) && turns++ < 200)
        {
            sim.AdvanceTurn();

            var currentHand = isBotTurn ? sim.botHand : sim.playerHand;
            var currentMana = isBotTurn ? sim.botMana : sim.playerMana;
            var validMoves = currentHand.Where(card => MCTSNode.IsValidMove(card, isBotTurn, isBotCountering) && card.ManaCost <= currentMana).ToList();

            if (!validMoves.Any()) break;

            var move = validMoves[_random.Next(validMoves.Count)];
            SimulateMove(sim, isBotTurn, move, isBotCountering, lastCard);

            sim.History.Add(sim.DeepCopy());

            lastCard = move;
            isBotTurn = !isBotTurn;
            isBotCountering = !isBotCountering;
        }

        return EvaluateGameStateMCTS(sim);
    }

    // Backpropagation Phase - Update die Knoten
    private void MCTSBackpropagate(MCTSNode node, double result)
    {
        while (node != null)
        {
            node.Visits++;
            node.Wins += result;
            node = node.Parent;
        }
    }

    // UCT
    private MCTSNode MCTSUCTSelect(MCTSNode node)
    {
        return node.Children.OrderByDescending(child => MCTSUCTValue(child, node.Visits)).First();
    }

    // UCT value für Knoten
    private double MCTSUCTValue(MCTSNode node, int totalVisits)
    {
        if (node.Visits == 0)
        {
            return double.MaxValue;
        }
        return (node.Wins / node.Visits) + Math.Sqrt(2 * Math.Log(totalVisits) / node.Visits);
    }

    private void SimulateMove(GameState state, bool isBotTurn, SimCard card, bool isCountering, SimCard opponentCard)
    {
        if (isBotTurn)
        {
            state.botMana -= card.ManaCost;
            state.botHand.Remove(card);
        }
        else
        {
            state.playerMana -= card.ManaCost;
            state.playerHand.Remove(card);
        }

        switch (card.TypeOfCard)
        {
            case TypeOfCard.Attack:
                if (isBotTurn)
                    state.last_player_damage = card.Damage;
                else
                    state.last_bot_damage = card.Damage;
                break;

            case TypeOfCard.Defense:
                int incoming = isBotTurn ? state.last_bot_damage : state.last_player_damage;
                int defense = isBotTurn
                    ? state.GetEffectiveBotDefense(card.Defense)
                    : state.GetEffectivePlayerDefense(card.Defense);

                int damage = CombatUtils.CalculateNetDamage(incoming, defense);
                if (isBotTurn)
                    state.botHealth -= damage;
                else
                    state.playerHealth -= damage;

                state.last_bot_damage = 0;
                state.last_player_damage = 0;
                break;

            case TypeOfCard.Special:
                var match = Regex.Match(card.Name, @"(HP|MP|GP)(\d+)");
                if (!match.Success) return;

                int value = int.Parse(match.Groups[2].Value);
                switch (match.Groups[1].Value)
                {
                    case "HP":
                        if (isBotTurn) state.botHealth += value;
                        else state.playerHealth += value;
                        break;
                    case "MP":
                        if (isBotTurn) state.botMana += value;
                        else state.playerMana += value;
                        break;
                    case "GP":
                        if (isBotTurn) state.botMoney += value;
                        else state.playerMoney += value;
                        break;
                }
                break;

            case TypeOfCard.Buff:
            case TypeOfCard.Debuff:
                state.ApplyBuffCard(card, isBotTurn);
                break;
        }
    }

    private bool IsGameOver(int hp1, int hp2)
    {
        return hp1 <= 0 || hp2 <= 0;
    }

    private double EvaluateGameStateMCTS(GameState state)
    {
        double score = 0;
        score += (state.botHealth - state.playerHealth);
        score += (state.botHand.Count - state.playerHand.Count) * 0.5;
        score += (state.botMana - state.playerMana) * 0.2;

        score += state.botBuffs.Sum(b => b.IsDebuff ? -b.Value : b.Value);
        score -= state.playerBuffs.Sum(b => b.IsDebuff ? -b.Value : b.Value);

        // Use historical trend
        if (state.History.Count > 1)
        {
            var first = state.History[0];
            score += (state.botHealth - first.botHealth) * 0.3;
            score += (state.botMana - first.botMana) * 0.2;
        }

        return score;
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

        public MCTSNode(GameState state, SimCard move = null, MCTSNode parent = null)
        {
            State = state;
            Move = move;
            Parent = parent;
        }

        public List<SimCard> GetValidMoves(bool isBotTurn, bool isCountering)
        {
            var hand = isBotTurn ? State.botHand : State.playerHand;
            int mana = isBotTurn ? State.botMana : State.playerMana;

            return hand.Where(card => IsValidMove(card, isBotTurn, isCountering) && card.ManaCost <= mana).ToList();
        }

        public static bool IsValidMove(SimCard card, bool isBotTurn, bool isCountering)
        {
            return card.ManaCost >= 0 &&
                   (isCountering ? card.TypeOfCard == TypeOfCard.Defense :
                                   card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special || card.TypeOfCard == TypeOfCard.Buff || card.TypeOfCard == TypeOfCard.Debuff);
        }
    }
}


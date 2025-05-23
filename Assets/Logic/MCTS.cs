using System;
using System.Collections.Generic;
using System.Linq;

//MCTS

public class MCTS
{
    private const int simulationCount = 2500;
    private Random _random = new Random();

    public SimCard MCTSFindBestMove(SimUser bot, SimUser player, SimCard placedCard, bool isBotCountering, bool botTurn)
    {
        var rootNode = new MCTSNode(bot, player, placedCard);

        for (int i = 0; i < simulationCount; i++)
        {
            var selectedNode = MCTSSelectNode(rootNode);
            var expandedNode = MCTSExpandNode(selectedNode, isBotCountering, botTurn);
            var result = MCTSSimulate(expandedNode, isBotCountering, botTurn, placedCard);
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
        var currentPlayer = botTurn ? node.Bot : node.Player;
        var validMoves = node.GetValidMoves(currentPlayer, botTurn, isCountering);

        foreach (var move in validMoves)
        {
            var child = new MCTSNode(node.Bot.Clone(), node.Player.Clone(), move, node);
            node.Children.Add(child);
        }

        return node.Children.Any() ? node.Children[_random.Next(node.Children.Count)] : node;
    }

    private double MCTSSimulate(MCTSNode node, bool isBotCountering, bool botTurn, SimCard opponentCard)
    {
        var simBot = node.Bot.Clone();
        var simPlayer = node.Player.Clone();
        bool isBotTurn = botTurn;
        SimCard damageCard = opponentCard;
        int turns = 0;

        while (!IsGameOver(simBot.HealthPoints, simPlayer.HealthPoints) && turns++ < 200)
        {
            var currentUser = isBotTurn ? simBot : simPlayer;
            var validMoves = node.GetValidMoves(currentUser, isBotTurn, isBotCountering);
            if (!validMoves.Any()) break;

            var move = validMoves[_random.Next(validMoves.Count)];
            if (currentUser.ManaPoints < move.ManaCost) continue;

            if (isBotTurn)
            {
                SimulateMove(simBot, simPlayer, move, isBotCountering, damageCard);
                isBotCountering = !isBotCountering;
                isBotTurn = !isBotTurn;
            }
            else
            {
                SimulateMove(simPlayer, simBot, move, isBotCountering, damageCard);
                isBotCountering = !isBotCountering;
                isBotTurn = !isBotTurn;
            }

            damageCard = move;
        }

        return EvaluateGameStateMCTS(simBot, simPlayer);
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

    private void SimulateMove(SimUser actor, SimUser opponent, SimCard card, bool isCountering, SimCard opponentCard)
    {
        if (actor.ManaPoints < card.ManaCost) return;
        actor.ManaPoints -= card.ManaCost;

        switch (card.TypeOfCard)
        {
            case TypeOfCard.Attack:
                opponent.HealthPoints -= card.Damage;
                break;
            case TypeOfCard.Defense:
                if (isCountering && opponentCard != null && opponentCard.TypeOfCard == TypeOfCard.Attack)
                {
                    actor.HealthPoints -= Math.Max(0, opponentCard.Damage - card.Defense);
                }
                break;
            case TypeOfCard.Special:
                if (card.Name.StartsWith("HP")) actor.HealthPoints += int.Parse(card.Name.Substring(2));
                if (card.Name.StartsWith("MP")) actor.ManaPoints += int.Parse(card.Name.Substring(2));
                if (card.Name.StartsWith("GP")) actor.Money += int.Parse(card.Name.Substring(2));
                break;
        }

        actor.UserDeck.Remove(card);
    }

    private bool IsGameOver(int hp1, int hp2)
    {
        return hp1 <= 0 || hp2 <= 0;
    }

    private double EvaluateGameStateMCTS(SimUser player1, SimUser player2)
    {
        double score = 0.0;
        score += (player1.HealthPoints - player2.HealthPoints);
        score += (player1.UserDeck.Count - player2.UserDeck.Count) * 0.5;
        score += (CountCardsOfType(player1, TypeOfCard.Special) - CountCardsOfType(player2, TypeOfCard.Special)) * 0.75;
        score += (CountCardsOfType(player1, TypeOfCard.Defense) - CountCardsOfType(player2, TypeOfCard.Defense)) * 0.3;
        return score;
    }

    private int CountCardsOfType(SimUser user, TypeOfCard type)
    {
        return user.UserDeck.Count(c => c.TypeOfCard == type);
    }

    public static bool IsValidMove(SimCard card, bool isBotTurn, bool isCountering)
    {
        return card.ManaCost >= 0 &&
               (isCountering ? card.TypeOfCard == TypeOfCard.Defense :
                               card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special);
    }


    public class MCTSNode
    {
        public SimUser Bot;
        public SimUser Player;
        public SimCard Move;
        public double Wins;
        public int Visits;
        public List<MCTSNode> Children = new List<MCTSNode>();
        public MCTSNode Parent;

        public MCTSNode(SimUser bot, SimUser player, SimCard move = null, MCTSNode parent = null)
        {
            Bot = bot;
            Player = player;
            Move = move;
            Parent = parent;
        }

        public List<SimCard> GetValidMoves(SimUser user, bool isBotTurn, bool isCountering)
        {
            return user.UserDeck
                .Where(card => MCTS.IsValidMove(card, isBotTurn, isCountering) && card.ManaCost <= user.ManaPoints)
                .ToList();
        }
    }
}

using System.Collections;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Networking;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine.Windows;
using UnityEngine.SceneManagement;
using Unity.Mathematics;
using static UnityEditor.Experimental.GraphView.GraphView;
using Random = System.Random;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using static Game;
using UnityEngine.Rendering;

//MCTS

public class MCTS
{
    private const int simulationCount = 1000; // statisch bearbeitet
    private Random _random = new Random();

    public Card MCTSFindBestMove(User bot, User player, Card placedCard, bool isBotCountering, bool botTurn)
    {
        var rootNode = new MCTSNode(bot, player, placedCard);

        for (int i = 0; i < simulationCount; i++)
        {
            Log($"iteration {i}");
            var selectedNode = MCTSSelectNode(rootNode);
            var expandedNode = MCTSExpandNode(selectedNode, isBotCountering, botTurn);
            var result = MCTSSimulate(expandedNode, isBotCountering, botTurn, placedCard);
            MCTSBackpropagate(expandedNode, result);
        }

        return rootNode.Children.OrderByDescending(child => child.Visits).FirstOrDefault()?.Move; // höhsten iterationen sind favorable
    }
    private void Log(string message)
    {
        Debug.Log($"[LOG] {DateTime.Now:HH:mm:ss} - {message}");
    }

    // Selection Phase - Blattknoten suchen/bewerten
    private MCTSNode MCTSSelectNode(MCTSNode node)
    {
        Log($"Selecting node with the {node.Children.Count} children...");
        while (node.Children.Any())
        {
            node = MCTSUCTSelect(node);
        }
        Log("Selection phase complete.");
        return node;
    }

    // Expansion Phase - Mögliche Moves und Child Knoten werden geadded
    private MCTSNode MCTSExpandNode(MCTSNode node, bool isCountering, bool botTurn)
    {
        var currentPlayer = botTurn ? node.Bot : node.Player;

        List<Card> validMoves;

        if (botTurn)
        {
            if (isCountering)
            {
                validMoves = node.GetValidMoves(node.Bot, botTurn, isCountering);
            }
            else
            {
                validMoves = node.GetValidMoves(node.Bot, botTurn, isCountering);
            }
        }
        else
        {
            if (isCountering)
            {
                validMoves = node.GetValidMoves(node.Player, botTurn, isCountering);
            }
            else
            {

                validMoves = node.GetValidMoves(node.Player, botTurn, isCountering);
            }
        }

        // Child Knoten
        foreach (var move in validMoves)
        {
            var child = new MCTSNode(CopyUser(node.Bot), CopyUser(node.Player), move, node);
            node.Children.Add(child);
        }

        return node.Children.Any() ? node.Children[_random.Next(node.Children.Count)] : node;
    }

    private double MCTSSimulate(MCTSNode node, bool isBotCountering, bool botTurn, Card opponentCard)
    {
        var simBot = CopyUser(node.Bot);
        var simPlayer = CopyUser(node.Player);
        bool isBotTurn = botTurn;
        Card damageCard = opponentCard;

        Random rng = new Random();

        while (!IsGameOver(simBot.HealthPoints, simPlayer.HealthPoints))
        {
            var currentUser = isBotTurn ? simBot : simPlayer;
            var validMoves = node.GetValidMoves(currentUser, isBotTurn, isBotCountering);

            if (!validMoves.Any()) break;

            // höhste UCT oder random
            var move = rng.NextDouble() < 0.7
                        ? validMoves.OrderByDescending(mv =>
                        {
                            var childNode = node.Children.FirstOrDefault(n => n.Move == mv);
                            return childNode != null ? MCTSUCTValue(childNode, node.Visits) : double.MinValue;
                        }).First()
                        : validMoves[rng.Next(validMoves.Count)];

            if (isBotTurn)
            {
                if (isBotCountering)
                {
                    SimulateMove(simBot, simPlayer, move, isBotTurn, damageCard); // Bot counters player's attack
                    isBotCountering = false;
                    isBotTurn = true;
                }
                else
                {
                    SimulateMove(simBot, simPlayer, move, isBotTurn, null); // Bot attacks
                    isBotCountering = true;
                    isBotTurn = false;
                }
            }
            else
            {
                if (isBotCountering)
                {
                    SimulateMove(simPlayer, simBot, move, isBotTurn, damageCard); // Player counters bot's attack
                    isBotCountering = false;
                    isBotTurn = false;
                }
                else
                {
                    SimulateMove(simPlayer, simBot, move, isBotTurn, null); // Player attacks
                    isBotCountering = true;
                    isBotTurn = true;
                }
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

    private User CopyUser(User user)
    {
        if (user is Bot bot)
        {
            return CopyBot(bot);
        }
        else if (user is Player player)
        {
            return CopyPlayer(player);
        }
        else
        {
            var gameObject = new GameObject("UserCopy");
            var newUser = gameObject.AddComponent<User>();
            newUser.Name = user.Name;
            newUser.Rating = user.Rating;
            newUser.HealthPoints = user.HealthPoints;
            newUser.ManaPoints = user.ManaPoints;
            newUser.Money = user.Money;
            newUser.UserDeck = user.UserDeck.ToList();
            return newUser;
        }
    }

    private Bot CopyBot(Bot bot)
    {
        var gameObject = new GameObject("BotCopy");
        var newBot = gameObject.AddComponent<Bot>();
        newBot.Name = bot.Name;
        newBot.Rating = bot.Rating;
        newBot.HealthPoints = bot.HealthPoints;
        newBot.ManaPoints = bot.ManaPoints;
        newBot.Money = bot.Money;
        newBot.UserDeck = bot.UserDeck.ToList();
        return newBot;
    }

    private Player CopyPlayer(Player player)
    {
        var gameObject = new GameObject("PlayerCopy");
        var newPlayer = gameObject.AddComponent<Player>();
        newPlayer.Name = player.Name;
        newPlayer.Rating = player.Rating;
        newPlayer.HealthPoints = player.HealthPoints;
        newPlayer.ManaPoints = player.ManaPoints;
        newPlayer.Money = player.Money;
        newPlayer.UserDeck = player.UserDeck.ToList();
        return newPlayer;
    }


    private void SimulateMove(User bot, User player, Card card, bool isBotTurn, Card opponentCard)
    {
        User actingUser = isBotTurn ? bot : player;
        User targetUser = isBotTurn ? player : bot;

        switch (card.TypeOfCard)
        {
            case TypeOfCard.Attack:
                targetUser.HealthPoints -= card.Damage;
                break;

            case TypeOfCard.Defense:
                if (opponentCard != null && opponentCard.TypeOfCard == TypeOfCard.Attack)
                {
                    actingUser.HealthPoints -= Math.Max(0, opponentCard.Damage - card.Defense);
                }
                break;

            case TypeOfCard.Special:
                if (card.Name.StartsWith("HP"))
                {
                    var healed = int.Parse(card.Name.Substring(2));
                    actingUser.HealthPoints += healed;
                }
                break;
        }

        actingUser.UserDeck.Remove(card);

        Debug.Log($"{(isBotTurn ? "Bot" : "Player")} used {card.TypeOfCard} card: {card.Name}");
    }



    private bool IsGameOver(int botHP, int playerHP)
    {
        return botHP <= 0 || playerHP <= 0;
    }

    private double EvaluateGameStateMCTS(User bot, User player)
    {
        double score = 0.0;
        score += (bot.HealthPoints - player.HealthPoints) * 1.0;
        score += (bot.UserDeck.Count - player.UserDeck.Count) * 0.5;
        score += (CountCardsOfTypeMCTS(bot, TypeOfCard.Special) - CountCardsOfTypeMCTS(player, TypeOfCard.Special)) * 0.75;
        score += (CountCardsOfTypeMCTS(bot, TypeOfCard.Defense) - CountCardsOfTypeMCTS(player, TypeOfCard.Defense)) * 0.3;
        return score;
    }

    private int CountCardsOfTypeMCTS(User user, TypeOfCard type)
    {
        return user.UserDeck.Count(card => card.TypeOfCard == type);
    }

    public static bool IsValidMove(Card card, bool isBotTurn, bool isBotCountering)
    {
        if (isBotCountering)
        {
            return card.TypeOfCard == TypeOfCard.Defense;
        }
        else if (isBotTurn && isBotCountering == false)
        {
            return card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special;
        }
        else
        {
            return card.TypeOfCard == TypeOfCard.Attack || card.TypeOfCard == TypeOfCard.Special;
        }
    }

    public class MCTSNode
    {
        public User Bot { get; set; }
        public User Player { get; set; }
        public Card Move { get; set; }
        public double Wins { get; set; }
        public int Visits { get; set; }
        public List<MCTSNode> Children { get; set; }
        public MCTSNode Parent { get; set; }

        public MCTSNode(User bot, User player, Card move = null, MCTSNode parent = null)
        {
            Bot = bot;
            Player = player;
            Move = move;
            Wins = 0;
            Visits = 0;
            Children = new List<MCTSNode>();
            Parent = parent;
        }

        // Get valid moves for a given user
        public List<Card> GetValidMoves(User user, bool isBotTurn, bool isBotCountering)
        {
            return user.UserDeck.Where(card => MCTS.IsValidMove(card, isBotTurn, isBotCountering)).ToList();
        }
    }
}

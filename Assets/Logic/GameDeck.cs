using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDeck : MonoBehaviour
{
    private static int _deckNumber;
    private List<Card> _deck;

    public GameDeck(List<Card> deck)
    {
        _deckNumber++;
        _deck = deck;
    }

    public List<Card> Deck { get => _deck; }

    public override string ToString()
    {
        string result = String.Empty;

        foreach (Card card in _deck)
        {
            result += card.ToString();
            result += "\n";
        }
        return result;
    }

    public Card DrawCard()
    {
        Card drawnCard = _deck.First();
        _deck.RemoveAt(0);

        return drawnCard;
    }

    public void Shuffle()
    {
        System.Random rdm = new System.Random();
        int deckCount = _deck.Count;
        while (deckCount > 1)
        {
            deckCount--;
            int randomInt = rdm.Next(deckCount + 1);
            Card card = _deck[randomInt];
            _deck[randomInt] = _deck[deckCount];
            _deck[deckCount] = card;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameDeck : MonoBehaviour
{
    public static int _deckNumber;
    public List<Card> _deck;

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
        if (_deck.Count == 0)
            throw new InvalidOperationException("Cannot draw from an empty deck.");

        Card drawnCard = _deck.First();
        _deck.RemoveAt(0);
        return drawnCard;
    }

    public void HandleDrawCardButton()
    {
        Card drawnCard = DrawCard();
        //Do Something with the card here (Assign to player deck etc)
        Debug.Log(drawnCard);
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

    public void FilterBuffDebuffCards()
    {
        if (PlayerPrefs.GetInt("ToggleBuffDebuffState", 1) == 0)
        {
            int beforeCount = _deck.Count;
            _deck = _deck.Where(c => !c.name.StartsWith("Extra")).ToList();
            int afterCount = _deck.Count;
            Debug.Log($"[Deck Filter] Removed {beforeCount - afterCount} Buff/Debuff cards.");
        }
    }
}

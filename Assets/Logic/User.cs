using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User : MonoBehaviour
{
    public string _name;
    public int _rating;
    public int _healthPoints;
    public int _manaPoints;
    public int _money;
    public List<Card> _userDeck;

    public string Name { get => _name; set => _name = value; }
    public int Rating { get => _rating; set => _rating = value; }
    public int HealthPoints { get => _healthPoints; set => _healthPoints = value; }
    public int ManaPoints { get => _manaPoints; set => _manaPoints = value; }
    public int Money { get => _money; set => _money = value; }
    public List<Card> UserDeck { get => _userDeck; set => _userDeck = value; }

    public User(string name, int rating, int healthPoints, int manaPoints, int money)
    {
        _name = name;
        _rating = rating;
        _healthPoints = healthPoints;
        _manaPoints = manaPoints;
        _money = money;
        _userDeck = new List<Card>();
    }

    public override string ToString()
    {
        return "Name: " + Name + "\nRating: " + Rating + "\nHealth Points: " + HealthPoints + "\nMana Points: " + ManaPoints + "\nMoney: " + Money;
    }

}
//ToDo: fragen wegen userinfo, was wollen wir da exactly?


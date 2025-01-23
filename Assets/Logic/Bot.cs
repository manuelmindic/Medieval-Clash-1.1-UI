using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bot : User
{
    private Difficulty _difficulty { get; set; }

    public Bot(string name, int rating, int healthPoints, int manaPoints, int money, Difficulty difficulty) : base(name, rating, healthPoints, manaPoints, money)
    {
        _difficulty = difficulty;
    }
}

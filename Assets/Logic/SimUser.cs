using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimUser
{
    public string Name;
    public int Rating;
    public int HealthPoints;
    public int ManaPoints;
    public int Money;
    public List<SimCard> UserDeck;
    public List<Buff> ActiveBuffs;

    public SimUser Clone()
    {
        return new SimUser
        {
            Name = this.Name,
            Rating = this.Rating,
            HealthPoints = this.HealthPoints,
            ManaPoints = this.ManaPoints,
            Money = this.Money,
            UserDeck = this.UserDeck.Select(c => c.Clone()).ToList(),
            ActiveBuffs = this.ActiveBuffs.Select(b => b.Clone()).ToList()
        };
    }
}


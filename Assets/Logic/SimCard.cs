using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimCard
{
    public string ImageFileName;
    public string Name;
    public int Price;
    public TypeOfCard TypeOfCard;
    public int Damage;
    public int Defense;
    public int ManaCost;
    public int Duration;
    public int EffectValue;

    public SimCard(Card card)
    {
        ImageFileName = card.ImageFileName;
        Name = card.Name;
        Price = card.Price;
        TypeOfCard = card.TypeOfCard;
        Damage = card.Damage;
        Defense = card.Defense;
        ManaCost = card.ManaCost;
        Duration = card.Duration;
        EffectValue = card.EffectValue;
    }

    public SimCard(SimCard simCard)
    {
        ImageFileName = simCard.ImageFileName;
        Name = simCard.Name;
        Price = simCard.Price;
        TypeOfCard = simCard.TypeOfCard;
        Damage = simCard.Damage;
        Defense = simCard.Defense;
        ManaCost = simCard.ManaCost;
        Duration = simCard.Duration;
        EffectValue = simCard.EffectValue;
    }

    public override bool Equals(object obj)
    {
        if (obj is not SimCard other) return false;
        return Name == other.Name &&
               TypeOfCard == other.TypeOfCard &&
               ManaCost == other.ManaCost &&
               Damage == other.Damage &&
               Defense == other.Defense;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, TypeOfCard, ManaCost, Damage, Defense);
    }

    public SimCard Clone()
    {
        return (SimCard)MemberwiseClone();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimCard
{
    public string Name;
    public TypeOfCard TypeOfCard;
    public int Damage;
    public int Defense;
    public int ManaCost;
    public int Duration;

    public SimCard Clone()
    {
        return (SimCard)MemberwiseClone();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Buff
{
    public BuffType Type;
    public int Value;
    public int Duration;

    public Buff(BuffType type, int value, int duration)
    {
        Type = type;
        Value = value;
        Duration = duration;
    }

    public void Tick() => Duration--;
    public bool IsExpired => Duration <= 0;
}

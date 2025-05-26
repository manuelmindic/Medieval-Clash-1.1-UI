using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Buff
{
    public BuffType Type;
    public int Value;
    public int Duration;
    public bool IsDebuff;

    public Buff(BuffType type, int value, int duration, bool isDebuff)
    {
        Type = type;
        Value = value;
        Duration = duration;
        IsDebuff = isDebuff;
    }

    public void Tick() => Duration--;
    public bool IsExpired => Duration <= 0;

    public Buff Clone()
    {
        return new Buff(Type, Value, Duration, IsDebuff);
    }
}

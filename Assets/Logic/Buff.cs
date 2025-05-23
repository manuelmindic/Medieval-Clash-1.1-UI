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

    public void Tick() => Duration--;
    public bool IsExpired => Duration <= 0;

    public Buff Clone()
    {
        return new Buff { Type = this.Type, Value = this.Value, Duration = this.Duration, IsDebuff = this.IsDebuff };
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CombatUtils
{
    public static void ApplyStartOfTurnBuffs(List<Buff> buffs, ref int hp)
    {
        List<Buff> expired = new List<Buff>();
        foreach (var buff in buffs)
        {
            switch (buff.Type)
            {
                case BuffType.DamageOverTime:
                    hp -= buff.Value;
                    Debug.LogWarning($"[Buff Tick] DOT -{buff.Value} HP");
                    break;
                case BuffType.HealOverTime:
                    hp += buff.Value;
                    Debug.LogWarning($"[Buff Tick] HOT +{buff.Value} HP");
                    break;
            }
            buff.Tick();
            if (buff.IsExpired) expired.Add(buff);
        }
        foreach (var buff in expired)
        {
            buffs.Remove(buff);
            Debug.LogWarning($"[Buff Expired] {buff.Type}");
        }
    }

    public static int GetEffectiveAttack(int baseAttack, List<Buff> buffs)
    {
        int total = 0;
        foreach (var buff in buffs.Where(b => b.Type == BuffType.AttackBoost))
        {
            total += buff.IsDebuff ? -buff.Value : buff.Value;
        }
        return baseAttack + total;
    }

    public static int GetEffectiveDefense(int baseDefense, List<Buff> buffs)
    {
        int total = 0;
        foreach (var buff in buffs.Where(b => b.Type == BuffType.DefenseBoost))
        {
            total += buff.IsDebuff ? -buff.Value : buff.Value;
        }
        return baseDefense + total;
    }

    public static int CalculateNetDamage(int attack, int defense)
    {
        return Mathf.Max(0, attack - defense);
    }
}

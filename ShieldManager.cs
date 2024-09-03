using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShieldSource
{
    Bubble,
    ShoddyShield,
    Buff,
    Potion
}

public class ShieldManager 
{
    int _maxHP;
    public int Sum {  get { return GetShieldSum(); } }
    ShieldSource[] _allSources = new ShieldSource[4] { ShieldSource.Bubble, ShieldSource.ShoddyShield, ShieldSource.Buff, ShieldSource.Potion};
    Dictionary<ShieldSource,int> _shieldLevels= new Dictionary<ShieldSource,int>();

    public void Initialize(int maxHP)
    {
        _maxHP = maxHP;
        ClearAllShields();
    }

    public void ClearAllShields()
    {
        foreach (ShieldSource source in _allSources) _shieldLevels[source] = 0;
    }

    public void AddShield(ShieldSource type, int amount)
    {
        int currentShieldSum = Sum;
        int newShieldSum = currentShieldSum + amount;

        if (newShieldSum > _maxHP)
        {
            amount = _maxHP - currentShieldSum;
        }

        _shieldLevels[type] += amount;
    }

    public int ReceiveDamage(int damage)
    {
        // Sort the shield levels by their values (ascending order)
        List<KeyValuePair<ShieldSource, int>> sortedShields = new List<KeyValuePair<ShieldSource, int>>(_shieldLevels);
        sortedShields.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));

        int remainingDamage = damage;

        // Iterate through the sorted shields and reduce the damage
        foreach (KeyValuePair<ShieldSource, int> shield in sortedShields)
        {
            if (remainingDamage <= 0)
                break;

            if (shield.Value > 0)
            {
                int shieldValue = _shieldLevels[shield.Key];

                if (shieldValue >= remainingDamage)
                {
                    _shieldLevels[shield.Key] -= remainingDamage;
                    remainingDamage = 0;
                }
                else
                {
                    remainingDamage -= shieldValue;
                    _shieldLevels[shield.Key] = 0;
                }
            }
        }

        return remainingDamage; // If shield is fully consumed, this allows the beastController to take the rest of the damage normally
    }

    private int GetShieldSum()
    {
        int totalShield = 0;
        foreach (KeyValuePair<ShieldSource,int> shieldPortion in _shieldLevels) totalShield += shieldPortion.Value;
        return totalShield;
    }

    public int GetShieldLevel(ShieldSource source)
    {
        return _shieldLevels[source];
    }

    public void ClampShield(ShieldSource source, int clamp)
    {
        if (_shieldLevels[source] > clamp) _shieldLevels[source] = clamp;
    }
}

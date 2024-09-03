using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectStyle
{
    Ensnare,
    Burn,
    Stun,
    Poison
}

public class DamageParticle
{
    public Vector2 Direction;
    public float KnockbackMagnitude;
    public int Damage;
    public float VariationScale;

    // [chance, duration]
    public Dictionary<EffectStyle, float[]> Effects = new Dictionary<EffectStyle, float[]>
    {
        { EffectStyle.Ensnare, new float[] { 0f, 0f } },
        { EffectStyle.Burn, new float[] { 0f, 0f } },
        { EffectStyle.Stun, new float[] { 0f, 0f } },
        { EffectStyle.Poison, new float[] { 0f, 0f } }
    };

    public static DamageParticle GetHealingParticle(int healAmount)
    {
        DamageParticle healParticle = new DamageParticle();
        healParticle.KnockbackMagnitude = 0;
        healParticle.Direction = Vector2.zero;
        healParticle.VariationScale = -1;
        healParticle.Damage = -healAmount;
        return healParticle;
    }

    public void Init(int baseDamage, float knockBackMagnitude, Vector2 direction, BeastController beast)
    {
        KnockbackMagnitude = knockBackMagnitude;
        Direction = direction;

        // Roll for a variation between -15% and +15% of the damage
        VariationScale = (float)(beast.RandomSystem.NextDouble() * 2 - 1); // Random value between -1 and 1
        float variationModifier = VariationScale * 0.15f; // Convert to +/- 15% 
        int damageRoll = Mathf.RoundToInt(baseDamage * (1 + variationModifier));

        // Calculate crit chance based on DEX
        float dex = beast.CurrentState.StatDict[Stat.DEX];
        float slope = (0.5f - 0.05f) / (40 - 1);
        float critChance = 0.05f + slope * (dex - 1); // DEX-scaled crit chance
        Item heldItem = beast.CurrentState.HeldItem;
        if (heldItem != null)
        {
            if (heldItem.EnhancementType == Enhancement.CritChance) critChance += heldItem.EnhancementMagnitude;
            if (heldItem.EnhancementType == Enhancement.ApplyPoison) SetEffectInformation(EffectStyle.Poison, heldItem.EnhancementMagnitude, 1.5f);
        }

        // Roll for a critical hit based on calculated crit chance
        if (beast.RandomSystem.NextDouble() < critChance)
        {
            damageRoll *= 2; // Double the damage on a critical hit
            VariationScale = 2; // Set variation scale to 2 to indicate a critical hit
        }
        if (heldItem != null && heldItem.EnhancementType == Enhancement.Damage) damageRoll = Mathf.RoundToInt((1f + heldItem.EnhancementMagnitude)*damageRoll);
        Damage = damageRoll;
    }

    public void DirtyInit(int fixedDamage)
    {
        KnockbackMagnitude = 0;
        Direction = Vector2.zero;
        Damage = fixedDamage;
    }

    public void ConvertToWhiff()
    {
        Damage = 0;
        VariationScale = -2;
    }

    public void SetEffectInformation(EffectStyle style, float chance, float duration)
    {
        Effects[style] = new float[] { chance, duration };
    }

    public void UpdateDirection(Vector2 newDirection)
    {
        Direction = newDirection;
    }
}

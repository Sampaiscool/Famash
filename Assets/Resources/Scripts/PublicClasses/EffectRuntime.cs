using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RuntimeEffect
{
    public EffectSOBase effect;
    public EffectParams parameters;

    public CardTrigger trigger;
    public SpellSpeed spellSpeed = SpellSpeed.None;

    // Once per turn tracking
    public bool isOncePerTurn = false;
    public bool hasBeenUsedThisTurn = false;

    // Targeting info
    public bool needsTarget = false;
    public CardType targetType = CardType.None;
    public bool targetEnemy = false;
    public bool targetAlly = false;

    public List<CardLocation> allowedLocations = new() { CardLocation.Field };

    [System.NonSerialized] public BaseController effectOwner;

    // Helper to check if a target is valid
    public bool IsValidTarget(CardRuntime target)
    {
        if (effectOwner == null) return false;

        // Card type
        if (targetType != CardType.None && target.cardData.cardType != targetType)
            return false;

        // Ownership
        if (targetEnemy && target.owner == effectOwner) return false;
        if (targetAlly && target.owner != effectOwner) return false;

        // Location
        if (!allowedLocations.Contains(target.location)) return false;

        return true;
    }
}

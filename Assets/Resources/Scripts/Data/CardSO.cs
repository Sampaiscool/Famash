using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Famash/Card")]
public class CardSO : ScriptableObject
{
    [Header("Basic Info")]
    public string cardId;
    public string cardName;
    [TextArea] public string description;

    [Header("Visuals")]
    public Sprite artwork;
    public Sprite frame;

    [Header("Gameplay")]
    public int cost;
    public CardType cardType;
    public SpellType spellType;
    public SpellSpeed spellSpeed;
    public int attack;
    public int health;
    public bool isDoobie;

    [Header("Region Ownership")]
    public RegionSO region;

    [Header("Keywords")]
    public List<KeywordType> keywords = new();

    [Header("Effect Triggers")]
    public List<CardTriggerGroup> triggerGroups = new();
}

[System.Serializable]
public class CardTriggerGroup
{
    public CardTrigger trigger;
    public List<EffectInstance> effects;
}

[System.Serializable]
public class EffectInstance
{
    public EffectSOBase effect;
    public EffectParams parameters;

    public bool isOncePerTurn;

    public bool needsTarget = false;

    public CardType targetType = CardType.None;

    public bool targetEnemy = false;
    public bool targetAlly = false;

    public List<CardLocation> allowedLocations = new() { CardLocation.Field };

    public SpellSpeed spellSpeed = SpellSpeed.None;

    [System.NonSerialized] public BaseController effectOwner;
}




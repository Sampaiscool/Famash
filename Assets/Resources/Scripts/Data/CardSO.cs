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
    public int attack;
    public int health;
    public bool isHeroCard;

    [Header("Region Ownership")]
    public RegionSO region;

    [Header("Keywords")]
    public List<KeywordType> keywords = new(); // simple list of flags

    [Header("Effect Triggers")]
    public List<CardTriggerGroup> triggerGroups = new(); // multiple triggers
}

[System.Serializable]
public class CardTriggerGroup
{
    public CardTrigger trigger;
    public List<EffectSOBase> effects;
}

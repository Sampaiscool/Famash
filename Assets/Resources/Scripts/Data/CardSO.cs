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
    public CardType cardType; // Unit, Spell, Field, Secret, Hero
    public int attack;
    public int health;
    public bool isHeroCard; // Champions in your case

    [Header("Region Ownership")]
    public RegionSO region; // New system

    [Header("Triggers")]
    public List<CardEffectTrigger> triggers;
}

public class CardEffectTrigger
{
    public CardTrigger trigger;
    public EffectSOBase[] effects;
}

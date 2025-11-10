using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardRuntime
{
    public string instanceId;
    public CardSO cardData;

    public int currentHealth;
    public int currentAttack;
    public bool isInPlay;
    public bool isDead;

    public List<KeywordType> activeKeywords = new();
    public List<EffectSOBase> activeEffects = new();

    [System.NonSerialized] public GameObject cardUI;
    [System.NonSerialized] public BaseController owner;

    public event System.Action OnStatsChanged;

    public CardLocation location = CardLocation.Hand;  // Default location is hand
    public int slotIndex = -1;  // -1 indicates no slot, for hand or graveyard

    public CardRuntime(CardSO source)
    {
        instanceId = System.Guid.NewGuid().ToString();
        cardData = source;
        currentHealth = source.health;
        currentAttack = source.attack;
        isInPlay = false;
        isDead = false;

        // copy base keywords and effects
        activeKeywords.AddRange(source.keywords);
        foreach (var group in source.triggerGroups)
            activeEffects.AddRange(group.effects);
    }
    public void ModifyStats(int attackChange, int healthChange)
    {
        currentAttack += attackChange;
        currentHealth += healthChange;
        OnStatsChanged?.Invoke();
    }
    public bool HasKeyword(KeywordType type) => activeKeywords.Contains(type);

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        OnStatsChanged?.Invoke(); // trigger UI update

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            Debug.Log($"{cardData.cardName} has died!");
            owner?.MoveToGraveyard(this);
        }
    }



    public void Trigger(CardTrigger trigger)
    {
        foreach (var group in cardData.triggerGroups)
        {
            if (group.trigger == trigger)
            {
                foreach (var e in group.effects)
                    e?.OnPlay(this);
            }
        }
    }
    public void UpdateUiStats()
    {

    }
}

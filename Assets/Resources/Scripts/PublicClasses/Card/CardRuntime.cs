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
    public List<RuntimeEffect> runtimeEffects = new();

    public HashSet<int> usedActivatedEffects = new();


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

        // Copy base keywords
        activeKeywords.AddRange(source.keywords);

        // Copy effect instances per trigger group
        foreach (var group in source.triggerGroups)
        {
            foreach (var instance in group.effects)
            {
                runtimeEffects.Add(new RuntimeEffect
                {
                    effect = instance.effect,
                    parameters = new EffectParams
                    {
                        amount1 = instance.parameters.amount1,
                        amount2 = instance.parameters.amount2,
                        amount3 = instance.parameters.amount3,
                        cost = instance.parameters.cost,
                    },
                    trigger = group.trigger,
                    isOncePerTurn = instance.isOncePerTurn,
                    hasBeenUsedThisTurn = false
                });
            }
        }
    }

    public void ModifyStats(int attackChange, int healthChange, bool isOverheal = false)
    {
        currentAttack += attackChange;

        if (healthChange > 0)
            HealCard(healthChange, isOverheal);

        OnStatsChanged?.Invoke();
    }
    private void HealCard(int amount, bool isOverheal)
    {
        if (isOverheal)
        {
            currentHealth += amount;
        }
        else
        {
            currentHealth += amount;
            if (currentHealth > cardData.health)
                currentHealth = cardData.health;
        }
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
        foreach (var runtime in runtimeEffects)
        {
            if (runtime.trigger != trigger)
                continue;

            if (runtime.isOncePerTurn && runtime.hasBeenUsedThisTurn)
                continue;

            runtime.effect?.Apply(this, runtime.parameters);

            if (runtime.isOncePerTurn)
                runtime.hasBeenUsedThisTurn = true;
        }
    }



    public void UpdateUiStats()
    {

    }
}

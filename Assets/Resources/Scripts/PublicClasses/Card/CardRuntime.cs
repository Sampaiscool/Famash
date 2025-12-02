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
                        // Also copy target if it exists
                        target = instance.parameters.target
                    },
                    trigger = group.trigger,
                    spellSpeed = instance.spellSpeed,
                    isOncePerTurn = instance.isOncePerTurn,
                    hasBeenUsedThisTurn = false,

                    // TARGETING INFO
                    needsTarget = instance.needsTarget,
                    targetType = instance.targetType,
                    targetEnemy = instance.targetEnemy,
                    targetAlly = instance.targetAlly,
                    allowedLocations = new List<CardLocation>(instance.allowedLocations)
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
        foreach (var re in runtimeEffects)
        {
            if (re.trigger != trigger) continue;

            // Skip once-per-turn
            if (re.isOncePerTurn && re.hasBeenUsedThisTurn)
                continue;

            // If the effect needs a target -> start target selection
            if (re.needsTarget)
            {
                BattleUIManager.Instance.StartTargetSelection(this, re, target =>
                {
                    re.parameters.target = target;
                    SpellStackManager.Instance.AddToStack(this, re);
                });
            }
            else
            {
                // No target? Straight on the stack
                SpellStackManager.Instance.AddToStack(this, re);
            }
        }
    }

    public EffectInstance GetActivateEffect(int effectIndex)
    {
        int counter = 0;

        foreach (var group in cardData.triggerGroups)
        {
            if (group.trigger == CardTrigger.OnActivate)
            {
                foreach (var eff in group.effects)
                {
                    if (counter == effectIndex)
                        return eff;

                    counter++;
                }
            }
        }
        return null;
    }


    public void UpdateUiStats()
    {

    }
}

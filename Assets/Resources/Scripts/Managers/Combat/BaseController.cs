using System.Collections.Generic;
using UnityEngine;

public abstract class BaseController : MonoBehaviour
{
    [Header("Setup")]
    public string controllerName;
    public HeroRuntime hero;

    [Header("Collections")]
    public DeckRuntime deckRuntime;
    public List<CardRuntime> hand = new();
    public CardRuntime[] fieldSlots = new CardRuntime[5]; // 5 slots on the field
    public List<CardRuntime> graveyard = new();

    public bool IsTurnDone { get; set; }
    public bool HasPerformedAction { get; set; }  // any card played or attack
    public bool CanRespond { get; set; }          // non-turn player response
    public bool HasAttack { get; set; }           // turn player attack allowed

    [HideInInspector] public bool isPlayer;
    [HideInInspector] public Transform handParent;

    // Build deck from DeckData
    public virtual void LoadDeck(DeckRuntime runtimeDeck)
    {
        if (runtimeDeck == null)
        {
            Debug.LogWarning($"{controllerName} has no runtime deck!");
            return;
        }

        deckRuntime = runtimeDeck;
        controllerName = runtimeDeck.deckName;
        hero = runtimeDeck.hero;

        Debug.Log($"{controllerName} loaded deck with {deckRuntime.runtimeCards.Count} runtime cards and region: {hero.mainRegion}");
    }

    public virtual void StartTurn(bool drawCard = true)
    {
        IsTurnDone = false;
        HasAttack = true;
        HasPerformedAction = false;

        // Gain mana (refresh and increase max mana)
        hero.StartTurn();  // This ensures both player and AI's mana are refreshed

        if (drawCard)
            DrawCard();

        BattleUIManager.Instance.UpdateHeroUI();
    }

    public virtual void EndTurn()
    {
        IsTurnDone = true;
        HasAttack = false;
        HasPerformedAction = false;
        CanRespond = false;
    }

    public virtual void DrawCard()
    {
        if (deckRuntime == null)
        {
            Debug.LogWarning($"{controllerName} has no deck runtime assigned!");
            return;
        }

        CardRuntime drawn = deckRuntime.DrawCard();
        if (drawn == null) return;

        hand.Add(drawn);
        BattleUIManager.Instance.SpawnCardUI(this, drawn, handParent);

        Debug.Log($"{controllerName} drew {drawn.cardData.cardName}");
    }

    public virtual bool TryPlayCard(CardRuntime card)
    {
        if (card == null) return false;
        if (!hero.CanAfford(card.cardData))
        {
            Debug.Log($"{controllerName} can’t afford {card.cardData.cardName}");
            return false;
        }

        hero.SpendMana(card.cardData.cost);

        // Remove from hand ONLY if it’s a unit card placed on the field
        if (card.cardData.cardType == CardType.Unit)
        {
            hand.Remove(card);
        }

        switch (card.cardData.cardType)
        {
            case CardType.Unit:
                PrepareUnitPlacement(card);
                break;

            case CardType.Spell:
                // ResolveSpell(card);
                break;

            case CardType.Field:
                // ResolveField(card);
                break;

            case CardType.Secret:
                // ResolveSecret(card);
                break;

            case CardType.Hero:
                // ResolveHero(card);
                break;
        }

        HasPerformedAction = true;      // signal that opponent can respond
        CanRespond = false;             // reset own response

        BattleUIManager.Instance.UpdateHeroUI();

        Debug.Log($"{controllerName} played {card.cardData.cardName}");

        return true;
    }


    // Attack method - only turn player can attack
    public virtual bool TryAttack(CardRuntime attacker, CardRuntime target)
    {
        if (!HasAttack)
        {
            Debug.Log($"{controllerName} cannot attack this turn!");
            return false;
        }

        if (attacker == null || target == null)
        {
            Debug.LogWarning("Invalid attacker or target");
            return false;
        }

        // Deal damage
        target.TakeDamage(attacker.currentAttack);
        attacker.isExhausted = true;
        HasAttack = false;
        HasPerformedAction = true;

        CanRespond = false; // after attack, opponent may respond in your system

        BattleUIManager.Instance.UpdateHeroUI();

        Debug.Log($"{controllerName}'s {attacker.cardData.cardName} attacked {target.cardData.cardName}");

        return true;
    }
    private void PrepareUnitPlacement(CardRuntime card)
    {
        if (!isPlayer) // AI will handle separately
        {
            // AI just puts the first unit in the first free slot
            for (int i = 0; i < fieldSlots.Length; i++)
            {
                if (fieldSlots[i] == null)
                {
                    PlaceUnitInSlot(card, i);
                    break;
                }
            }
            return;
        }

        // Human player: wait for click
        BattleUIManager.Instance.HighlightAvailableSlots(this);

        // Subscribe to slot-click event
        BattleUIManager.Instance.OnFieldSlotClicked = (slotIndex) =>
        {
            if (fieldSlots[slotIndex] != null) return; // occupied
            PlaceUnitInSlot(card, slotIndex);
            BattleUIManager.Instance.ClearSlotHighlights();
            BattleUIManager.Instance.OnFieldSlotClicked = null; // unsubscribe
        };
    }
    private void PlaceUnitInSlot(CardRuntime card, int slotIndex)
    {
        fieldSlots[slotIndex] = card;
        card.isInPlay = true;

        // Move the UI to the slot
        Transform slotTransform = BattleUIManager.Instance.GetAvailableFieldSlots(this)[slotIndex];
        card.cardUI.transform.SetParent(slotTransform, false);
        card.cardUI.transform.localPosition = Vector3.zero;

        Debug.Log($"{controllerName} placed {card.cardData.cardName} in slot {slotIndex}");
    }


    //public virtual void MoveToGraveyard(CardRuntime card)
    //{
    //    if (field.Contains(card))
    //        field.Remove(card);
    //    else if (hand.Contains(card))
    //        hand.Remove(card);

    //    graveyard.Add(card);

    //    BattleUIManager.Instance.RefreshHandUI(this);
    //    Debug.Log($"{controllerName} moved {card.cardData.cardName} to graveyard");
    //}
}

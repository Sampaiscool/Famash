using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
    public CardRuntime[] activeSlots = new CardRuntime[5];
    public List<CardRuntime> graveyard = new();

    public List<CardRuntime> preparedAttacks = new List<CardRuntime>();
    public List<CardRuntime> preparedBlocks = new List<CardRuntime>();



    public bool IsTurnDone { get; set; }
    public bool HasPerformedAction { get; set; }  // any card played or attack
    public bool CanRespond { get; set; }          // non-turn player response
    public bool HasAttack { get; set; }           // turn player attack allowed

    public delegate IEnumerator ResponseWindowDelegate(BaseController actor, CardRuntime playedCard);

    public static event Action<BaseController, CardRuntime> OnResponseWindow;

    [HideInInspector] public bool isPlayer;
    [HideInInspector] public Transform handParent;

    private bool isDrawing = false;
    private int pendingDraws = 0;

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

        BattleUIManager.Instance.UpdateHeroUI();

        foreach (var card in fieldSlots)
        {
            if (card == null || card.runtimeEffects == null)
                continue;

            foreach (var effect in card.runtimeEffects)
                effect.hasBeenUsedThisTurn = false;
        }
    }

    public virtual void EndTurn()
    {
        IsTurnDone = true;
        HasAttack = false;
        HasPerformedAction = false;
        CanRespond = false;
    }

    public void DrawCards(int amount)
    {
        if (amount <= 0) return;
        pendingDraws += amount;

        if (!isDrawing)
            StartCoroutine(DrawCardsRoutine());
    }

    /// <summary>
    /// Tries to play a card from the hand
    /// </summary>
    /// <param name="card">Card thats getting player</param>
    /// <param name="openResponseWindow">Wheter the player should get a response window</param>
    /// <returns>If the card got played</returns>
    public virtual bool TryPlayCard(CardRuntime card, bool openResponseWindow = true)
    {
        if (card == null || !hero.CanAfford(card.cardData))
            return false;

        if (card.location == CardLocation.Hand)
            card.location = CardLocation.Field;

        // PLAYER UNIT = use placement system
        if (isPlayer && card.cardData.cardType == CardType.Unit)
        {
            PrepareUnitPlacement(card, openResponseWindow);
            return true;
        }

        // Spend mana for spells/AI unit placement
        hero.SpendMana(card.cardData.cost);

        switch (card.cardData.cardType)
        {
            case CardType.Unit:
                {
                    hand.Remove(card);
                    for (int i = 0; i < fieldSlots.Length; i++)
                        if (fieldSlots[i] == null)
                        {
                            PlaceUnitInSlot(card, i);
                            card.location = CardLocation.Field;
                            card.slotIndex = i;
                            break;
                        }
                    break;
                }

            case CardType.Spell:
                {
                    // Spells: remove from hand and then trigger OnPlay (which queues runtime effects)
                    hand.Remove(card);
                    break;
                }
        }

        card.Trigger(CardTrigger.OnPlay);

        // Give the other player a response window **only if no stack exists yet**
        if (!SpellStackManager.Instance.HasPendingStack)
        {
            BattleManager.Instance.NotifyActionTaken(card.owner);
        }

        HasPerformedAction = true;
        CanRespond = false;
        BattleUIManager.Instance.UpdateHeroUI();

        return true;
    }



    public void PrepareAttack(CardRuntime card)
    {
        if (card == null) return;

        int fromIndex = card.slotIndex;

        // Only move if the matching active slot is available
        if (fromIndex >= 0 && fromIndex < activeSlots.Length)
        {
            MoveCardToActiveSlot(card, fromIndex);
            preparedAttacks.Add(card);
        }
        else
        {
            Debug.LogWarning($"{card.cardData.cardName} could not find a matching active slot.");
        }

        if (preparedAttacks.Count > 0)
            BattleUIManager.Instance.ShowConfirmAttackButton();
    }


    public void ConfirmAllAttacks()
    {
        foreach (var card in preparedAttacks)
        {
            // Attack logic, assuming it’s targeting the opponent’s cards or hero
            if (card != null && card.isInPlay)
            {
                // Assuming you have a target for each attack, for now, we will assume each attack targets a random opponent card
                CardRuntime target = GetOppositeOpponentCard(card.slotIndex);
                if (target != null)
                {
                    TryAttack(card, target);
                }
                else
                {
                    // Target opponent hero if no valid target
                    TryAttack(card, null);
                }
            }
        }

        // Reset prepared attacks after confirming
        preparedAttacks.Clear();
    }

    public CardRuntime GetOppositeOpponentCard(int attackingSlotIndex)
    {
        // Check if the attacking slot index is valid
        if (attackingSlotIndex < 0 || attackingSlotIndex >= fieldSlots.Length)
        {
            Debug.LogWarning("Invalid slot index for attack.");
            return null;
        }
        CardRuntime target = BattleManager.Instance.player.fieldSlots[attackingSlotIndex];

        // If there's a card in the opponent's corresponding field slot, return it
        if (target != null)
        {
            return target;
        }
        Debug.Log($"{controllerName} attack target not found in slot {attackingSlotIndex}. Attacking opponent's hero instead.");

        return null;
    }

    // Attack method - only turn player can attack
    public virtual bool TryAttack(CardRuntime attacker, CardRuntime target)
    {
        if (!HasAttack)
        {
            Debug.Log($"{controllerName} cannot attack this turn!");
            return false;
        }

        if (attacker == null)
        {
            Debug.LogWarning("Invalid attacker");
            return false;
        }

        if (target != null)
        {
            // Simultaneous damage exchange
            target.TakeDamage(attacker.currentAttack);
            attacker.TakeDamage(target.currentAttack);

            Debug.Log($"{controllerName}'s {attacker.cardData.cardName} attacked {target.cardData.cardName}");

            // Remove destroyed cards
            if (attacker.currentHealth <= 0)
            {
                Debug.Log($"{attacker.cardData.cardName} was destroyed!");
            }
            if (target.currentHealth <= 0)
            {
                Debug.Log($"{target.cardData.cardName} was destroyed!");
            }

            HasPerformedAction = true;
            BattleUIManager.Instance.UpdateHeroUI();

            return true;
        }
        else
        {
            // Direct hit to enemy hero
            int attackerDamage = attacker.currentAttack;
            BattleManager.Instance.otherPlayer.hero.TakeDamage(attackerDamage);
            Debug.Log($"{controllerName}'s {attacker.cardData.cardName} attacked the opponent's hero directly!");
            return true;
        }
    }

    public void ResolvePreparedAttacks(BaseController defender)
    {
        if (preparedAttacks.Count == 0)
            return;

        Debug.Log($"{controllerName} begins resolving attacks!");

        for (int i = 0; i < activeSlots.Length; i++)
        {
            CardRuntime attacker = activeSlots[i];
            if (attacker == null || !preparedAttacks.Contains(attacker))
                continue;

            CardRuntime blocker = defender.activeSlots[i];

            if (blocker != null)
            {
                // Clash between attacker and blocker
                TryAttack(attacker, blocker);
            }
            else
            {
                // No blocker — direct hit
                TryAttack(attacker, null);
            }

            // Wait a short moment for clarity if you animate later
            // (You can add: yield return new WaitForSeconds(0.3f); if coroutine-based)
        }

        // Clean-up phase — return survivors to their field slots
        for (int i = 0; i < activeSlots.Length; i++)
        {
            CardRuntime attacker = activeSlots[i];
            if (attacker != null && attacker.currentHealth > 0)
                MoveCardBackToField(attacker, i);
        }

        for (int i = 0; i < defender.activeSlots.Length; i++)
        {
            CardRuntime blocker = defender.activeSlots[i];
            if (blocker != null && blocker.currentHealth > 0)
                defender.MoveCardBackToField(blocker, i);
        }

        preparedAttacks.Clear();
        HasAttack = false;

        Debug.Log($"{controllerName} finished resolving attacks.");
    }


    // inside BaseController (or wherever PrepareUnitPlacement lives)
    private void PrepareUnitPlacement(CardRuntime card, bool openResponseWindow)
    {
        if (!isPlayer)
        {
            // AI auto-place
            for (int i = 0; i < fieldSlots.Length; i++)
                if (fieldSlots[i] == null)
                {
                    PlaceUnitInSlot(card, i);
                    card.location = CardLocation.Field;
                    card.slotIndex = i;

                    card.Trigger(CardTrigger.OnPlay);

                    break;
                }

            return;
        }

        // Player: show placement UI
        BattleUIManager.Instance.HighlightAvailableSlots(this);

        BattleUIManager.Instance.OnFieldSlotClicked = (slotIndex) =>
        {
            if (fieldSlots[slotIndex] != null) return;

            hero.SpendMana(card.cardData.cost);
            hand.Remove(card);
            PlaceUnitInSlot(card, slotIndex);

            card.location = CardLocation.Field;
            card.slotIndex = slotIndex;

            // Trigger OnPlay -> adds to stack
            card.Trigger(CardTrigger.OnPlay);

            HasPerformedAction = true;
            CanRespond = false;

            BattleUIManager.Instance.ClearSlotHighlights();
            BattleUIManager.Instance.OnFieldSlotClicked = null;
            BattleUIManager.Instance.UpdateHeroUI();
        };
    }




    public void PlaceUnitInSlot(CardRuntime card, int slotIndex)
    {
        fieldSlots[slotIndex] = card;
        card.isInPlay = true;
        card.location = CardLocation.Field;
        card.slotIndex = slotIndex;

        // Move the UI
        Transform slotTransform = BattleUIManager.Instance.GetAvailableFieldSlots(this)[slotIndex];
        card.cardUI.transform.SetParent(slotTransform, false);
        card.cardUI.transform.localPosition = Vector3.zero;

        // Notify battle manager: other player can respond
        if (BattleManager.Instance.priorityState == BattleManager.PriorityState.NormalTurn)
        {
            BattleManager.Instance.NotifyActionTaken(this);
        }
    }

    public void MoveCardToActiveSlot(CardRuntime card, int slotIndex)
    {
        if (card == null)
        {
            Debug.LogError("MoveCardToActiveSlot: card is null");
            return;
        }

        if (slotIndex < 0 || slotIndex >= activeSlots.Length)
        {
            Debug.LogError("Invalid slot index for active slot.");
            return;
        }

        // Save old field index so RemoveCardFromFieldSlot can clear it
        int oldFieldIndex = card.slotIndex;

        // Remove from field array if it was on the field
        RemoveCardFromFieldSlot(card);

        // Put into active slots array
        activeSlots[slotIndex] = card;

        // Update card’s location and slot index
        card.location = CardLocation.Active;
        card.slotIndex = slotIndex;

        // Move the UI to the active slot transforms (use dedicated active slot parents)
        var activeSlotTransforms = BattleUIManager.Instance.GetAvailableActiveSlots(this);
        if (activeSlotTransforms == null || slotIndex >= activeSlotTransforms.Length)
        {
            Debug.LogError("No active slot transforms available or index out of range.");
        }
        else if (card.cardUI != null)
        {
            Transform slotTransform = activeSlotTransforms[slotIndex];
            card.cardUI.transform.SetParent(slotTransform, false);
            card.cardUI.transform.localPosition = Vector3.zero;
        }
        else
        {
            Debug.LogWarning("MoveCardToActiveSlot: card.cardUI is null, cannot move visual.");
        }

        // Update hero / field UI state if needed
        BattleUIManager.Instance.UpdateHeroUI();
    }

    private void RemoveCardFromFieldSlot(CardRuntime card)
    {
        if (card == null) return;

        // If card.slotIndex was valid and the fieldSlots holds the card, clear it
        int idx = card.slotIndex;
        if (idx >= 0 && idx < fieldSlots.Length && fieldSlots[idx] == card)
        {
            fieldSlots[idx] = null;
            Debug.Log($"{card.cardData.cardName} removed from field slot {idx}");
        }

        // Clear any lingering field flags (don't clear slotIndex — caller will set new one)
        // card.slotIndex = -1; // caller updates slotIndex when placing into active
    }
    public void MoveCardBackToField(CardRuntime card, int fromActiveIndex)
    {
        if (card == null)
            return;

        // Find the first empty field slot
        int fieldIndex = fromActiveIndex;

        if (fieldIndex == -1)
        {
            Debug.LogWarning("No available field slot to return card to!");
            return;
        }

        // Clear from active slot array
        if (fromActiveIndex >= 0 && fromActiveIndex < activeSlots.Length && activeSlots[fromActiveIndex] == card)
            activeSlots[fromActiveIndex] = null;

        // Assign to field slot
        fieldSlots[fieldIndex] = card;

        // Update data
        card.location = CardLocation.Field;
        card.slotIndex = fieldIndex;

        // Move the visual back to the field UI
        var fieldSlotTransforms = BattleUIManager.Instance.GetAvailableFieldSlots(this);
        if (fieldSlotTransforms == null || fieldIndex >= fieldSlotTransforms.Length)
        {
            Debug.LogError("No field slot transforms available or index out of range.");
            return;
        }

        if (card.cardUI != null)
        {
            Transform slotTransform = fieldSlotTransforms[fieldIndex];
            card.cardUI.transform.SetParent(slotTransform, false);
            card.cardUI.transform.localPosition = Vector3.zero;
        }

        Debug.Log($"{card.cardData.cardName} returned to field slot {fieldIndex}");
    }


    public virtual void MoveToGraveyard(CardRuntime card)
    {
        if (card == null) return;

        // Remove from any zone that holds it
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            if (fieldSlots[i] == card)
            {
                fieldSlots[i] = null;
                break;
            }
        }

        for (int i = 0; i < activeSlots.Length; i++)
        {
            if (activeSlots[i] == card)
            {
                activeSlots[i] = null;
                break;
            }
        }

        if (hand.Contains(card))
            hand.Remove(card);

        // Add to graveyard
        graveyard.Add(card);
        card.isInPlay = false;
        card.location = CardLocation.Graveyard;

        // Optionally hide or disable the card's visual on board
        if (card.cardUI != null)
        {
            card.cardUI.gameObject.SetActive(false);
        }

        BattleUIManager.Instance.UpdateHeroUI();
        Debug.Log($"{controllerName} moved {card.cardData.cardName} to graveyard");
    }

    private System.Collections.IEnumerator DrawCardsRoutine()
    {
        isDrawing = true;

        // while there are requests queued, attempt to draw
        while (pendingDraws > 0)
        {
            // safety: if no deck, stop
            if (deckRuntime == null)
            {
                Debug.LogWarning($"{controllerName} has no deck runtime assigned!");
                pendingDraws = 0;
                break;
            }

            // try draw one runtime card
            CardRuntime drawn = deckRuntime.DrawCard();
            if (drawn == null)
            {
                Debug.Log($"{controllerName} tried to draw but the deck is empty!");
                pendingDraws = 0;
                break;
            }

            // spawn UI and prepare animation
            // NOTE: SpawnCardUI must return the spawned GameObject (see earlier fix)
            GameObject cardObj = BattleUIManager.Instance.SpawnCardUI(this, drawn, handParent);
            if (cardObj == null)
            {
                // fallback: if SpawnCardUI returned null, still add to hand and continue
                hand.Add(drawn);
                Debug.LogWarning("SpawnCardUI returned null — card added to hand without animation.");
                pendingDraws--;
                yield return null;
                continue;
            }

            // Ensure RectTransform and CanvasGroup exist
            var rect = cardObj.GetComponent<RectTransform>();
            CanvasGroup canvasGroup = cardObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = cardObj.AddComponent<CanvasGroup>();

            // start tiny + invisible
            rect.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;

            // animate grow + fade (feel free to tweak duration)
            float duration = 0.35f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / duration);
                rect.localScale = Vector3.one * p;
                canvasGroup.alpha = p;
                yield return null;
            }

            rect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;

            // now that the UI is visible, add to hand list
            hand.Add(drawn);

            // small delay before next queued draw (tweak as needed)
            yield return new WaitForSeconds(0.12f);

            // consumed one queued draw
            pendingDraws--;
        }

        isDrawing = false;
    }

    /// <summary>
    /// Called by BattleManager to notify all listeners that a response window has begun.
    /// </summary>
    public static void TriggerResponseWindow(BaseController responder, CardRuntime playedCard = null)
    {
        OnResponseWindow?.Invoke(responder, playedCard);
    }
}

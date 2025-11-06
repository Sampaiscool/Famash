using System.Collections;
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

    public delegate IEnumerator ResponseWindowDelegate(BaseController actor, CardRuntime playedCard);
    public static event ResponseWindowDelegate OnResponseWindow;

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


    public virtual bool TryPlayCard(CardRuntime card)
    {
        if (card == null || !hero.CanAfford(card.cardData))
            return false;

        // For player units: wait for placement instead of instant play
        if (isPlayer && card.cardData.cardType == CardType.Unit)
        {
            PrepareUnitPlacement(card);
            return false; // Wait for placement
        }

        hero.SpendMana(card.cardData.cost);

        switch (card.cardData.cardType)
        {
            case CardType.Unit:
                hand.Remove(card);
                for (int i = 0; i < fieldSlots.Length; i++)
                {
                    if (fieldSlots[i] == null)
                    {
                        PlaceUnitInSlot(card, i);
                        break;
                    }
                }
                break;
            case CardType.Spell:
                break;
        }

        HasPerformedAction = true;
        CanRespond = false;

        BattleUIManager.Instance.UpdateHeroUI();
        Debug.Log($"{controllerName} played {card.cardData.cardName}");

        // Response window logic
        if (isPlayer)
        {
            // Player plays -> AI responds
            BattleManager.Instance.StartResponseWindow(BattleManager.Instance.opponent);
        }
        else
        {
            // AI plays -> Player responds
            BattleManager.Instance.StartResponseWindow(BattleManager.Instance.player);
        }

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
        if (!isPlayer)
        {
            // AI logic stays the same
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

        // Show highlights
        BattleUIManager.Instance.HighlightAvailableSlots(this);

        // Set up slot click
        BattleUIManager.Instance.OnFieldSlotClicked = (slotIndex) =>
        {
            if (fieldSlots[slotIndex] != null)
                return;

            hero.SpendMana(card.cardData.cost);
            hand.Remove(card);
            PlaceUnitInSlot(card, slotIndex);

            HasPerformedAction = true;
            CanRespond = false;

            BattleUIManager.Instance.ClearSlotHighlights();
            BattleUIManager.Instance.OnFieldSlotClicked = null;
            BattleUIManager.Instance.UpdateHeroUI();

            Debug.Log($"{controllerName} placed {card.cardData.cardName} in slot {slotIndex}");

            BattleManager.Instance.StartResponseWindow(BattleManager.Instance.opponent);
        };
    }

    public void PlaceUnitInSlot(CardRuntime card, int slotIndex)
    {
        fieldSlots[slotIndex] = card;
        card.isInPlay = true;

        // Move the UI to the slot
        Transform slotTransform = BattleUIManager.Instance.GetAvailableFieldSlots(this)[slotIndex];
        card.cardUI.transform.SetParent(slotTransform, false);
        card.cardUI.transform.localPosition = Vector3.zero;

        OnResponseWindow.ToString();
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

            Debug.Log($"{controllerName} drew {drawn.cardData.cardName}");

            // small delay before next queued draw (tweak as needed)
            yield return new WaitForSeconds(0.12f);

            // consumed one queued draw
            pendingDraws--;
        }

        isDrawing = false;
    }
}

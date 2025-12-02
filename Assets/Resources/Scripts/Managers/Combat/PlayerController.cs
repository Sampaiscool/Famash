using UnityEngine;

public class PlayerController : BaseController
{
    private CardRuntime selectedCard = null;

    // Called by CardInGame when the player clicks a card
    public void OnCardClicked(CardRuntime card)
    {
        // If already choosing a slot, this acts as a cancel toggle
        if (selectedCard != null)
        {
            CancelPlacementMode();
            return;
        }

        // Only allow card play if it's your priority to act
        if (!BattleManager.Instance.IsPlayerAllowedToAct(this))
        {
            Debug.Log("Not your priority to act right now.");
            return;
        }

        // If it's a unit in hand, enter placement mode
        if (hand.Contains(card) && card.cardData.cardType == CardType.Unit)
        {
            selectedCard = card;
            BattleUIManager.Instance.HighlightAvailableSlots(this);
            // BattleManager will adjust end-turn button label via GivePriorityTo/HandlePass.
            return;
        }

        // Otherwise, try to play card (spells, etc)
        if (hand.Contains(card))
        {
            TryPlayCard(card, true);
            // TryPlayCard will call card.Trigger(OnPlay) which will add effects to the stack
            // and BattleManager.NotifyActionTaken() should be called by SpellStackManager.AddToStack
            // so we don't call StartResponseWindow here anymore.
        }
    }

    public void CancelPlacementMode()
    {
        selectedCard = null;
        BattleUIManager.Instance.ClearSlotHighlights();
        BattleUIManager.Instance.OnFieldSlotClicked = null;
        BattleUIManager.Instance.SetEndTurnButtonLabel("End Turn");
        BattleUIManager.Instance.SetEndTurnButtonActive(true);

        Debug.Log("Cancelled placement mode.");
    }

    public override void EndTurn()
    {
        base.EndTurn();
        CancelPlacementMode();
    }

    public bool IsPlacingCard => selectedCard != null;

    // This should be set from the UI when the player clicks a highlighted slot.
    // We keep the logic in base PrepareUnitPlacement but provide a helper here to confirm placement.
    public void ConfirmPlacementToSlot(int slotIndex)
    {
        if (selectedCard == null) return;
        // Delegate to BaseController's placement handler (we assume it uses the standard PrepareUnitPlacement logic).
        // If you use the lambda approach in PrepareUnitPlacement, you'll have to wire OnFieldSlotClicked to call this.
        // For safety, call the PrepareUnitPlacement's slot handler here if needed:
        if (BattleUIManager.Instance.OnFieldSlotClicked != null)
            BattleUIManager.Instance.OnFieldSlotClicked(slotIndex);

        selectedCard = null;
    }
}

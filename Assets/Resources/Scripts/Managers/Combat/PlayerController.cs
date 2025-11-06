using UnityEngine;

public class PlayerController : BaseController
{
    private CardRuntime selectedCard = null;

    public void OnCardClicked(CardRuntime card)
    {
        // If already choosing a slot, this acts as a cancel toggle
        if (selectedCard != null)
        {
            CancelPlacementMode();
            return;
        }

        // Only allow card play if it’s your turn or you can respond
        if (!BattleManager.Instance.IsPlayerAllowedToAct(this))
        {
            Debug.Log("Not your turn or you can’t respond right now.");
            return;
        }

        // Enter placement mode
        selectedCard = card;
        BattleUIManager.Instance.HighlightAvailableSlots(this);
        BattleUIManager.Instance.SetEndTurnButtonLabel("Cancel");
        BattleUIManager.Instance.SetEndTurnButtonActive(true);

        BattleUIManager.Instance.OnFieldSlotClicked = (slotIndex) =>
        {
            if (fieldSlots[slotIndex] != null) return;

            // Actually place and spend
            hero.SpendMana(selectedCard.cardData.cost);
            hand.Remove(selectedCard);
            PlaceUnitInSlot(selectedCard, slotIndex);
            BattleUIManager.Instance.ClearSlotHighlights();
            BattleUIManager.Instance.OnFieldSlotClicked = null;

            HasPerformedAction = true;
            CanRespond = false;
            selectedCard = null;

            BattleUIManager.Instance.SetEndTurnButtonLabel("End Turn");
            BattleUIManager.Instance.UpdateHeroUI();

            // Notify battle manager to open opponent response
            BattleManager.Instance.StartResponseWindow(BattleManager.Instance.opponent);
        };
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
}

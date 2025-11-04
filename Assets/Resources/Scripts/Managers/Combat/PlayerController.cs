using UnityEngine;

public class PlayerController : BaseController
{
    // Called by UI when player clicks a card
    public void OnCardClicked(CardRuntime card)
    {
        if (!hand.Contains(card))
            return;

        bool played = TryPlayCard(card);
        if (!played)
            return;

        // After player plays a card, opponent can respond
        BattleManager.Instance.opponent.CanRespond = true;
    }

    // You can add attack logic later:
    public void OnAttack()
    {
        if (!HasAttack)
        {
            Debug.Log("Cannot attack right now!");
            return;
        }

        // Resolve attack here
        Debug.Log($"{controllerName} attacks!");

        HasAttack = false;

        // After attacking, opponent can respond with cards
        BattleManager.Instance.opponent.CanRespond = true;
    }
}

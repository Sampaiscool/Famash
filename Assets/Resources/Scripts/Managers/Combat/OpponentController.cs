using System.Collections;
using UnityEngine;

public class OpponentController : BaseController
{
    public override void StartTurn(bool drawCard = true)
    {
        IsTurnDone = false;
        HasAttack = true;
        HasPerformedAction = false;

        // Refresh and gain mana for the AI at the start of their turn
        base.StartTurn(drawCard);  // Call base StartTurn() to refresh mana

        StartCoroutine(AIPlayLoop());
    }

    private IEnumerator AIPlayLoop()
    {
        while (!IsTurnDone)
        {
            yield return new WaitForSeconds(1f);

            // Try to find a playable card
            CardRuntime playableCard = null;
            foreach (var c in hand)
            {
                if (hero.CanAfford(c.cardData))
                {
                    playableCard = c;
                    break;
                }
            }

            if (playableCard != null)
            {
                // AI plays the card
                TryPlayCard(playableCard);
                Debug.Log($"{controllerName} played {playableCard.cardData.cardName}");

                // Start a proper response window for the player
                BattleManager.Instance.StartResponseWindow(BattleManager.Instance.player);

                // Wait until the response window is over
                yield return new WaitUntil(() => BattleManager.Instance.currentResponder == null);

                // Small breather before next action
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // No playable cards, end turn after a small delay
                yield return new WaitForSeconds(1f);
                IsTurnDone = true;
            }
        }
    }

    private void OnEnable()
    {
        BaseController.OnResponseWindow += HandleResponseWindow;
    }

    private void OnDisable()
    {
        BaseController.OnResponseWindow -= HandleResponseWindow;
    }

    private IEnumerator HandleResponseWindow(BaseController actor, CardRuntime playedCard)
    {
        if (actor == this) yield break; // AI does not respond to itself

        // Check AI hand for playable cards
        CardRuntime playable = null;
        foreach (var c in hand)
        {
            if (hero.CanAfford(c.cardData))
            {
                playable = c;
                break;
            }
        }

        if (playable != null)
        {
            yield return new WaitForSeconds(0.5f); // small reaction delay
            TryPlayCard(playable);
        }
    }

}



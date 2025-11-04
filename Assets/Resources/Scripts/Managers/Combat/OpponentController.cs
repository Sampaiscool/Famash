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

            // Play a card if possible
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
                TryPlayCard(playableCard);
                Debug.Log($"{controllerName} played {playableCard.cardData.cardName}");

                // Allow the player to respond to the AI's action
                BattleManager.Instance.player.CanRespond = true;
                yield return new WaitForSeconds(0.5f);  // Wait for the player's response
                BattleManager.Instance.player.CanRespond = false;
            }
            else
            {
                // No playable cards, end turn after a pause
                yield return new WaitForSeconds(1f);
                IsTurnDone = true;
            }
        }
    }
}



using System.Collections;
using UnityEngine;

public class OpponentController : BaseController
{
    public override void StartTurn(bool drawCard = true)
    {
        IsTurnDone = false;
        HasAttack = true;
        HasPerformedAction = false;

        base.StartTurn(drawCard);  // refresh mana, etc.

        StartCoroutine(AIPlayLoop());
    }

    private IEnumerator AIPlayLoop()
    {
        // Play phase
        while (!IsTurnDone)
        {
            yield return new WaitForSeconds(1f);

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

                // Player gets response window
                BattleManager.Instance.StartResponseWindow(BattleManager.Instance.player);

                // Wait for responses
                yield return new WaitUntil(() => BattleManager.Instance.currentResponder == null);

                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // No more plays, continue to attack phase
                yield return new WaitForSeconds(0.75f);
                break;
            }
        }

        // Attack phase (if any attackers available)
        yield return StartCoroutine(AIAttackPhase());

        // End turn after a short delay
        yield return new WaitForSeconds(1f);
        IsTurnDone = true;
    }

    private IEnumerator AIAttackPhase()
    {
        HasAttack = true;

        Debug.Log($"{controllerName} is preparing attacks...");

        bool preparedAny = false;

        // Go through all field slots and prepare attack for each unit
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            CardRuntime unit = fieldSlots[i];
            if (unit != null && HasAttack == true)
            {
                MoveCardToActiveSlot(unit, i);
                BattleManager.Instance.PrepareAttack(unit);
                preparedAny = true;
                yield return new WaitForSeconds(0.25f); // slight delay for pacing
            }
        }

        if (preparedAny)
        {
            yield return new WaitForSeconds(0.75f);
            Debug.Log($"{controllerName} confirms attacks!");

            // Start the player’s response window before attacks resolve
            BattleManager.Instance.StartResponseWindow(BattleManager.Instance.player);

            // Wait until player finishes responding
            yield return new WaitUntil(() => BattleManager.Instance.currentResponder == null);

            // Confirm and resolve the attacks
            BattleManager.Instance.ConfirmAttack();
        }
        else
        {
            Debug.Log($"{controllerName} has no attackers.");
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
        if (actor == this) yield break; // don't respond to own plays

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
            yield return new WaitForSeconds(0.5f);
            TryPlayCard(playable);
        }
    }
}

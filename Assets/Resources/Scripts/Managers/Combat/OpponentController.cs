using System.Collections;
using System.Linq;
using UnityEngine;

public class OpponentController : BaseController
{
    private bool hasRespondedThisWindow = false;
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
        while (!IsTurnDone)
        {
            yield return new WaitForSeconds(1f);

            var playableCard = hand.FirstOrDefault(c => hero.CanAfford(c.cardData));
            if (playableCard == null) break;

            TryPlayCard(playableCard, true); // don’t open response window here
                                              // explicitly open response only once, controlled by BattleManager
            BattleManager.Instance.StartResponseWindow(BattleManager.Instance.player, playableCard);
            yield return new WaitUntil(() => BattleManager.Instance.currentResponder == null);

            yield return new WaitForSeconds(0.25f);
        }

        yield return StartCoroutine(AIAttackPhase());
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
            BattleManager.Instance.StartResponseWindow(BattleManager.Instance.player, null);

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
    private IEnumerator AIAutoBlock()
    {
        var attacker = BattleManager.Instance.turnPlayer;
        var defender = this;

        // Small delay for pacing
        yield return new WaitForSeconds(0.5f);

        bool blockedAny = false;

        for (int i = 0; i < attacker.activeSlots.Length; i++)
        {
            var attackingCard = attacker.activeSlots[i];
            if (attackingCard == null) continue;

            // Try to find a unit on the defender’s field in the same slot
            var potentialBlocker = defender.fieldSlots[i];
            if (potentialBlocker != null)
            {
                defender.MoveCardToActiveSlot(potentialBlocker, i);
                defender.preparedBlocks.Add(potentialBlocker);

                Debug.Log($"{controllerName} blocks {attacker.controllerName}'s {attackingCard.cardData.cardName} with {potentialBlocker.cardData.cardName} in lane {i}.");

                blockedAny = true;
                yield return new WaitForSeconds(0.3f);
            }
        }

        if (blockedAny)
        {
            Debug.Log($"{controllerName} finished assigning blockers.");
        }
        else
        {
            Debug.Log($"{controllerName} had no available blockers.");
        }

        // Close the response window so combat can continue
        BattleManager.Instance.EndResponseWindow();
    }


    private void OnEnable()
    {
        BaseController.OnResponseWindow += OnResponseWindowHandler;
    }

    private void OnDisable()
    {
        BaseController.OnResponseWindow -= OnResponseWindowHandler;
    }
    private void OnResponseWindowHandler(BaseController actor, CardRuntime playedCard)
    {
        StartCoroutine(HandleResponseWindow(actor, playedCard));
    }

    private IEnumerator HandleResponseWindow(BaseController actor, CardRuntime playedCard)
    {
        if (actor == this) yield break;
        if (hasRespondedThisWindow) yield break;
        hasRespondedThisWindow = true;

        if (BattleManager.Instance.currentResponder == this)
        {
            yield return StartCoroutine(AIAutoBlock());
        }
        else
        {
            var playable = hand.FirstOrDefault(c => hero.CanAfford(c.cardData));
            if (playable != null)
            {
                yield return new WaitForSeconds(0.5f);
                TryPlayCard(playable);
            }
        }

        hasRespondedThisWindow = false;
    }

}

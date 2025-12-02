using System.Collections;
using System.Linq;
using UnityEngine;

public class OpponentController : BaseController
{
    private bool hasRespondedThisPriority = false;

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
            yield return new WaitForSeconds(0.3f);

            // Wait until AI has priority
            if (BattleManager.Instance.priorityOwner != this)
            {
                yield return null;
                continue;
            }

            // --- Phase 1: Respond to stack if it exists ---
            if (SpellStackManager.Instance.HasPendingStack)
            {
                Debug.Log($"{controllerName} responding to stack...");
                // Respond or pass
                var playableStack = hand.FirstOrDefault(c => hero.CanAfford(c.cardData));
                if (playableStack != null)
                    TryPlayCard(playableStack, openResponseWindow: true);

                BattleManager.Instance.HandlePass(this);
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            // --- Phase 2: Play cards during normal turn priority ---
            var playableCard = hand.FirstOrDefault(c => hero.CanAfford(c.cardData));
            if (playableCard != null)
            {
                TryPlayCard(playableCard, openResponseWindow: true);

                // Notify BattleManager so the other player can respond
                BattleManager.Instance.NotifyActionTaken(this);
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            // --- Phase 3: Prepare attacks if normal turn ---
            if (BattleManager.Instance.priorityState == BattleManager.PriorityState.NormalTurn && HasAttack)
            {
                yield return StartCoroutine(AIAttackPhase());
                IsTurnDone = true; // AI finishes turn after attacks
                yield break;
            }

            // --- Phase 4: No actions, pass priority ---
            BattleManager.Instance.HandlePass(this);
            yield return new WaitForSeconds(0.2f);
        }
    }


    private IEnumerator AIAttackPhase()
    {
        HasAttack = true;

        // Move units to active slots
        for (int i = 0; i < fieldSlots.Length; i++)
        {
            var unit = fieldSlots[i];
            if (unit != null)
            {
                MoveCardToActiveSlot(unit, i);
                BattleManager.Instance.PrepareAttack(unit);
                yield return new WaitForSeconds(0.25f);
            }
        }

        // Open priority window for opponent to respond
        BattleManager.Instance.priorityState = BattleManager.PriorityState.PriorityWindow;
        BattleManager.Instance.GivePriorityTo(BattleManager.Instance.player);

        // Wait until the priority window ends (two passes or stack resolves)
        yield return new WaitUntil(() => BattleManager.Instance.priorityState == BattleManager.PriorityState.NormalTurn);

        // Now resolve attacks immediately
        ResolvePreparedAttacks(BattleManager.Instance.player);

        HasAttack = false;
    }

    private IEnumerator AIAutoBlock()
    {
        // Only auto block if AI has priority in a blocker response phase
        yield return new WaitForSeconds(0.5f);

        var attacker = BattleManager.Instance.turnPlayer;

        for (int i = 0; i < attacker.activeSlots.Length; i++)
        {
            var attackingCard = attacker.activeSlots[i];
            if (attackingCard == null) continue;

            var potentialBlocker = fieldSlots[i];
            if (potentialBlocker != null)
            {
                MoveCardToActiveSlot(potentialBlocker, i);
                if (!preparedBlocks.Contains(potentialBlocker))
                    preparedBlocks.Add(potentialBlocker);

                yield return new WaitForSeconds(0.25f);
            }
        }

        // After auto-blocking, pass priority
        BattleManager.Instance.HandlePass(this);
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
        // legacy event: if you're still using OnResponseWindow event, let it trigger AI handling
        // but prefer the priority system paths above.
        StartCoroutine(HandleResponseWindow(actor, playedCard));
    }

    private IEnumerator HandleResponseWindow(BaseController actor, CardRuntime playedCard)
    {
        if (actor == this) yield break;
        if (hasRespondedThisPriority) yield break;
        hasRespondedThisPriority = true;

        if (BattleManager.Instance.priorityOwner == this)
        {
            // If AI has priority, maybe block
            yield return StartCoroutine(AIAutoBlock());
        }
        else
        {
            // If AI can act (has playable), play something quick
            var playable = hand.FirstOrDefault(c => hero.CanAfford(c.cardData));
            if (playable != null)
            {
                yield return new WaitForSeconds(0.5f);
                TryPlayCard(playable);
            }
        }

        hasRespondedThisPriority = false;
    }
}

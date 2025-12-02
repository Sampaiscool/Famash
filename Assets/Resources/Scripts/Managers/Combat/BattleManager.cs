using System.Collections;
using System.Linq;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public PlayerController player;
    public OpponentController opponent;

    public BaseController turnPlayer;
    public BaseController otherPlayer;

    public bool firstTurn = true;

    // ---------------------------
    // New Runeterra priority state
    // ---------------------------
    public enum PriorityState
    {
        NormalTurn,     // Attacker can attack OR act
        PriorityWindow  // Both play responses only
    }

    public PriorityState priorityState = PriorityState.NormalTurn;
    public BaseController priorityOwner; // Who currently holds priority
    public bool lastPassFromTurnPlayer = false;

    void Awake() => Instance = this;

    // ---------------------------
    // Setup phase
    // ---------------------------
    public void SetupBattle()
    {
        turnPlayer = player;
        otherPlayer = opponent;

        player.DrawCards(5);
        opponent.DrawCards(5);

        for (int i = 0; i < player.fieldSlots.Length; i++)
            player.fieldSlots[i] = null;

        for (int i = 0; i < opponent.fieldSlots.Length; i++)
            opponent.fieldSlots[i] = null;

        for (int i = 0; i < player.activeSlots.Length; i++)
            player.activeSlots[i] = null;

        for (int i = 0; i < opponent.activeSlots.Length; i++)
            opponent.activeSlots[i] = null;

        // Turn player starts with priority
        priorityOwner = turnPlayer;

        StartCoroutine(GameLoop());
    }


    // ---------------------------
    // Basic Turn Loop
    // ---------------------------
    IEnumerator GameLoop()
    {
        while (true)
        {
            BattleUIManager.Instance.UpdateTurnHighlight(turnPlayer);

            yield return StartCoroutine(TakeTurn(turnPlayer));

            var tmp = turnPlayer;
            turnPlayer = otherPlayer;
            otherPlayer = tmp;

            firstTurn = false;
        }
    }

    IEnumerator TakeTurn(BaseController active)
    {
        if (!firstTurn)
            active.DrawCards(1);

        StartTurn(active);
        yield return new WaitUntil(() => active.IsTurnDone);
        EndTurn(active);
    }

    void StartTurn(BaseController controller)
    {
        controller.hero.StartTurn();

        player.hero.StartTurn();
        opponent.hero.StartTurn();

        controller.StartTurn(!firstTurn);

        priorityState = PriorityState.NormalTurn;
        priorityOwner = controller;
        lastPassFromTurnPlayer = false;

        BattleUIManager.Instance.UpdateHeroUI();
        BattleUIManager.Instance.SetEndTurnButtonActive(controller.isPlayer);
    }

    void EndTurn(BaseController controller)
    {
        controller.EndTurn();

        player.hero.EndTurnGainMana();
        opponent.hero.EndTurnGainMana();

        BattleUIManager.Instance.UpdateHeroUI();
        BattleUIManager.Instance.SetEndTurnButtonActive(false);
    }

    // ---------------------------
    // PASSING & PRIORITY
    // ---------------------------
    public void OnEndTurnButtonClicked()
    {
        HandlePass(player);
    }

    public void HandlePass(BaseController passer)
    {
        // Can only pass if you have priority
        if (passer != priorityOwner)
            return;

        bool passerIsTurnPlayer = (passer == turnPlayer);

        // NormalTurn pass by turnPlayer = end turn
        if (priorityState == PriorityState.NormalTurn && passerIsTurnPlayer)
        {
            passer.IsTurnDone = true;
            return;
        }

        // We are in PriorityWindow (responding)
        if (lastPassFromTurnPlayer != passerIsTurnPlayer)
        {
            // Not consecutive → give priority to other
            lastPassFromTurnPlayer = passerIsTurnPlayer;
            BaseController next = (passer == player) ? opponent : player;
            GivePriorityTo(next);
            return;
        }

        // ---------------------------
        // Two passes in a row!
        // ---------------------------
        if (SpellStackManager.Instance.HasPendingStack)
        {
            SpellStackManager.Instance.ResolveStack();
        }

        // Return to normal turn control
        priorityState = PriorityState.NormalTurn;
        priorityOwner = turnPlayer;
        lastPassFromTurnPlayer = false;

        BattleUIManager.Instance.UpdateTurnHighlight(turnPlayer);
        BattleUIManager.Instance.SetEndTurnButtonLabel("End Turn");
        BattleUIManager.Instance.SetEndTurnButtonActive(turnPlayer.isPlayer);
    }

    public void GivePriorityTo(BaseController next)
    {
        priorityOwner = next;
        BattleUIManager.Instance.UpdateTurnHighlight(next);

        if (next.isPlayer)
        {
            BattleUIManager.Instance.SetEndTurnButtonLabel("Pass");
            BattleUIManager.Instance.SetEndTurnButtonActive(true);
        }
        else
        {
            // AI now has priority -> start its response coroutine
            StartPriorityResponseIfAI(next);
        }
    }


    // ---------------------------
    // Called whenever a spell/summon/ability is added to the stack
    // ---------------------------
    public void NotifyActionTaken(BaseController actor)
    {
        priorityState = PriorityState.PriorityWindow;

        BaseController next = (actor == player) ? opponent : player;
        GivePriorityTo(next);

        lastPassFromTurnPlayer = false;
    }


    // ---------------------
    // Attacking & Blocking 
    // ---------------------
    public void PrepareAttack(CardRuntime card)
    {
        if (priorityState == PriorityState.PriorityWindow)
            return; // Cannot attack inside the response cycle

        if (turnPlayer == player)
            player.PrepareAttack(card);
        else
            opponent.PrepareAttack(card);

        if (player.preparedAttacks.Count > 0 || opponent.preparedAttacks.Count > 0)
            BattleUIManager.Instance.ShowConfirmAttackButton();
    }

    public void ConfirmAttack()
    {
        var attacker = turnPlayer;
        var defender = otherPlayer;

        if (attacker.preparedAttacks.Count == 0)
            return;

        BattleUIManager.Instance.HideConfirmAttackButton();

        // Enter priority window for blocker/responses
        priorityState = PriorityState.PriorityWindow;
        GivePriorityTo(defender);

        StartCoroutine(ResolveBattleAfterResponses(attacker, defender));
    }
    public bool IsPlayerAllowedToAct(BaseController actor)
    {
        // Cannot act if the stack is currently resolving
        if (SpellStackManager.Instance.stackIsResolving)
            return false;

        // Normal turn: actor has priority if they are the current priority owner
        if (priorityOwner == actor)
            return true;

        // Response: if stack exists, only the actor with priority may respond
        if (SpellStackManager.Instance.HasPendingStack && priorityOwner == actor)
            return true;

        return false;
    }


    IEnumerator ResolveBattleAfterResponses(BaseController attacker, BaseController defender)
    {
        // Wait until priorityWindow ends -> two passes
        yield return new WaitUntil(() =>
            priorityState == PriorityState.NormalTurn
        );

        attacker.ResolvePreparedAttacks(defender);
    }

    public void ConfirmBlockers()
    {
        // Called by the player during priority window
        // They designate blockers, then pass to continue

        HandlePass(priorityOwner);
    }


    // ---------------------------
    // AI Response
    // ---------------------------
    public void StartPriorityResponseIfAI(BaseController controller)
    {
        if (!controller.isPlayer)
            StartCoroutine(AIResponseLoop(controller));
    }

    private IEnumerator AIResponseLoop(BaseController ai)
    {
        yield return new WaitForSeconds(0.5f);

        if (BattleManager.Instance.priorityOwner != ai)
            yield break;

        // If stack exists, AI responds to stack
        if (SpellStackManager.Instance.HasPendingStack)
        {
            Debug.Log($"{ai.controllerName} passes on stack.");
            BattleManager.Instance.HandlePass(ai);
            yield break;
        }

        // Otherwise, play a summon if possible
        var summonable = ai.hand.FirstOrDefault(c => c.cardData.cardType == CardType.Unit && ai.hero.CanAfford(c.cardData));
        if (summonable != null)
            ai.TryPlayCard(summonable, openResponseWindow: false);

        yield return new WaitForSeconds(0.3f);

        BattleManager.Instance.HandlePass(ai);
    }

}

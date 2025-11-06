using System.Collections;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public PlayerController player;
    public OpponentController opponent;

    public BaseController turnPlayer;
    public BaseController otherPlayer;

    public bool firstTurn = true;

    private bool waitingForResponse = false;
    public BaseController currentResponder;

    void Awake() => Instance = this;

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

        StartCoroutine(GameLoop());
    }

    IEnumerator GameLoop()
    {
        while (true)
        {
            // Highlight whose turn it is
            BattleUIManager.Instance.UpdateTurnHighlight(turnPlayer);

            yield return StartCoroutine(TakeTurn(turnPlayer));

            var temp = turnPlayer;
            turnPlayer = otherPlayer;
            otherPlayer = temp;

            firstTurn = false;
        }
    }

    IEnumerator TakeTurn(BaseController active)
    {
        if (!firstTurn)
            turnPlayer.DrawCards(1);

        StartTurn(active);
        yield return new WaitUntil(() => active.IsTurnDone);
        EndTurn(active);
    }

    void StartTurn(BaseController controller)
    {
        bool isTurnPlayer = (controller == turnPlayer);

        // Both players refill every turn
        player.hero.StartTurn();
        opponent.hero.StartTurn();

        controller.StartTurn(!firstTurn);
        BattleUIManager.Instance.SetEndTurnButtonActive(controller.isPlayer);
        BattleUIManager.Instance.UpdateTurnHighlight(controller);
        BattleUIManager.Instance.UpdateHeroUI();
    }


    void EndTurn(BaseController controller)
    {
        // Current turn ends
        controller.EndTurn();

        // Both heroes gain 1 max mana and refill — Runeterra style
        player.hero.EndTurnGainMana();
        opponent.hero.EndTurnGainMana();

        // Update UI after mana change
        BattleUIManager.Instance.UpdateHeroUI();

        // Hide the End Turn button (until next turn starts)
        BattleUIManager.Instance.SetEndTurnButtonActive(false);
    }


    // Player clicks End Turn button
    public void OnEndTurnButtonClicked()
    {
        // 1️ Cancel placement mode
        if (player.IsPlacingCard)
        {
            player.CancelPlacementMode();
            return;
        }

        // 2️ End current response window
        if (waitingForResponse && currentResponder == player)
        {
            EndResponseWindow();
            return;
        }

        // 3️ End turn normally
        if (turnPlayer == player)
        {
            if (player.preparedAttacks.Count > 0 || opponent.preparedAttacks.Count > 0)
            {
                // If attacks are prepared, confirm the attack instead of ending the turn
                ConfirmAttack();
            }
            else
            {
                player.IsTurnDone = true;  // End turn if no attacks prepared
            }
        }
    }
    public void PrepareAttack(CardRuntime card)
    {
        if (turnPlayer == player)
        {
            player.PrepareAttack(card);  // Prepare attack for the player
        }
        else
        {
            opponent.PrepareAttack(card);  // Prepare attack for the opponent
        }

        // Show the Confirm Attack button if attacks are prepared
        if (player.preparedAttacks.Count > 0 || opponent.preparedAttacks.Count > 0)
        {
            BattleUIManager.Instance.ShowConfirmAttackButton();
        }
    }

    public void ConfirmAttack()
    {
        BaseController attacker = turnPlayer;
        BaseController defender = otherPlayer;

        if (attacker == null || defender == null) return;

        if (attacker.preparedAttacks.Count == 0)
        {
            Debug.Log("No attacks prepared!");
            return;
        }

        // Hide confirm attack button during response
        BattleUIManager.Instance.HideConfirmAttackButton();

        // Open the response window for the defender
        StartResponseWindow(defender);

        // After response window ends, continue attack resolution
        StartCoroutine(ResolveBattleAfterResponse(attacker, defender));
    }
    public void StartResponseWindow(BaseController responder)
    {
        currentResponder = responder;
        waitingForResponse = true;

        // Update highlight — show who’s acting now
        BattleUIManager.Instance.UpdateTurnHighlight(responder);

        if (responder.isPlayer)
        {
            BattleUIManager.Instance.SetEndTurnButtonActive(true);
            BattleUIManager.Instance.SetEndTurnButtonLabel("Pass");
        }
        else
        {
            StartCoroutine(AIResponseLoop(responder));
        }
    }

    public void EndResponseWindow()
    {
        waitingForResponse = false;
        currentResponder = null;

        // Return highlight to the current turn player
        BattleUIManager.Instance.UpdateTurnHighlight(turnPlayer);

        BattleUIManager.Instance.SetEndTurnButtonLabel("End Turn");
        BattleUIManager.Instance.SetEndTurnButtonActive(turnPlayer.isPlayer);
    }

    public bool IsPlayerAllowedToAct(PlayerController pc)
    {
        return (turnPlayer == pc && !waitingForResponse) ||
               (waitingForResponse && currentResponder == pc);
    }
    private IEnumerator ResolveBattleAfterResponse(BaseController attacker, BaseController defender)
    {
        // Wait until response window closes (defender passes)
        yield return new WaitUntil(() => !waitingForResponse);

        // Now resolve the battle
        attacker.ResolvePreparedAttacks(defender);
    }

    private IEnumerator AIResponseLoop(BaseController ai)
    {
        yield return new WaitForSeconds(0.5f);

        // Find a playable card
        CardRuntime cardToPlay = null;
        foreach (var c in ai.hand)
        {
            if (ai.hero.CanAfford(c.cardData))
            {
                cardToPlay = c;
                break;
            }
        }

        if (cardToPlay != null)
        {
            ai.TryPlayCard(cardToPlay);
            yield return new WaitForSeconds(0.5f);

            // After AI acts, give player a response
            StartResponseWindow(player);
        }
        else
        {
            EndResponseWindow(); // nothing to play, pass back
        }
    }
}

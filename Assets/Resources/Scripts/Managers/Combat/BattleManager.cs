using System.Collections;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    public PlayerController player;
    public OpponentController opponent;

    private BaseController turnPlayer;
    private BaseController otherPlayer;
    private bool firstTurn = true;

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
        controller.StartTurn(!firstTurn);
        BattleUIManager.Instance.SetEndTurnButtonActive(controller.isPlayer);

        // Refresh highlight (important if last response was different)
        BattleUIManager.Instance.UpdateTurnHighlight(controller);
    }

    void EndTurn(BaseController controller)
    {
        controller.EndTurn();
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
            player.IsTurnDone = true;
            return;
        }
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

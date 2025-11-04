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

    void Awake() => Instance = this;

    void Start()
    {
        Instance = this;
    }

    public void SetupBattle()
    {
        turnPlayer = player;
        otherPlayer = opponent;

        // Initial draw 5 cards each
        for (int i = 0; i < 5; i++)
        {
            player.DrawCard();
            opponent.DrawCard();
        }

        //Setup Field
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
            StartTurn(turnPlayer);

            // Wait until turn player ends turn
            yield return new WaitUntil(() => turnPlayer.IsTurnDone);

            EndTurn(turnPlayer);

            // Swap turn player
            var temp = turnPlayer;
            turnPlayer = otherPlayer;
            otherPlayer = temp;

            firstTurn = false;
        }
    }

    void StartTurn(BaseController playerController)
    {
        playerController.IsTurnDone = false;
        playerController.HasAttack = true;
        playerController.HasPerformedAction = false;
        playerController.CanRespond = false;

        if (!firstTurn)
            playerController.DrawCard();

        playerController.StartTurn();

        // Enable end-turn button only for local player
        BattleUIManager.Instance.SetEndTurnButtonActive(playerController.isPlayer);
    }

    void EndTurn(BaseController playerController)
    {
        playerController.IsTurnDone = true;
        playerController.HasAttack = false;
        playerController.HasPerformedAction = false;
        playerController.CanRespond = false;

        BattleUIManager.Instance.SetEndTurnButtonActive(false);

        playerController.EndTurn();
    }

    // Called by end-turn button
    public void OnEndTurnButtonClicked()
    {
        if (turnPlayer.isPlayer)
            turnPlayer.IsTurnDone = true;
    }
}

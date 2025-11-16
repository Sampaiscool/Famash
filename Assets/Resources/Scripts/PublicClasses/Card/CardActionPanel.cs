using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Collections.AllocatorManager;

public class CardActionPanel : MonoBehaviour
{
    public RectTransform contentParent; // ScrollView content
    public GameObject actionButtonPrefab; // prefab with Button + Text

    private CardRuntime currentCard;
    private BattleUIManager uiManager;

    private void Start()
    {
        uiManager = BattleUIManager.Instance;
    }

    public void Setup(CardRuntime card)
    {
        currentCard = card;

        // Clear old buttons
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        bool isTurnPlayer = BattleManager.Instance.currentResponder == null && BattleManager.Instance.turnPlayer.HasAttack;
        bool canAttack = BattleManager.Instance.turnPlayer.HasAttack;
        bool isResponsePhase = BattleManager.Instance.currentResponder != null &&
                       BattleManager.Instance.currentResponder == BattleManager.Instance.otherPlayer;

        // Allow preparing attack only if the card is in play and it's the turn player's turn
        if (card.isInPlay && isTurnPlayer && canAttack)
            AddAction("Prepare Attack", OnPrepareAttack);

        // Add active effects
        for (int i = 0; i < card.activeEffects.Count; i++)
        {
            int index = i; // capture for closure
            AddAction($"Activate {i + 1}", () =>
            {
                OnActivate(index);
            });
        }

        if (isResponsePhase && card.isInPlay && BattleManager.Instance.turnPlayer.preparedAttacks.Count > 0)
            AddAction("Block", OnBlock);
    }

    private void AddAction(string label, UnityEngine.Events.UnityAction callback)
    {
        if (actionButtonPrefab == null)
        {
            Debug.LogError("CardActionPanel: actionButtonPrefab is not assigned!");
            return;
        }

        if (contentParent == null)
        {
            Debug.LogError("CardActionPanel: contentParent is not assigned!");
            return;
        }

        GameObject btnObj = Instantiate(actionButtonPrefab, contentParent);
        var btn = btnObj.GetComponent<Button>();
        var txt = btnObj.GetComponentInChildren<TMP_Text>();

        if (btn == null)
        {
            Debug.LogError("CardActionPanel: Button component missing on prefab!");
            return;
        }
        if (txt == null)
        {
            Debug.LogError("CardActionPanel: Text component missing on prefab!");
            return;
        }

        if (txt != null) txt.text = label;
        btn.onClick.AddListener(callback);
    }

    void OnPrepareAttack()
    {
        if (currentCard != null)
        {
            // First, check if the card is on the field
            if (currentCard.location == CardLocation.Field)
            {
                // Find the correct slot for the card in the active slots
                int slotIndex = currentCard.slotIndex;

                if (slotIndex != -1)
                {
                    // Move the card from the field to the active slot
                    BattleManager.Instance.turnPlayer.MoveCardToActiveSlot(currentCard, slotIndex);

                    // Now prepare for attack
                    BattleManager.Instance.PrepareAttack(currentCard);
                    Debug.Log($"{currentCard.cardData.cardName} prepared for attack and placed in active slot {slotIndex}");
                }
                else
                {
                    Debug.Log("No available active slots for the card.");
                }
            }
            else
            {
                Debug.Log("Card is not in play on the field.");
            }
        }
        Destroy(gameObject);
    }


    void OnActivate(int effectIndex)
    {
        if (currentCard != null)
        {
            //currentCard.activeEffects[effectIndex]?.OnPlay(currentCard);
            Debug.Log($"{currentCard.cardData.cardName} activated effect {effectIndex + 1}");
        }
        Destroy(gameObject);
    }
    void OnBlock()
    {
        if (currentCard == null) return;

        int slotIndex = currentCard.slotIndex;

        BaseController attacker = BattleManager.Instance.turnPlayer;
        BaseController defender = BattleManager.Instance.currentResponder;

        if (attacker.activeSlots == null || slotIndex < 0 || slotIndex >= attacker.activeSlots.Length)
        {
            Debug.Log("Invalid lane index for block.");
            Destroy(gameObject);
            return;
        }

        CardRuntime attackingCard = attacker.activeSlots[slotIndex];
        if (attackingCard == null)
        {
            Debug.Log("No attacker in this lane to block!");
            Destroy(gameObject);
            return;
        }

        // Move defender into active slot
        defender.MoveCardToActiveSlot(currentCard, slotIndex);

        // Track that this card is blocking
        if (!defender.preparedBlocks.Contains(currentCard))
            defender.preparedBlocks.Add(currentCard);

        // Change the End Turn button to say "Confirm Blockers"
        BattleUIManager.Instance.SetEndTurnButtonLabel("Confirm Blockers");

        Debug.Log($"{defender.controllerName}'s {currentCard.cardData.cardName} is blocking {attacker.controllerName}'s {attackingCard.cardData.cardName} in lane {slotIndex}.");

        Destroy(gameObject);
    }
}

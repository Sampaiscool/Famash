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

        Debug.Log($"Player has attacK? {canAttack}");

        // Allow preparing attack only if the card is in play and it's the turn player's turn
        if (card.isInPlay && isTurnPlayer && canAttack)
            AddAction("Prepare Attack", OnPrepareAttack);

        // Add active effects
        for (int i = 0; i < card.activeEffects.Count; i++)
        {
            int index = i; // capture for closure
            AddAction($"Activate Effect {i + 1}: {card.activeEffects[i].effectName}", () =>
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
            // Activate the effect based on index
            currentCard.activeEffects[effectIndex]?.OnPlay(currentCard);
            Debug.Log($"{currentCard.cardData.cardName} activated effect {effectIndex + 1}");
        }
        Destroy(gameObject);
    }
    void OnBlock()
    {
        if (currentCard == null) return;

        int slotIndex = currentCard.slotIndex;

        // Only block if there’s an enemy attacker in that same lane
        if (BattleManager.Instance.turnPlayer.activeSlots[slotIndex] != null)
        {
            // Move this card to the active slot to block
            BattleManager.Instance.currentResponder.MoveCardToActiveSlot(currentCard, slotIndex);

            Debug.Log($"{currentCard.cardData.cardName} blocks in slot {slotIndex}");
        }
        else
        {
            Debug.Log("No attacker in this lane to block!");
        }

        Destroy(gameObject);
    }


    // New method to handle placing the card in the active slot
    void OnPlaceInActiveSlot(CardRuntime card)
    {
        // Ensure the card is not already in play and can be placed
        if (card != null && !card.isInPlay)
        {
            int availableSlot = -1;

            // Find the first available active slot (depending on whether it's player or opponent)
            for (int i = 0; i < BattleManager.Instance.turnPlayer.activeSlots.Length; i++)
            {
                if (BattleManager.Instance.turnPlayer.activeSlots[i] == null)  // Slot is available
                {
                    availableSlot = i;
                    break;
                }
            }

            if (availableSlot != -1)
            {
                // Now place the card into the selected active slot
                BattleManager.Instance.turnPlayer.MoveCardToActiveSlot(card, availableSlot);
                Debug.Log($"{card.cardData.cardName} placed in active slot {availableSlot}");
            }
            else
            {
                Debug.Log("No available active slots to place the card.");
            }
        }
    }

}

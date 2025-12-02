using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardActionPanel : MonoBehaviour
{
    public RectTransform contentParent;
    public GameObject actionButtonPrefab;

    private CardRuntime currentCard;
    private BattleUIManager uiManager;

    private void Start()
    {
        uiManager = BattleUIManager.Instance;
    }

    public void Setup(CardRuntime card)
    {
        currentCard = card;

        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        var bm = BattleManager.Instance;
        bool hasPriority = bm.priorityOwner == bm.turnPlayer || !SpellStackManager.Instance.HasPendingStack;
        bool isResponsePhase = SpellStackManager.Instance.HasPendingStack && bm.priorityOwner != bm.turnPlayer;
        bool canAttack = bm.turnPlayer.HasAttack && hasPriority;

        if (card.isInPlay && canAttack)
            AddAction("Prepare Attack", OnPrepareAttack);

        for (int i = 0; i < card.runtimeEffects.Count; i++)
        {
            var re = card.runtimeEffects[i];
            if (re.trigger != CardTrigger.OnActivate) continue;
            if (re.isOncePerTurn && re.hasBeenUsedThisTurn) continue;
            re.effectOwner = card.owner;
            if (re.needsTarget && !HasValidTargets(re)) continue;

            int idx = i;
            AddAction("Activate Effect", () => OnActivate(idx));
        }

        if (card.isInPlay && isResponsePhase && bm.turnPlayer.preparedAttacks.Count > 0)
            AddAction("Block", OnBlock);
    }

    private void AddAction(string label, UnityEngine.Events.UnityAction callback)
    {
        GameObject btnObj = Instantiate(actionButtonPrefab, contentParent);
        var btn = btnObj.GetComponent<Button>();
        var txt = btnObj.GetComponentInChildren<TMP_Text>();
        txt.text = label;
        btn.onClick.AddListener(callback);
    }

    private void OnPrepareAttack()
    {
        if (currentCard == null || currentCard.location != CardLocation.Field) { Destroy(gameObject); return; }

        int slotIndex = currentCard.slotIndex;
        if (slotIndex != -1)
        {
            BattleManager.Instance.turnPlayer.MoveCardToActiveSlot(currentCard, slotIndex);
            BattleManager.Instance.PrepareAttack(currentCard);
        }

        Destroy(gameObject);
    }

    private void OnActivate(int effectIndex)
    {
        var effect = currentCard.runtimeEffects[effectIndex];
        effect.effectOwner = currentCard.owner;

        // Add effect to stack instead of directly applying
        SpellStackManager.Instance.AddToStack(currentCard, effect);

        Destroy(gameObject);
    }

    private void OnBlock()
    {
        if (currentCard == null) return;

        int slotIndex = currentCard.slotIndex;
        var attacker = BattleManager.Instance.turnPlayer;
        var defender = BattleManager.Instance.otherPlayer;

        if (slotIndex < 0 || slotIndex >= attacker.activeSlots.Length) { Destroy(gameObject); return; }

        CardRuntime attackingCard = attacker.activeSlots[slotIndex];
        if (attackingCard == null) { Destroy(gameObject); return; }

        defender.MoveCardToActiveSlot(currentCard, slotIndex);
        if (!defender.preparedBlocks.Contains(currentCard))
            defender.preparedBlocks.Add(currentCard);

        BattleUIManager.Instance.SetEndTurnButtonLabel("Confirm Blockers");
        Destroy(gameObject);
    }

    private bool HasValidTargets(RuntimeEffect effect)
    {
        foreach (var cardUI in FindObjectsByType<CardInGame>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (cardUI.runtimeCard != null && effect.IsValidTarget(cardUI.runtimeCard))
                return true;
        return false;
    }
}

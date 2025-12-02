using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    public System.Action<int> OnFieldSlotClicked;
    public System.Action<CardRuntime> OnCardClickedTargetMode;


    [Header("Player UI")]
    public TMP_Text playerHealthText;
    public TMP_Text playerManaText;
    public Transform playerHandParent;
    public GameObject endTurnButton;

    [Header("Opponent UI")]
    public TMP_Text opponentHealthText;
    public TMP_Text opponentManaText;
    public Transform opponentHandParent;

    [Header("Field Slots")]
    public Transform[] playerFieldSlots = new Transform[5];
    public Transform[] opponentFieldSlots = new Transform[5];
    public Transform[] playerActiveSlots = new Transform[5];
    public Transform[] opponentActiveSlots = new Transform[5];

    [Header("Stack UI")]
    public GameObject stackPanel;
    public Transform stackContent;

    [Header("Turn Panels")]
    public Image playerTurnPanel;
    public Image opponentTurnPanel;

    [Header("Turn Panel Colors")]
    public Color activeTurnColor = Color.white;
    public Color inactiveTurnColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Turn Highlight Settings")]
    public float fadeDuration = 0.3f; // seconds
    private Coroutine panelFadeRoutine;

    [Header("Prefabs")]
    public GameObject cardPrefab;
    public GameObject stackEntryPrefab;

    [Header("Graveyard UI")]
    public GameObject graveyardPanelPrefab;


    private PlayerController player;
    private OpponentController opponent;

    void Awake() => Instance = this;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        opponent = FindFirstObjectByType<OpponentController>();

        if (player == null || opponent == null)
        {
            Debug.LogError("Player or Opponent not found in scene!");
            return;
        }

        // Load decks from BattleLoader
        if (BattleLoader.Instance != null)
        {
            player.LoadDeck(BattleLoader.Instance.playerDeckRuntime);
            opponent.LoadDeck(BattleLoader.Instance.enemyDeckRuntime);
        }
        else
        {
            Debug.LogWarning("BattleLoader missing, loading fallback decks...");
        }

        // Assign UI parents
        player.handParent = playerHandParent;
        opponent.handParent = opponentHandParent;

        player.isPlayer = true;

        // Initial UI setup
        UpdateHeroUI();

        BattleManager.Instance.SetupBattle();
    }

    public void UpdateHeroUI()
    {
        playerHealthText.text = $"HP: {player.hero.currentHealth}";
        playerManaText.text = $"Mana: {player.hero.currentMana}/{player.hero.maxMana}";

        opponentHealthText.text = $"HP: {opponent.hero.currentHealth}";
        opponentManaText.text = $"Mana: {opponent.hero.currentMana}/{opponent.hero.maxMana}";
    }

    public GameObject SpawnCardUI(BaseController owner, CardRuntime card, Transform parent)
    {
        GameObject cardObj = Instantiate(cardPrefab, parent);
        var ui = cardObj.GetComponent<CardInGame>();
        if (ui != null)
        {
            ui.Bind(card, owner);
            card.cardUI = cardObj;
        }
        else
            Debug.LogError("Card prefab missing CardInGame component!");

        return cardObj;
    }

    public void RefreshHandUI(BaseController controller)
    {
        foreach (Transform child in controller.handParent)
            Destroy(child.gameObject);

        foreach (var card in controller.hand)
            SpawnCardUI(controller, card, controller.handParent);
    }

    public void UpdateTurnHighlight(BaseController currentTurn)
    {
        // Reset both first
        playerHealthText.color = Color.gray;
        playerManaText.color = Color.gray;
        opponentHealthText.color = Color.gray;
        opponentManaText.color = Color.gray;

        // Highlight current turn
        if (currentTurn == player)
        {
            playerHealthText.color = Color.white;
            playerManaText.color = Color.white;
            SetEndTurnButtonLabel("End Turn");
        }
        else if (currentTurn == opponent)
        {
            opponentHealthText.color = Color.white;
            opponentManaText.color = Color.white;
            SetEndTurnButtonLabel("Waiting...");
        }
    }

    public void HighlightTargets(RuntimeEffect effect)
    {
        var allCards = FindObjectsByType<CardInGame>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var cardUI in allCards)
        {
            if (cardUI.runtimeCard == null)
                continue;

            if (effect.IsValidTarget(cardUI.runtimeCard))
                cardUI.artwork.color = Color.yellow;
            else
                cardUI.artwork.color = Color.gray;
        }
    }

    public void ClearTargetHighlights()
    {
        var allCards = FindObjectsByType<CardInGame>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (var cardUI in allCards)
        {
            if (cardUI == null) continue;

            cardUI.artwork.color = Color.white;
        }
    }


    public void HighlightAvailableSlots(BaseController owner)
    {
        Transform[] slots = owner.isPlayer ? playerFieldSlots : opponentFieldSlots;

        for (int i = 0; i < slots.Length; i++)
        {
            var img = slots[i].GetComponent<Image>();
            img.color = Color.white; // reset first

            if (owner.fieldSlots[i] == null)
                img.color = Color.green; // available
            else
                img.color = Color.red; // occupied
        }
    }
    // In BattleUIManager
    public void StartTargetSelection(CardRuntime activator, RuntimeEffect effect, System.Action<CardRuntime> callback)
    {
        Debug.Log("Targeting mode started.");

        HighlightTargets(effect);

        // Assign a click handler
        OnCardClickedTargetMode = (CardRuntime clicked) =>
        {
            if (!effect.IsValidTarget(clicked))
                return;

            ClearTargetHighlights();
            OnCardClickedTargetMode = null;

            callback(clicked);
        };
    }


    public bool IsValidTarget(EffectInstance inst, CardRuntime target)
    {
        BaseController owner = inst.effectOwner;
        if (owner == null)
        {
            Debug.LogWarning("Effect owner not set on effect instance!");
            return false;
        }

        // 1. Card type
        if (inst.targetType != CardType.None &&
            target.cardData.cardType != inst.targetType)
            return false;

        // 2. Ownership
        if (inst.targetEnemy && target.owner == owner)
            return false; // enemy = not your own

        if (inst.targetAlly && target.owner != owner)
            return false; // ally = must be your own

        // 3. Location — allow if allowedLocation is None, otherwise match
        if (!inst.allowedLocations.Contains(target.location))
            return false;


        return true;
    }
    public void UpdateStackUI(IEnumerable<StackEntry> stack)
    {
        stackPanel.SetActive(true);

        // Clear old entries
        foreach (Transform child in stackContent)
            Destroy(child.gameObject);

        // Add new entries, top at bottom
        foreach (var entry in stack.Reverse())
        {
            GameObject obj = Instantiate(stackEntryPrefab, stackContent);

            var ui = obj.GetComponent<StackEntryUI>();
            if (ui != null)
                ui.Bind(entry);
            else
                Debug.LogWarning("StackEntryPrefab is missing StackEntryUI component!");
        }
    }
    public void HideStackUI()
    {
        stackPanel.SetActive(false);
    }


    public void UpdateActiveSlotsUI()
    {
        for (int i = 0; i < BattleManager.Instance.turnPlayer.activeSlots.Length; i++)
        {
            var card = BattleManager.Instance.turnPlayer.activeSlots[i];
            if (card != null && card.cardUI != null)
            {
                // Update the UI representation of the active slot with the card
                Transform slotTransform = GetAvailableFieldSlots(BattleManager.Instance.turnPlayer)[i];
                card.cardUI.transform.SetParent(slotTransform, false);
                card.cardUI.transform.localPosition = Vector3.zero;
            }
        }
    }

    public void OnSlotClicked(int index)
    {
        OnFieldSlotClicked?.Invoke(index);
    }

    public void ClearSlotHighlights()
    {
        foreach (var slot in playerFieldSlots)
            slot.GetComponent<Image>().color = Color.white;
        foreach (var slot in opponentFieldSlots)
            slot.GetComponent<Image>().color = Color.white;
    }

    public Transform[] GetAvailableFieldSlots(BaseController owner)
    {
        return owner.isPlayer ? playerFieldSlots : opponentFieldSlots;
    }
    public Transform[] GetAvailableActiveSlots(BaseController owner)
    {
        return owner.isPlayer ? playerActiveSlots : opponentActiveSlots;
    }

    public void SetEndTurnButtonActive(bool active)
    {
        if (endTurnButton != null)
            endTurnButton.SetActive(active);
    }
    public void ShowConfirmAttackButton()
    {
        SetEndTurnButtonLabel("Confirm Attack");
        SetEndTurnButtonActive(true);
    }
    public void HideConfirmAttackButton()
    {
        SetEndTurnButtonActive(false);  // Hides the button after confirming the attack
    }

    // Update the button text and functionality
    public void SetEndTurnButtonLabel(string text)
    {
        var tmp = endTurnButton.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = text;
        }
    }
    public void OnGraveyardButtonClicked(BaseController owner)
    {
        // Try to find an existing panel
        GraveyardPanel existing = FindFirstObjectByType<GraveyardPanel>();

        if (existing != null)
        {
            // If it exists, destroy it (close)
            Destroy(existing.gameObject);
        }
        else
        {
            // Otherwise, open a new one
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found for GraveyardPanel!");
                return;
            }

            GraveyardPanel panel = Instantiate(graveyardPanelPrefab, canvas.transform)
                .GetComponent<GraveyardPanel>();

            panel.Setup(owner);
        }
    }

}

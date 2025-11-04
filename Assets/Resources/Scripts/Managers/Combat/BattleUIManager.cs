using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    public static BattleUIManager Instance { get; private set; }

    public System.Action<int> OnFieldSlotClicked;

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

    [Header("Prefabs")]
    public GameObject cardPrefab;

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
        playerManaText.text = $"Mana: {player.hero.currentMana}/{player.hero.heroData.resourceMax}";

        opponentHealthText.text = $"HP: {opponent.hero.currentHealth}";
        opponentManaText.text = $"Mana: {opponent.hero.currentMana}/{opponent.hero.heroData.resourceMax}";
    }

    public void SpawnCardUI(BaseController owner, CardRuntime card, Transform parent)
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
    }

    public void RefreshHandUI(BaseController controller)
    {
        foreach (Transform child in controller.handParent)
            Destroy(child.gameObject);

        foreach (var card in controller.hand)
            SpawnCardUI(controller, card, controller.handParent);
    }



    public void HighlightAvailableSlots(BaseController owner)
    {
        Transform[] slots = owner.isPlayer ? playerFieldSlots : opponentFieldSlots;

        for (int i = 0; i < slots.Length; i++)
        {
            Debug.Log($"Slot {i} is {(owner.fieldSlots[i] == null ? "empty" : "occupied")}");

            var img = slots[i].GetComponent<Image>();
            img.color = Color.white; // reset first

            if (owner.fieldSlots[i] == null)
                img.color = Color.green; // available
            else
                img.color = Color.red; // occupied
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

    public void SetEndTurnButtonActive(bool active)
    {
        if (endTurnButton != null)
            endTurnButton.SetActive(active);
    }
}

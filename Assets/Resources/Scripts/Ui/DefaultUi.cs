using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DefaultUi : MonoBehaviour
{
    public Image deckImage;
    public TMP_Text deckNameText;
    public Button debugBattleButton;

    private DeckData selectedDeck;

    void Start()
    {
        LoadSelectedDeck();
        debugBattleButton.onClick.AddListener(OnDebugBattleClicked);
    }

    public void LoadSelectedDeck()
    {
        if (!PlayerPrefs.HasKey("SelectedDeckId"))
        {
            Debug.Log("No selected deck saved in PlayerPrefs.");
            return;
        }

        string deckId = PlayerPrefs.GetString("SelectedDeckId");
        DeckData deck = DeckManager.Instance.GetDeck(deckId);

        if (deck == null)
        {
            Debug.LogWarning($"No deck found with ID {deckId}");
            return;
        }

        selectedDeck = deck;

        if (deck.heroIds.Count > 0)
        {
            var hero = DeckManager.Instance.GetHeroById(deck.heroIds[0]);
            if (hero != null)
            {
                deckImage.sprite = hero.portrait;
                deckNameText.text = deck.deckName;
            }
        }
    }

    void OnDebugBattleClicked()
    {
        if (selectedDeck == null)
        {
            Debug.LogWarning("No deck selected!");
            return;
        }

        // Pass both player and opponent decks (same one)
        BattleLoader.Instance.PreparePlayerDeck(selectedDeck);
        BattleLoader.Instance.PrepareEnemyDeck(selectedDeck);

        SceneManager.LoadScene("BattleScene");
    }
}

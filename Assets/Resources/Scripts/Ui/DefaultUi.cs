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
        if (!PlayerPrefs.HasKey("SelectedDeckId")) return;

        string deckId = PlayerPrefs.GetString("SelectedDeckId");
        DeckData deck = DeckManager.Instance.GetDeck(deckId);

        if (deck == null) return;
        selectedDeck = deck;

        deckNameText.text = deck.deckName;

        // Show main region icon instead of hero portrait
        var mainRegion = DeckManager.Instance.GetRegionById(deck.mainRegionId);
        //if (mainRegion != null && mainRegion.regionIcon != null)
        //    deckImage.sprite = mainRegion.regionIcon;
    }

    void OnDebugBattleClicked()
    {
        if (selectedDeck == null) return;

        BattleLoader.Instance.PreparePlayerDeck(selectedDeck);
        BattleLoader.Instance.PrepareEnemyDeck(selectedDeck);

        SceneManager.LoadScene("BattleScene");
    }
}

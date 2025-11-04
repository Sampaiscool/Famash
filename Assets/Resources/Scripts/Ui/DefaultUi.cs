using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DefaultUi : MonoBehaviour
{
    public Image deckImage;
    public TMP_Text deckNameText;

    void Start()
    {
        LoadSelectedDeck();
    }

    void LoadSelectedDeck()
    {
        if (!PlayerPrefs.HasKey("SelectedDeckId"))
        {
            Debug.Log("No selected deck saved in PlayerPrefs.");
            return;
        }

        string deckId = PlayerPrefs.GetString("SelectedDeckId");
        if (DeckManager.Instance == null)
        {
            Debug.LogError("DeckManager.Instance is null!");
            return;
        }

        DeckData deck = DeckManager.Instance.GetDeck(deckId);
        if (deck == null)
        {
            Debug.LogWarning($"No deck found with ID {deckId}");
            return;
        }

        if (deck.heroIds.Count == 0)
        {
            Debug.LogWarning("Deck has no heroes assigned.");
            return;
        }

        var hero = DeckManager.Instance.GetHeroById(deck.heroIds[0]);
        if (hero == null)
        {
            Debug.LogWarning($"Hero with ID {deck.heroIds[0]} not found.");
            return;
        }

        deckImage.sprite = hero.portrait;
        deckNameText.text = deck.deckName;
    }


}

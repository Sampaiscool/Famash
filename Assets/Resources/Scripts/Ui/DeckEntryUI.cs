using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckEntryUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image deckImage;
    public TMP_Text deckNameText;
    public Button selectButton;
    public Button openButton;
    public GameObject buttonsContainer; // assign the group with Select & Edit

    private DeckData deckData;

    void Start()
    {
        buttonsContainer.SetActive(false);
    }

    public void Bind(DeckData data)
    {
        deckData = data;
        deckNameText.text = data.deckName;

        // Update to support multiple heroes
        var heroNames = new List<string>();
        foreach (var heroId in data.heroIds)
        {
            var hero = DeckManager.Instance.GetHeroById(heroId);
            if (hero != null)
                heroNames.Add(hero.heroName);
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => OnSelectClicked());

        openButton.onClick.RemoveAllListeners();
        openButton.onClick.AddListener(() => OnOpenClicked());

        UpdatePreview(deckData);
    }


    public void OnSelectClicked()
    {
        MenuManager.Instance.SetCurrentDeck(deckData);

        // Update UI immediately
        UpdatePreview(deckData);
    }

    private void OnOpenClicked()
    {
        CreateDeckUI.Instance.OpenWithDeck(deckData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonsContainer.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonsContainer.SetActive(false);
    }
    void UpdatePreview(DeckData deck)
    {
        if (deck.heroIds == null || deck.heroIds.Count == 0) return;
        if (DeckManager.Instance == null || DeckManager.Instance.allHeroes == null) return;

        var hero = DeckManager.Instance.GetHeroById(deck.heroIds[0]);
        if (hero != null && hero.portrait != null)
        {
            deckImage.sprite = hero.portrait;
        }

        deckNameText.text = deck.deckName;
    }

}

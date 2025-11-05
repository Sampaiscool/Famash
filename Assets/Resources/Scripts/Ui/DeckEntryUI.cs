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
    public GameObject buttonsContainer;

    private DeckData deckData;
    private CreateDeckUI createDeckUI;

    void Start()
    {
        buttonsContainer.SetActive(false);
    }

    public void Bind(DeckData data)
    {
        deckData = data;
        deckNameText.text = data.deckName;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClicked);

        openButton.onClick.RemoveAllListeners();
        openButton.onClick.AddListener(OnOpenClicked);

        UpdatePreview(deckData);
    }

    public void OnSelectClicked()
    {
        MenuManager.Instance.SetCurrentDeck(deckData);
        UpdatePreview(deckData);
    }

    private void OnOpenClicked()
    {
        createDeckUI = FindFirstObjectByType<CreateDeckUI>();
        createDeckUI.OpenWithDeck(deckData);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonsContainer.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        buttonsContainer.SetActive(false);
    }

    private void UpdatePreview(DeckData deck)
    {
        if (DeckManager.Instance == null) return;

        var mainRegion = DeckManager.Instance.GetRegionById(deck.mainRegionId);
        //if (mainRegion != null && mainRegion.regionIcon != null)
        //    deckImage.sprite = mainRegion.regionIcon;

        deckNameText.text = deck.deckName;
    }
}

using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardInGame : MonoBehaviour, IPointerClickHandler
{
    [Header("Common Elements")]
    public Image artwork;
    public TMP_Text costText;

    private BaseController owner;

    [Header("Unit Elements")]
    public GameObject unitPanel;
    public TMP_Text attackText;
    public TMP_Text healthText;

    [Header("Spell Elements")]
    public GameObject spellPanel;

    [Header("Field Elements")]
    public GameObject fieldPanel;

    [Header("Hero Elements")]
    public GameObject heroPanel;
    public TMP_Text heroHealthText;
    public TMP_Text heroDamageText;
    public TMP_Text heroLevelText;

    [Header("Detail Popup / Action Panel Prefabs")]
    public GameObject cardDetailPrefab;       // traditional middle-click info
    public GameObject cardActionPanelPrefab;  // scrollable action panel

    private CardRuntime runtimeCard;
    private Canvas uiCanvas;

    private GameObject currentActionPanel;

    public void Bind(CardRuntime card, BaseController ownerController)
    {
        runtimeCard = card;
        owner = ownerController;

        var data = card.cardData;

        artwork.sprite = data.artwork;
        costText.text = data.cost.ToString();

        unitPanel.SetActive(false);
        spellPanel.SetActive(false);
        fieldPanel.SetActive(false);
        heroPanel.SetActive(false);

        switch (data.cardType)
        {
            case CardType.Unit:
                unitPanel.SetActive(true);
                attackText.text = card.currentAttack.ToString();
                healthText.text = card.currentHealth.ToString();
                break;
            case CardType.Spell:
            case CardType.Secret:
                spellPanel.SetActive(true);
                break;
            case CardType.Field:
                fieldPanel.SetActive(true);
                break;
            case CardType.Hero:
                heroPanel.SetActive(true);
                heroHealthText.text = card.currentHealth.ToString();
                heroLevelText.text = "1";
                break;
        }
    }

    private void OnLeftClick()
    {
        if (owner == null || runtimeCard == null)
        {
            Debug.LogWarning("Card clicked without owner or runtimeCard!");
            return;
        }

        // Playing from hand
        if (owner.hand.Contains(runtimeCard))
        {
            owner.TryPlayCard(runtimeCard);
            return;
        }

        // Responding with a card on the field
        bool isFieldCard = owner.fieldSlots.Any(c => c == runtimeCard);

        if (isFieldCard)
        {
            if (currentActionPanel != null)
            {
                Destroy(currentActionPanel); // despawn if already active
                currentActionPanel = null;
                return;
            }

            ShowActionPanel();
            return;
        }

        Debug.Log($"Clicked {runtimeCard.cardData.cardName} (not in hand)");
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            ShowCardInfo();
        }
    }
    private void ShowActionPanel()
    {
        if (runtimeCard == null || cardActionPanelPrefab == null) return;

        // Remove existing panel if any
        if (currentActionPanel != null)
            Destroy(currentActionPanel);

        // Ensure canvas
        uiCanvas ??= FindFirstObjectByType<Canvas>();
        if (uiCanvas == null) return;

        // Spawn panel as sibling of the card so localPosition works
        currentActionPanel = Instantiate(cardActionPanelPrefab, transform.parent);

        RectTransform cardRect = GetComponent<RectTransform>();
        RectTransform panelRect = currentActionPanel.GetComponent<RectTransform>();

        // Place the panel on top of the card
        float yOffset = cardRect.rect.height / 2 + panelRect.rect.height / 2 + 5f; // 5f is a small margin
        panelRect.localPosition = cardRect.localPosition + new Vector3(0f, yOffset, 0f);

        // Make sure the scale is correct
        panelRect.localScale = Vector3.one;

        // Setup panel with the current card
        CardActionPanel panel = currentActionPanel.GetComponent<CardActionPanel>();
        panel?.Setup(runtimeCard);
    }



    private void ShowCardInfo()
    {
        if (runtimeCard == null || cardDetailPrefab == null)
            return;

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("No Canvas found for card info popup!");
            return;
        }

        var popupObj = Instantiate(cardDetailPrefab, canvas.transform);
        var popup = popupObj.GetComponent<CardInfoPopup>();

        // If you’re using CardDetailUI instead, you can swap this:
        // var popup = popupObj.GetComponent<CardDetailUI>();

        popup.Setup(runtimeCard.cardData);
    }


    public void Refresh()
    {
        if (runtimeCard == null) return;
        var data = runtimeCard.cardData;

        if (data.cardType == CardType.Unit)
        {
            attackText.text = runtimeCard.currentAttack.ToString();
            healthText.text = runtimeCard.currentHealth.ToString();
        }
        else if (data.cardType == CardType.Hero)
        {
            heroHealthText.text = runtimeCard.currentHealth.ToString();
        }
    }
}

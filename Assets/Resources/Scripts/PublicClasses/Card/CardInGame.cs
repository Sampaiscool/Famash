using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardInGame : MonoBehaviour, IPointerClickHandler
{
    [Header("Common Elements")]
    public Image artwork;
    public TMP_Text costText;
    public Button cardButton;

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

    [Header("Detail Popup")]
    public GameObject cardDetailPrefab;

    private CardRuntime runtimeCard;
    private Canvas uiCanvas;

    public void Bind(CardRuntime card, BaseController ownerController)
    {
        runtimeCard = card;
        owner = ownerController;

        var data = card.cardData;

        artwork.sprite = data.artwork;
        costText.text = data.cost.ToString();

        // Hide all type panels first
        unitPanel.SetActive(false);
        spellPanel.SetActive(false);
        fieldPanel.SetActive(false);
        heroPanel.SetActive(false);

        // Then show and fill the one we need
        switch (data.cardType)
        {
            case CardType.Unit:
                unitPanel.SetActive(true);
                attackText.text = card.currentAttack.ToString();
                healthText.text = card.currentHealth.ToString();
                break;

            case CardType.Spell:
                spellPanel.SetActive(true);
                break;

            case CardType.Field:
                fieldPanel.SetActive(true);
                break;

            case CardType.Secret:
                spellPanel.SetActive(true);
                break;

            case CardType.Hero:
                heroPanel.SetActive(true);
                heroHealthText.text = card.currentHealth.ToString();
                heroLevelText.text = "1";
                break;
        }

        // Button behavior
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnLeftClick);

    }

    private void OnLeftClick()
    {
        if (owner == null || runtimeCard == null)
        {
            Debug.LogWarning("Card clicked without owner or runtimeCard!");
            return;
        }

        // Only allow playing cards that are in the player's hand
        if (owner.hand.Contains(runtimeCard))
        {
            bool played = owner.TryPlayCard(runtimeCard);
            if (played)
            {
                Debug.Log($"Played {runtimeCard.cardData.cardName}");
            }
            else
            {
                Debug.Log($"{runtimeCard.cardData.cardName} could not be played!");
            }
        }
        else
        {
            // Could be logic for attacking, blocking, etc.
            Debug.Log($"Clicked {runtimeCard.cardData.cardName} (not in hand)");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ShowDetails();
        }
    }

    private void ShowDetails()
    {
        if (cardDetailPrefab == null) return;

        var detail = Instantiate(cardDetailPrefab, transform.position, Quaternion.identity);
        uiCanvas ??= MenuManager.Instance.FindUiCanvas();
        detail.transform.SetParent(uiCanvas.transform, false);

        var detailUI = detail.GetComponent<CardDetailUI>();
        detailUI.Bind(runtimeCard);
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

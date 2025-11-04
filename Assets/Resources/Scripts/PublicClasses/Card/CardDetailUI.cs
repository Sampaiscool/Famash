using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardDetailUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text descriptionText;
    public TMP_Text costText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public TMP_Text effectsText;
    public Button closeButton;

    private CardRuntime runtimeCard;

    public void Bind(CardRuntime card)
    {
        runtimeCard = card;
        var data = card.cardData;

        nameText.text = data.cardName;
        typeText.text = data.cardType.ToString();
        descriptionText.text = data.description;
        costText.text = $"Cost: {data.cost}";
        attackText.text = $"ATK: {data.attack}";
        healthText.text = $"HP: {data.health}";

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(CloseCardDetail);
    }

    void CloseCardDetail()
    {
        Destroy(gameObject); // Destroy the popup when closing
    }
}

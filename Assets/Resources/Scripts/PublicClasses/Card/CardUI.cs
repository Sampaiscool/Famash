using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public Image artwork;
    public TMP_Text nameText;
    public Button cardButton;
    public GameObject cardDetailPrefab;  // Assign this in the inspector

    private CardRuntime runtimeCard;
    private Canvas UICanvas;

    public void Bind(CardRuntime card)
    {
        runtimeCard = card;
        nameText.text = card.cardData.cardName;
        artwork.sprite = card.cardData.artwork;

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => OnCardClicked());
    }

    void OnCardClicked()
    {
        SpawnCardDetailPopup();
    }

    void SpawnCardDetailPopup()
    {
        // Instantiate the detail popup at the card's position
        GameObject cardDetail = Instantiate(cardDetailPrefab, transform.position, Quaternion.identity);

        UICanvas = MenuManager.Instance.FindUiCanvas();

        // Find the Canvas to parent the popup to (if needed)
        cardDetail.transform.SetParent(UICanvas.transform, false);

        // Now bind the data to the card detail popup
        CardDetailUI detailUI = cardDetail.GetComponent<CardDetailUI>();
        detailUI.Bind(runtimeCard);
    }
}

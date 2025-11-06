using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardSelectEntry : MonoBehaviour, IPointerClickHandler
{
    public Image artwork;
    public TMP_Text nameText;
    public TMP_Text countText;

    [HideInInspector] public CardSO cardData;
    private int count = 0;

    public GameObject cardInfoPopupPrefab;

    public System.Action<CardSO, int> onCountChanged; // (card, newCount)

    private void Awake()
    {
        UpdateCount();
    }

    public void Bind(CardSO card)
    {
        cardData = card;
        nameText.text = card.cardName;
        artwork.sprite = card.artwork;
        count = 0;
        UpdateCount();
    }

    public void SetCount(int c)
    {
        count = Mathf.Clamp(c, 0, 3);
        UpdateCount();
    }

    public void OnLeftClick()
    {
        if (count < 3)
        {
            count++;
            UpdateCount();
            onCountChanged?.Invoke(cardData, count);
        }
    }

    public void OnRightClick()
    {
        if (count > 0)
        {
            count--;
            UpdateCount();
            onCountChanged?.Invoke(cardData, count);
        }
    }

    private void UpdateCount()
    {
        countText.text = count > 0 ? $"x{count}" : "";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            OnLeftClick();
        else if (eventData.button == PointerEventData.InputButton.Right)
            OnRightClick();
        else if (eventData.button == PointerEventData.InputButton.Middle)
            ShowCardInfo();
    }

    private void ShowCardInfo()
    {
        if (cardData == null || cardInfoPopupPrefab == null) return;

        var canvas = FindFirstObjectByType<Canvas>();
        var popupObj = Instantiate(cardInfoPopupPrefab, canvas.transform);
        var popup = popupObj.GetComponent<CardInfoPopup>();
        popup.Setup(cardData);
    }
}

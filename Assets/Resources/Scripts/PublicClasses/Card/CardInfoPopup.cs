using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardInfoPopup : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image artwork;
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text healthText;
    public TMP_Text attackText;
    public TMP_Text effectText;

    private static GameObject popupPrefab;

    public static void Show(CardSO card)
    {
        if (popupPrefab == null)
        {
            popupPrefab = Resources.Load<GameObject>("UI/CardInfoPopup");
            if (popupPrefab == null)
            {
                Debug.LogError("Missing prefab: Resources/UI/CardInfoPopup");
                return;
            }
        }

        // Create popup instance
        var canvas = FindFirstObjectByType<Canvas>();
        var instance = Instantiate(popupPrefab, canvas.transform);
        var popup = instance.GetComponent<CardInfoPopup>();
        popup.Setup(card);
    }

    public void Setup(CardSO card)
    {
        if (artwork) artwork.sprite = card.artwork;
        if (nameText) nameText.text = card.cardName;
        if (typeText) typeText.text = card.cardType.ToString();

        switch (card.cardType)
        {
            case CardType.Unit:
                healthText.text = card.health.ToString();
                attackText.text = card.attack.ToString();
                effectText.text = card.description;
                break;

            case CardType.Spell:
                effectText.text = card.description;
                break;

            default:
                effectText.text = "";
                break;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            DestoyInfoPopup();
        else if (eventData.button == PointerEventData.InputButton.Right)
            DestoyInfoPopup();
        else if (eventData.button == PointerEventData.InputButton.Middle)
            DestoyInfoPopup();
    }

    void DestoyInfoPopup()
    {
        Destroy(gameObject);
    }
}

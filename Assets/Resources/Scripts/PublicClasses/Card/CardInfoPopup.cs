using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardInfoPopup : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image artwork;
    public Image biggerArtwork;
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text healthText;
    public TMP_Text attackText;
    public TMP_Text effectText;

    private bool isBiggerArtworkShown = false;

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
        if (biggerArtwork) biggerArtwork.sprite = card.artwork;
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

        biggerArtwork.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Check if the clicked object is the artwork
        if (eventData.pointerCurrentRaycast.gameObject == artwork.gameObject)
        {
            ShowBiggerArtwork();
            return; // stop further handling
        }

        // If bigger artwork is shown, hide it
        if (isBiggerArtworkShown)
        {
            HideBiggerArtwork();
            return;
        }

        // Otherwise destroy the popup
        DestroyInfoPopup();
    }


    void ShowBiggerArtwork()
    {
        if (biggerArtwork == null || artwork == null) return;

        isBiggerArtworkShown = true;

        // Enable the bigger artwork
        biggerArtwork.gameObject.SetActive(true);

        // Scale it up
        Vector2 originalSize = artwork.rectTransform.sizeDelta;
        biggerArtwork.rectTransform.sizeDelta = originalSize * 3.5f;

        // Center it on the screen
        biggerArtwork.rectTransform.position = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Optionally, make it on top of everything
        biggerArtwork.transform.SetAsLastSibling();
    }

    void HideBiggerArtwork()
    {
        isBiggerArtworkShown = false;
        biggerArtwork.gameObject.SetActive(false);
    }

    void DestroyInfoPopup()
    {
        Destroy(gameObject);
    }
}

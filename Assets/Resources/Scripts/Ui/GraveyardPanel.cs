using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraveyardPanel : MonoBehaviour
{
    public RectTransform contentParent;
    public GameObject cardUIPrefab; // reuse your CardInGame prefab

    private BaseController owner;

    public void Setup(BaseController ownerController)
    {
        owner = ownerController;

        // Clear old UI
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        // Spawn UI for each card in the graveyard
        foreach (var card in owner.graveyard)
        {
            var cardObj = Instantiate(cardUIPrefab, contentParent);
            var cardUI = cardObj.GetComponent<CardInGame>();
            cardUI.Bind(card, owner);
        }
    }

    public void ClosePanel()
    {
        Destroy(gameObject);
    }
}

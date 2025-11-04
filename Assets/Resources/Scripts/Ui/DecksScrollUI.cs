using System.Collections.Generic;
using UnityEngine;

public class DecksScrollUI : MonoBehaviour
{
    public RectTransform content;
    public GameObject deckEntryPrefab;

    private List<GameObject> instantiated = new List<GameObject>();

    void Start() => Refresh();

    public void Refresh()
    {
        if (DeckManager.Instance == null || DeckManager.Instance.decks == null) return;
        if (deckEntryPrefab == null || content == null)
        {
            Debug.LogError("DecksScrollUI is missing references!");
            return;
        }

        DeckManager.Instance.LoadDecks();

        Clear();

        foreach (var deck in DeckManager.Instance.decks)
        {
            var go = Instantiate(deckEntryPrefab, content);
            var ui = go.GetComponent<DeckEntryUI>();
            if (ui != null) ui.Bind(deck);
            instantiated.Add(go);
        }
    }


    public void Clear()
    {
        foreach (var g in instantiated) if (g != null) Destroy(g);
        instantiated.Clear();
    }
}

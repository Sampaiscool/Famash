using UnityEngine;

public class MenuSceneUiManager : MonoBehaviour
{
    public GameObject[] pages;

    public void Start()
    {
        ShowPage("Default");
    }

    // Show the specified page and hide all others
    public void ShowPage(string pageName)
    {
        foreach (var page in pages)
            page.SetActive(page.name == pageName);
    }
}

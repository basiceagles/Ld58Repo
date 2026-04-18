using UnityEngine;

public class Hyperlink : MonoBehaviour
{
    [Header("Hyperlink Settings")]
    [SerializeField] private string hyperlink = "https://www.example.com";

    public void OpenHyperlink()
    {
        Application.OpenURL(hyperlink);
    }
}

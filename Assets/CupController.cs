using UnityEngine;
using TMPro;

public class CupController : MonoBehaviour
{
    [SerializeField] private GameObject steamObject;
    [SerializeField] private TMP_Text teaTimeText;

    public bool IsSweetened { get; private set; }

    private void Awake()
    {
        if (steamObject != null) steamObject.SetActive(false);
        if (teaTimeText != null) teaTimeText.gameObject.SetActive(false);
        IsSweetened = false;
    }

    public void SetSteam(bool on)
    {
        if (steamObject != null) steamObject.SetActive(on);
    }

    public void MarkSweetened()
    {
        IsSweetened = true;
    }

    public void ShowTeaTimeMessage(string message)
    {
        if (teaTimeText == null) return;
        teaTimeText.text = message;
        teaTimeText.gameObject.SetActive(true);
    }
}

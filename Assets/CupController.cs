using UnityEngine;
using TMPro;

public class CupController : MonoBehaviour
{
    [Header("Reference na child objekte skodelice")]
    [SerializeField] private GameObject teaLiquidObject;
    [SerializeField] private GameObject steamObject;

    [Header("Optional: TextMeshPro message (child)")]
    [SerializeField] private TMP_Text teaTimeText;

    public bool HasTea { get; private set; }
    public bool SteamActive { get; private set; }
    public bool IsSweetened { get; private set; }

    private void Awake()
    {
        if (teaLiquidObject != null) teaLiquidObject.SetActive(false);
        if (steamObject != null) steamObject.SetActive(false);
        if (teaTimeText != null) teaTimeText.gameObject.SetActive(false);

        HasTea = false;
        SteamActive = false;
        IsSweetened = false;
    }

    public void FillTeaAndStartSteam()
    {
        HasTea = true;

        if (teaLiquidObject != null) teaLiquidObject.SetActive(true);

        // v navodilih: “iz skodelice naj se začne kaditi para”
        SteamActive = true;
        if (steamObject != null) steamObject.SetActive(true);
    }

    public void StopSteamIfActive()
    {
        if (!SteamActive) return;

        SteamActive = false;
        if (steamObject != null) steamObject.SetActive(false);
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

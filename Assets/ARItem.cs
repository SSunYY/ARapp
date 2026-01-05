using UnityEngine;

public enum ARItemType
{
    Cajnik,      // čajnik
    CajaSkatla,  // škatla s čajem
    Skodelica,   // skodelica
    Sladkor,     // posodica s sladkorjem
    Keksi        // krožnik s piškoti
}

public class ARItem : MonoBehaviour
{
    [Header("Izberi tip objekta za napredno logiko (bližina, dogodki)")]
    [SerializeField] private ARItemType itemType;

    public ARItemType ItemType => itemType;
}

using UnityEngine;

public class ProximityLogic : MonoBehaviour
{
    [Header("Bližina v metrih (Unity enote). Prilagodi po potrebi.")]
    [SerializeField] private float proximityDistance = 0.25f;

    [Header("Napis (slovensko ali angleško)")]
    [SerializeField] private string teaTimeMessage = "Uživajte v čajanki.";

    // Notranja stanja (da ne prožimo neskončno)
    private bool boxOpened;
    private bool cupFilled;
    private bool messageShown;

    // Edge trigger za sladkor (da se kocka ne odšteva vsak frame)
    private bool sugarWasNearCup;

    private void Update()
    {
        // Najdi vse ARItem-e (tudi inactive so vključeni, a mi filtriramo na activeInHierarchy)
        var items = UnityEngine.Object.FindObjectsByType<ARItem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        ARItem teapot = null;
        ARItem teaBox = null;
        ARItem cup = null;
        ARItem sugar = null;
        ARItem cookies = null;

        // vzemi prve aktivne po tipu (ker imaš po 1 kos vsakega)
        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            if (it == null || !it.gameObject.activeInHierarchy) continue;

            switch (it.ItemType) // <-- PRAVILNO: uporablja public getter
            {
                case ARItemType.Cajnik:
                    if (teapot == null) teapot = it;
                    break;

                case ARItemType.CajaSkatla:
                    if (teaBox == null) teaBox = it;
                    break;

                case ARItemType.Skodelica:
                    if (cup == null) cup = it;
                    break;

                case ARItemType.Sladkor:
                    if (sugar == null) sugar = it;
                    break;

                case ARItemType.Keksi:
                    if (cookies == null) cookies = it;
                    break;
            }
        }

        // Če osnovnih objektov ni, ne delaj nič
        if (teapot == null || teaBox == null || cup == null)
            return;

        var teaBoxCtrl = teaBox.GetComponent<TeaBoxController>();
        var cupCtrl = cup.GetComponent<CupController>();

        // 1) Če je škatla blizu čajnika -> odpri škatlo
        if (!boxOpened && IsNear(teapot.transform.position, teaBox.transform.position))
        {
            boxOpened = true;
            if (teaBoxCtrl != null) teaBoxCtrl.SetOpen(true);
        }

        // 2) Če so čajnik + (odprta) škatla + skodelica skupaj -> napolni skodelico in začni paro
        if (boxOpened && !cupFilled)
        {
            bool cupNearTeapot = IsNear(cup.transform.position, teapot.transform.position);
            bool cupNearBox = IsNear(cup.transform.position, teaBox.transform.position);

            if (cupNearTeapot && cupNearBox)
            {
                cupFilled = true;
                if (cupCtrl != null) cupCtrl.FillTeaAndStartSteam();
            }
        }

        // 3) Sladkor: ko sladkor pride blizu skodelice s čajem -> odstrani 1 kocko in ustavi paro (prvič)
        if (cupFilled && sugar != null && cupCtrl != null)
        {
            bool sugarNearCup = IsNear(sugar.transform.position, cup.transform.position);

            // “rising edge” – sproži samo ob prihodu v bližino
            if (sugarNearCup && !sugarWasNearCup)
            {
                var sugarCtrl = sugar.GetComponent<SugarController>();
                if (sugarCtrl != null)
                {
                    bool removed = sugarCtrl.ConsumeOneCube();

                    if (removed)
                    {
                        // Prvič ustavi paro, potem pa naj ostane ustavljena (pri ponovitvi ni treba ustavljati)
                        cupCtrl.StopSteamIfActive();
                        cupCtrl.MarkSweetened();
                    }
                }
            }

            sugarWasNearCup = sugarNearCup;
        }

        // 4) Piškoti: ko je čaj sladkan in piškoti pridejo blizu skodelice -> prikaži napis
        if (!messageShown && cupCtrl != null && cupCtrl.IsSweetened && cookies != null)
        {
            bool cookiesNearCup = IsNear(cookies.transform.position, cup.transform.position);
            if (cookiesNearCup)
            {
                messageShown = true;
                cupCtrl.ShowTeaTimeMessage(teaTimeMessage);
            }
        }
    }

    private bool IsNear(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a, b) <= proximityDistance;
    }
}

using UnityEngine;

public class ProximityLogic : MonoBehaviour
{
    [SerializeField] private float proximityDistance = 0.25f;
    [SerializeField] private string teaTimeMessage = "Uživajte v čajanki.";

    private bool sugarWasNearCup;
    private bool messageShown;

    private void Update()
    {
        var items = Object.FindObjectsByType<ARItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        ARItem teapot = null;
        ARItem teaBox = null;
        ARItem cup = null;
        ARItem sugar = null;
        ARItem cookies = null;

        for (int i = 0; i < items.Length; i++)
        {
            var it = items[i];
            if (it == null || !it.gameObject.activeInHierarchy) continue;

            switch (it.ItemType)
            {
                case ARItemType.Cajnik:     teapot ??= it; break;
                case ARItemType.CajaSkatla: teaBox ??= it; break;
                case ARItemType.Skodelica:  cup ??= it; break;
                case ARItemType.Sladkor:    sugar ??= it; break;
                case ARItemType.Keksi:      cookies ??= it; break;
            }
        }

        // 1) ŠKATLA + ČAJNIK -> škatla se odpre
        if (teaBox != null)
        {
            var boxCtrl = teaBox.GetComponent<TeaBoxController>();
            if (boxCtrl != null)
            {
                bool openNow = (teapot != null) && IsNear(teapot.transform.position, teaBox.transform.position);
                boxCtrl.SetOpen(openNow);
            }
        }

        // 2) SKODELICA + ČAJNIK -> para začne (brez tekočine)
        if (cup != null)
        {
            var cupCtrl = cup.GetComponent<CupController>();
            if (cupCtrl != null)
            {
                bool steamNow = (teapot != null) && IsNear(teapot.transform.position, cup.transform.position);
                cupCtrl.SetSteam(steamNow);
            }
        }

        // Za sladkor in kekse potrebujemo skodelico + njen controller
        if (cup == null) { sugarWasNearCup = false; return; }
        var cupController2 = cup.GetComponent<CupController>();
        if (cupController2 == null) { sugarWasNearCup = false; return; }

        // 3) SLADKOR + SKODELICA -> izginja 1 kocka ob prihodu
        if (sugar != null)
        {
            bool sugarNearCup = IsNear(sugar.transform.position, cup.transform.position);

            if (sugarNearCup && !sugarWasNearCup)
            {
                var sugarCtrl = sugar.GetComponent<SugarController>();
                if (sugarCtrl != null)
                {
                    bool removed = sugarCtrl.ConsumeOneCube();
                    if (removed)
                        cupController2.MarkSweetened();
                }
            }

            sugarWasNearCup = sugarNearCup;
        }
        else
        {
            sugarWasNearCup = false;
        }

        // 4) KEKSI + SKODELICA -> pokaži tekst, ko je sladkano
        if (!messageShown && cupController2.IsSweetened && cookies != null)
        {
            if (IsNear(cookies.transform.position, cup.transform.position))
            {
                messageShown = true;
                cupController2.ShowTeaTimeMessage(teaTimeMessage);
            }
        }
    }

    private bool IsNear(Vector3 a, Vector3 b) => Vector3.Distance(a, b) <= proximityDistance;
}

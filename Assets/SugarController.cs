using System.Collections.Generic;
using UnityEngine;

public class SugarController : MonoBehaviour
{
    [Header("Kocke sladkorja (child objekti) - vrstni red je pomemben")]
    [SerializeField] private List<GameObject> sugarCubes = new List<GameObject>();

    public int Remaining => CountRemaining();

    public bool ConsumeOneCube()
    {
        // odstrani prvo aktivno kocko po vrstnem redu
        for (int i = 0; i < sugarCubes.Count; i++)
        {
            var cube = sugarCubes[i];
            if (cube == null) continue;

            if (cube.activeSelf)
            {
                cube.SetActive(false);
                return true;
            }
        }
        return false;
    }

    private int CountRemaining()
    {
        int c = 0;
        for (int i = 0; i < sugarCubes.Count; i++)
        {
            if (sugarCubes[i] != null && sugarCubes[i].activeSelf) c++;
        }
        return c;
    }
}

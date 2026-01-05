using UnityEngine;

public class TeaBoxController : MonoBehaviour
{
    [Header("Reference na child objekte škatle")]
    [SerializeField] private GameObject closedObject;
    [SerializeField] private GameObject openObject;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        // Varnost: če imaš samo en model, naj se vsaj ne sesuje
        if (closedObject != null) closedObject.SetActive(true);
        if (openObject != null) openObject.SetActive(false);
        IsOpen = false;
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;

        if (closedObject != null) closedObject.SetActive(!open);
        if (openObject != null) openObject.SetActive(open);
    }

    public void OpenBox()
    {
        IsOpen = true;
        ApplyState(IsOpen);
    }

    public void CloseBox()
    {
        IsOpen = false;
        ApplyState(IsOpen);
    }

    private void ApplyState(bool open)
    {
        if (closedObject != null) closedObject.SetActive(!open);
        if (openObject != null) openObject.SetActive(open);
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

public class UI_PanelClickHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool isHeld = false;

    // Called when the pointer is pressed down
    public void OnPointerDown(PointerEventData eventData)
    {
        isHeld = true;
    }

    // Called when the pointer is released
    public void OnPointerUp(PointerEventData eventData)
    {
        isHeld = false;
    }
}

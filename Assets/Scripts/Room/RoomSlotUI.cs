using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RoomSlotUI : MonoBehaviour, IPointerClickHandler
{
    public int slotIndex;
    public IdleManager idleManager;
    public RoomPlacementManager placementManager;
    public GameObject highlightObject;

    public void SetHighlight(bool state, bool canPlace)
    {
        if (highlightObject == null) return;

        highlightObject.SetActive(state);

        if (state)
        {
            var img = highlightObject.GetComponent<Image>();

            if (img != null)
                img.color = canPlace ? new Color(0, 1, 0, 0.35f) : new Color(1, 0, 0, 0.35f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (placementManager != null)
        {
            placementManager.TryPlaceAtSlot(this);
        }
    }
}

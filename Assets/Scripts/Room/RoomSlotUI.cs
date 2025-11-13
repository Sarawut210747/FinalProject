using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class RoomSlotUI : MonoBehaviour, IPointerClickHandler
{
    public int slotIndex;   // index ใน IdleManager.rooms
    public IdleManager idleManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        idleManager.TryPlaceRoom(slotIndex);
    }
}

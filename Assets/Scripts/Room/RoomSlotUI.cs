using UnityEngine;
using UnityEngine.EventSystems;

public class RoomSlotUI : MonoBehaviour, IPointerClickHandler
{
    public int slotIndex;
    public IdleManager idleManager;

    // ถ้าใช้ Button.OnClick()
    public void OnClick()
    {
        idleManager.TryPlaceRoom(slotIndex);
    }

    // หรือใช้คลิกจาก IPointerClickHandler ก็ได้
    public void OnPointerClick(PointerEventData eventData)
    {
        idleManager.TryPlaceRoom(slotIndex);
    }
}

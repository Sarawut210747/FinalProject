using UnityEngine;

public class RoomPlacementManager : MonoBehaviour
{
    public IdleManager idleManager;
    public RoomSlotUI[] allSlots;

    private RoomTypeSO selectedRoomType;
    private bool isPlacing = false;

    // 🔥 เพิ่มบรรทัดนี้
    public bool IsPlacing => isPlacing;

    void Start()
    {
        RegisterSlots();
    }

    public void RegisterSlots()
    {
        allSlots = FindObjectsByType<RoomSlotUI>(FindObjectsSortMode.None);

        foreach (var slot in allSlots)
        {
            slot.placementManager = this;
        }
    }

    public void BeginPlacement(RoomTypeSO type)
    {
        selectedRoomType = type;
        isPlacing = true;

        UpdateHighlights();
    }

    public void CancelPlacement()
    {
        isPlacing = false;
        selectedRoomType = null;

        foreach (var slot in allSlots)
        {
            slot.SetHighlight(false, false);
        }
    }

    private void UpdateHighlights()
    {
        if (!isPlacing) return;

        foreach (var slot in allSlots)
        {
            bool canPlace = idleManager.CanPlaceRoomAt(slot.slotIndex);
            slot.SetHighlight(true, canPlace);
        }
    }

    public void TryPlaceAtSlot(RoomSlotUI slot)
    {
        if (!isPlacing) return;

        int index = slot.slotIndex;

        if (idleManager.CanPlaceRoomAt(index))
        {
            idleManager.PlaceRoomAtIndex(index, selectedRoomType);
        }

        CancelPlacement();
    }

}

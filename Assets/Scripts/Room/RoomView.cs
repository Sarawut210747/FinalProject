using UnityEngine;
using UnityEngine.UI;

// สคริปต์นี้ไว้ติดกับ Prefab RoomView
// ทำหน้าที่แค่แสดงรูปห้อง + เปลี่ยนสีตามสถานะเช่า
public class RoomView : MonoBehaviour
{
    [Header("UI")]
    public Image roomImage;
    public int roomIndex;


    public void SetIndex(int i)
    {
        roomIndex = i;
    }

    public void Setup(Sprite sprite, bool isRented)
    {
        if (sprite != null)
        {
            roomImage.sprite = sprite;
            roomImage.enabled = true;
        }
        else
        {
            roomImage.enabled = false;
        }
    }
}


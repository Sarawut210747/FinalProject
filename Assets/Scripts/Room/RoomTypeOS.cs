using UnityEngine;

[CreateAssetMenu(fileName = "RoomType", menuName = "IdleHotel/RoomType")]
public class RoomTypeSO : ScriptableObject
{
    public string roomName;
    public Sprite roomSprite;
    public float Cost = 500;      // ราคาซื้อห้อง
    public float rentPrice = 100;    // ค่าเช่าที่ลูกค้าจ่าย
    public int rentDurationDays = 30;
}

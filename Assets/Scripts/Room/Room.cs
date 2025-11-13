using UnityEngine;

[System.Serializable]
public class Room
{
    [Header("Basic Info")]
    public string roomName = "Room";   // ชื่อห้อง เอาไว้ดูใน Inspector เฉย ๆ
    public int rentPrice = 500;        // ค่าเช่าห้อง 1 เดือน (ได้ตอนลูกค้าเข้า)
    public bool isRented = false;      // ห้องนี้มีลูกค้าอยู่รึยัง
    public RoomTypeSO roomType;   // ประเภทของห้องที่ถูกซื้อ


    [Header("Visual")]
    public Sprite roomSprite;          // รูปห้อง ใช้ไปแสดงที่ Image ใน UI

    [Header("Contract")]
    public int rentStartYear;          // ปีที่ลูกค้าเริ่มเข้าอยู่
    public int rentStartMonth;         // เดือนที่เริ่มอยู่
    public int rentStartDay;           // วันที่เริ่มอยู่

    public int rentDurationDays = 30;  // ระยะเวลาสัญญา (ค่าเริ่มต้น = 30 วัน = 1 เดือน)
}

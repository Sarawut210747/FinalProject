using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;   // สำหรับ Image
using System.Collections.Generic;

public class IdleManager : MonoBehaviour
{
    // ------------------------------
    // เงินในเกม
    // ------------------------------
    [Header("Gold Settings")]
    public float currentGold = 0f;             // จำนวนเงินปัจจุบัน

    [Header("UI")]
    public TMP_Text goldText;                  // Text แสดง Gold
    public TMP_Text offlineRewardText;         // ไว้โชว์ข้อความอื่น ๆ (จะไม่ใช้ก็ได้)
    public TMP_Text monthlyRewardText;         // Text แสดงข้อความรายได้ของเดือนนั้น (เงินเดือน)


    // ------------------------------
    // เงินเดือนพื้นฐาน (ไม่เกี่ยวกับค่าเช่าห้อง)
    // ------------------------------

    // key สำหรับเซฟว่าจ่ายเงินเดือนของเดือนนี้ไปรึยัง
    const string KEY_LAST_MONTHLY_YEAR = "IDLE_LAST_MONTHLY_YEAR";
    const string KEY_LAST_MONTHLY_MONTH = "IDLE_LAST_MONTHLY_MONTH";

    // ------------------------------
    // วัน-เดือน-ปี ในเกม
    // ------------------------------
    [Header("Game Date")]
    public int gameYear = 1;
    public int gameMonth = 1;
    public int gameDay = 1;
    public int daysPerMonth = 30;              // 1 เดือนในเกม = 30 วัน
    public TMP_Text dateText;                  // Text แสดงวันที่ในเกม

    const string KEY_GAME_YEAR = "IDLE_GAME_YEAR";
    const string KEY_GAME_MONTH = "IDLE_GAME_MONTH";
    const string KEY_GAME_DAY = "IDLE_GAME_DAY";

    // ------------------------------
    // ระบบเวลาในเกม — X วิ = 1 วันในเกม
    // ------------------------------
    [Header("Game Time")]
    public float realSecondsPerDay = 10f;      // กี่วินาทีของโลก = 1 วันในเกม
    private float dayTimer = 0f;               // ตัวจับเวลาสะสม

    const string KEY_LAST_REAL_TIME = "LAST_REAL_TIME"; // timestamp ตอนปิดเกม

    // ------------------------------
    // ห้อง + รูปห้อง
    // ------------------------------
    [Header("Rooms Setting")]
    public List<Room> rooms = new List<Room>();         // เริ่มจาก 1 ห้อง (ไปตั้ง Size ใน Inspector ได้)

    // ------------------------------
    // ฟังก์ชันช่วยคำนวณจำนวนวันระหว่างวันที่ 2 วัน
    // ใช้สำหรับเช็คว่าอยู่ครบสัญญารึยัง
    // ------------------------------
    int CountDaysPassed(int y1, int m1, int d1, int y2, int m2, int d2)
    {
        // นิยาม: 1 ปี = 360 วัน (12 เดือน * 30 วัน)
        int days1 = y1 * 360 + (m1 - 1) * 30 + d1;
        int days2 = y2 * 360 + (m2 - 1) * 30 + d2;
        return days2 - days1;
    }

    [Header("Customer Settings")]
    [Range(0f, 1f)]
    public float customerSpawnChancePerDay = 0.3f;
    // โอกาสที่ลูกค้าจะมาเช่าห้องต่อวัน (0.3 = 30%)

    [Header("Room UI Auto")]
    public RoomView roomViewPrefab;
    public GameObject roomSlotPrefab;
    public Transform roomContainer;
    public int totalRoomSlots;

    // private RoomView[] roomViews;
    // prefix สำหรับ key ของข้อมูลห้องใน PlayerPrefs
    const string KEY_ROOM_PREFIX = "IDLE_ROOM_";

    // ฟังก์ชันช่วยสร้างชื่อ key เช่น "IDLE_ROOM_0_IS_RENTED"
    string GetRoomKey(int index, string field)
    {
        return KEY_ROOM_PREFIX + index + "_" + field;
    }
    private bool isPlacingRoom = false;
    private RoomTypeSO roomToPlace = null;

    // ==========================================================
    #region Start & Update
    // ==========================================================

    void Start()
    {
        // โหลดเงินกับวันที่ล่าสุด
        LoadGold();
        LoadGameDate();
        LoadRooms();

        // เดินวันย้อนหลังตามเวลาที่ออฟไลน์ไป
        CheckOfflineGameTime();

        // อัปเดต UI เบื้องต้น
        UpdateDateUI();
        UpdateGoldUI();
        CreateRoomViews();
        GenerateRoomSlots();
        // เซฟเวลาปัจจุบันไว้เป็นจุดอ้างอิงสำหรับออฟไลน์รอบหน้า
        PlayerPrefs.SetString(KEY_LAST_REAL_TIME, DateTime.UtcNow.Ticks.ToString());
    }

    void Update()
    {
        // นับเวลาจริงสะสม
        dayTimer += Time.deltaTime;

        // ครบจำนวนวินาทีที่กำหนด = 1 วันในเกม
        if (dayTimer >= realSecondsPerDay)
        {
            dayTimer -= realSecondsPerDay;
            AdvanceOneDay(); // ขยับเวลาในเกมไป 1 วัน
        }
    }

    #endregion
    // ==========================================================

    void OnApplicationPause(bool pause)
    {
        if (pause) SaveData();
    }

    void OnApplicationQuit()
    {
        SaveData();
    }

    // ------------------------------
    // เซฟเงิน + วันที่ + timestamp ปัจจุบัน
    // ------------------------------
    void SaveData()
    {
        PlayerPrefs.SetFloat("IDLE_GOLD", currentGold);

        SaveGameDate();   // เซฟวันที่
        SaveRooms();      // 🟢 เซฟสถานะห้อง (ลูกค้า, วันเริ่มเช่า, สัญญา)

        // เซฟเวลาปัจจุบันไว้ใช้คำนวณออฟไลน์รอบหน้า
        PlayerPrefs.SetString(KEY_LAST_REAL_TIME, DateTime.UtcNow.Ticks.ToString());

        PlayerPrefs.Save();
    }

    // --------------------------------------------------------
    // เซฟสถานะ "ห้อง" ทุกห้องลง PlayerPrefs
    // - เซฟว่าเช่ารึยัง (isRented)
    // - เซฟวันเริ่มเช่า (rentStartYear/Month/Day)
    // - เซฟระยะเวลาสัญญา (rentDurationDays)
    // --------------------------------------------------------
    void SaveRooms()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            Room r = rooms[i];

            // bool เซฟเป็น 0/1
            PlayerPrefs.SetInt(GetRoomKey(i, "IS_RENTED"), r.isRented ? 1 : 0);

            PlayerPrefs.SetInt(GetRoomKey(i, "START_YEAR"), r.rentStartYear);
            PlayerPrefs.SetInt(GetRoomKey(i, "START_MONTH"), r.rentStartMonth);
            PlayerPrefs.SetInt(GetRoomKey(i, "START_DAY"), r.rentStartDay);

            PlayerPrefs.SetInt(GetRoomKey(i, "DURATION"), r.rentDurationDays);
        }
    }
    // --------------------------------------------------------
    // โหลดสถานะห้องจาก PlayerPrefs กลับเข้า rooms[]
    // ถ้าไม่มี key นั้น ๆ จะใช้ค่าที่ตั้งใน Inspector ตามเดิม
    // --------------------------------------------------------
    void LoadRooms()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            Room r = rooms[i];

            // ถ้ายังไม่เคยเซฟห้องนี้เลย → ข้าม ใช้ค่าจาก Inspector
            if (!PlayerPrefs.HasKey(GetRoomKey(i, "IS_RENTED")))
                continue;

            int rentedInt = PlayerPrefs.GetInt(GetRoomKey(i, "IS_RENTED"), 0);
            r.isRented = (rentedInt == 1);

            r.rentStartYear = PlayerPrefs.GetInt(GetRoomKey(i, "START_YEAR"), 0);
            r.rentStartMonth = PlayerPrefs.GetInt(GetRoomKey(i, "START_MONTH"), 0);
            r.rentStartDay = PlayerPrefs.GetInt(GetRoomKey(i, "START_DAY"), 0);

            // เผื่อในอนาคตอยากเปลี่ยนระยะสัญญาแล้วเซฟไว้
            r.rentDurationDays = PlayerPrefs.GetInt(GetRoomKey(i, "DURATION"), r.rentDurationDays);
        }
    }



    void LoadGold()
    {
        currentGold = PlayerPrefs.GetFloat("IDLE_GOLD", 0f);
    }

    // ------------------------------
    // ตอนเปิดเกม: เช็คว่าตอนเราปิดไป เวลาจริงผ่านไปกี่วิ
    // → แปลงเป็นกี่วันในเกม → เดิน AdvanceOneDay() ย้อนหลัง
    // ------------------------------
    void CheckOfflineGameTime()
    {
        if (!PlayerPrefs.HasKey(KEY_LAST_REAL_TIME))
            return;

        long lastTicks = long.Parse(PlayerPrefs.GetString(KEY_LAST_REAL_TIME));
        DateTime last = new DateTime(lastTicks, DateTimeKind.Utc);
        DateTime now = DateTime.UtcNow;

        double realSecondsPassed = (now - last).TotalSeconds;
        double gameDaysPassed = realSecondsPassed / realSecondsPerDay;

        int daysToProcess = Mathf.FloorToInt((float)gameDaysPassed);

        // เดินวันย้อนหลังทีละวัน (เช็คสัญญา + เงินเดือนไปด้วย)
        for (int i = 0; i < daysToProcess; i++)
        {
            AdvanceOneDay();
        }
    }

    // ------------------------------
    // UI แสดงจำนวนเงิน
    // ------------------------------
    void UpdateGoldUI()
    {
        if (goldText != null)
            goldText.text = $"Gold: {currentGold:0}";
    }

    // ------------------------------
    // เพิ่ม/ใช้เงิน เรียกจากสคริปต์อื่นได้
    // ------------------------------
    public void AddGold(float amount)
    {
        currentGold += amount;
        UpdateGoldUI();
    }

    public bool SpendGold(float amount)
    {
        if (currentGold >= amount)
        {
            currentGold -= amount;
            UpdateGoldUI();
            return true;
        }
        return false;
    }

    // ------------------------------
    // เงินเดือนพื้นฐาน (ไม่รวมค่าเช่าห้อง)
    // จ่ายเมื่อวันที่ 1 ของเดือน และยังไม่ได้จ่ายเดือนนี้
    // ------------------------------
    void CheckMonthlySalary()
    {
        int lastYear = PlayerPrefs.GetInt(KEY_LAST_MONTHLY_YEAR, 0);
        int lastMonth = PlayerPrefs.GetInt(KEY_LAST_MONTHLY_MONTH, 0);

        bool alreadyPaidThisMonth = (lastYear == gameYear && lastMonth == gameMonth);

        // เงื่อนไข: เป็นวันที่ 1 และยังไม่จ่ายของเดือนนี้
        if (!alreadyPaidThisMonth && gameDay == 1)
        {
            // if (monthlyRewardText != null)
            //     monthlyRewardText.text = $"เงินเดือนพื้นฐาน +{monthlySalary}";

            PlayerPrefs.SetInt(KEY_LAST_MONTHLY_YEAR, gameYear);
            PlayerPrefs.SetInt(KEY_LAST_MONTHLY_MONTH, gameMonth);
            PlayerPrefs.Save();

            UpdateGoldUI();
        }
    }

    // ------------------------------
    // โหลด/เซฟ วันเดือนปีในเกม
    // ------------------------------
    void LoadGameDate()
    {
        gameYear = PlayerPrefs.GetInt(KEY_GAME_YEAR, 1);
        gameMonth = PlayerPrefs.GetInt(KEY_GAME_MONTH, 1);
        gameDay = PlayerPrefs.GetInt(KEY_GAME_DAY, 1);
    }

    void SaveGameDate()
    {
        PlayerPrefs.SetInt(KEY_GAME_YEAR, gameYear);
        PlayerPrefs.SetInt(KEY_GAME_MONTH, gameMonth);
        PlayerPrefs.SetInt(KEY_GAME_DAY, gameDay);
    }

    // ------------------------------
    // อัปเดต UI วันที่
    // ------------------------------
    void UpdateDateUI()
    {
        if (dateText != null)
            dateText.text = $"Day {gameDay} / {gameMonth} / {gameYear}";
    }

    // ------------------------------
    // ข้ามวันในเกม 1 วัน
    // - ข้ามเดือน/ปีถ้าถึง limit
    // - เช็คสัญญาหมดอายุ
    // - เช็คเงินเดือน
    // ------------------------------
    public void AdvanceOneDay()
    {
        gameDay++;

        if (gameDay > daysPerMonth)
        {
            gameDay = 1;
            gameMonth++;

            if (gameMonth > 12)
            {
                gameMonth = 1;
                gameYear++;
            }
        }

        // เช็คสัญญาหมดอายุ
        CheckRoomContracts();

        // 👇 เพิ่มตรงนี้: ลองสุ่มลูกค้าเข้า เมื่อมีห้องว่าง
        TryAutoRentRoomPerDay();

        // เช็คเงินเดือนพื้นฐาน
        CheckMonthlySalary();

        UpdateDateUI();
        SaveGameDate();
    }


    // ------------------------------
    // ปุ่ม Reset เกม (ล้างเซฟทั้งหมด)
    // ------------------------------
    public void ResetGame()
    {
        PlayerPrefs.DeleteAll();

        currentGold = 0;
        gameYear = 1;
        gameMonth = 1;
        gameDay = 1;

        // เคลียร์สถานะห้องทั้งหมด
        foreach (Room r in rooms)
        {
            r.isRented = false;
            r.rentStartYear = 0;
            r.rentStartMonth = 0;
            r.rentStartDay = 0;
        }

        UpdateGoldUI();
        UpdateDateUI();
        SaveData();

        if (monthlyRewardText != null) monthlyRewardText.text = "";
        if (offlineRewardText != null) offlineRewardText.text = "";

        Debug.Log("Game Reset Complete!");
    }

    // ------------------------------------------------------------------
    // ฟังก์ชันให้ "ลูกค้าเข้าห้อง" ตาม index ของห้อง
    // index = หมายเลขห้องใน Array rooms[]
    // ------------------------------------------------------------------
    public void RentRoom(int index)
    {
        // ถ้าหลุดขอบ array ก็ไม่ต้องทำอะไร
        if (index < 0 || index >= rooms.Count)
            return;

        // เลือกห้องตาม index
        Room r = rooms[index];

        // ถ้าห้องนี้มีลูกค้าอยู่แล้ว ก็ไม่ต้องทำอะไร
        if (r.isRented)
            return;

        // ตั้งให้ห้องนี้มีลูกค้า
        r.isRented = true;

        // บันทึกวันที่เริ่มเช่า
        r.rentStartYear = gameYear;
        r.rentStartMonth = gameMonth;
        r.rentStartDay = gameDay;

        Debug.Log($"[RentRoom] Customer moved into Room {r.roomName} at {gameDay}/{gameMonth}/{gameYear}");

        // **อัปเดตรูปห้องให้รู้ว่ามีลูกค้าแล้ว**
        //UpdateRoomView(index);
    }

    // ------------------------------
    // ระบบสุ่มลูกค้าเข้าอัตโนมัติวันละครั้ง
    // ------------------------------
    void TryAutoRentRoomPerDay()
    {
        // หา "ห้องว่าง" ห้องแรก
        int freeIndex = -1;
        for (int i = 0; i < rooms.Count; i++)
        {
            if (!rooms[i].isRented)
            {
                freeIndex = i;
                break;
            }
        }

        // ถ้าไม่มีห้องว่าง → ไม่ต้องทำอะไร
        if (freeIndex == -1)
            return;

        // สุ่ม 0–1 ถ้าน้อยกว่าหรือเท่าค่า customerSpawnChancePerDay → ให้ลูกค้าเข้าห้อง
        float roll = UnityEngine.Random.value;

        if (roll <= customerSpawnChancePerDay)
        {
            RentRoom(freeIndex);  // ใช้ฟังก์ชันที่เราเขียนไว้แล้ว
            Debug.Log($"[Auto] Customer moved into {rooms[freeIndex].roomName}");
        }
    }


    // ------------------------------
    // บังคับให้ลูกค้าย้ายออก (ใช้ได้ทั้งจากปุ่ม หรือจากระบบสัญญาหมด)
    // ------------------------------
    public void ForceMoveOut(int index)
    {
        if (index < 0 || index >= rooms.Count)
            return;

        Room r = rooms[index];

        r.isRented = false;
        r.rentStartYear = 0;
        r.rentStartMonth = 0;
        r.rentStartDay = 0;

        Debug.Log($"Room {r.roomName} -> ลูกค้าย้ายออก");

    }

    // ------------------------------
    // เช็คสัญญาของทุกห้อง ว่าครบ rentDurationDays รึยัง
    // ถ้าครบ → ย้ายออกอัตโนมัติ
    // ------------------------------
    void CheckRoomContracts()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            Room r = rooms[i];

            if (!r.isRented)
                continue;

            // ถ้ายังไม่มีวันเริ่มต้น (0/0/0) แสดงว่ายังไม่ได้ตั้ง → ข้ามไปก่อน
            if (r.rentStartYear == 0 || r.rentStartMonth == 0 || r.rentStartDay == 0)
                continue;

            int totalDaysPassed = CountDaysPassed(
                r.rentStartYear, r.rentStartMonth, r.rentStartDay,
                gameYear, gameMonth, gameDay
            );

            if (totalDaysPassed >= r.rentDurationDays)
            {
                Debug.Log($"Room {r.roomName} contract expired after {totalDaysPassed} days.");
                ForceMoveOut(i);
            }
        }
    }
    // สร้าง UI ห้องตามจำนวน rooms[]
    void CreateRoomViews()
    {
        // ลบลูกเก่าออกก่อน (กันกรณีมีของเก่า)
        if (roomContainer != null)
        {
            for (int i = roomContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(roomContainer.GetChild(i).gameObject);
            }
        }

        // roomViews = new RoomView[rooms.Count];

        for (int i = 0; i < rooms.Count; i++)
        {
            // สร้าง RoomView ใต้ roomContainer
            RoomView view = Instantiate(roomViewPrefab, roomContainer);
            // roomViews[i] = view;

            // เซ็ตข้อมูลเริ่มต้น
            Sprite sprite = rooms[i].roomSprite;
            bool isRented = rooms[i].isRented;

            view.Setup(sprite, isRented);
        }
    }
    // อัปเดตหน้าตาของห้องห้องเดียว
    // void UpdateRoomView(int index)
    // {
    //     if (roomViews == null) return;
    //     if (index < 0 || index >= roomViews.Length) return;

    //     Sprite sprite = rooms[index].roomSprite;
    //     bool isRented = rooms[index].isRented;

    //     roomViews[index].Setup(sprite, isRented);
    // }
    public void BeginPlacementMode(RoomTypeSO type)
    {
        isPlacingRoom = true;
        roomToPlace = type;
        Debug.Log("Select a slot to place new room");
    }
    // ------------------------------
    // ฟังก์ชันวางห้องลง Slot
    // ------------------------------
    public void TryPlaceRoom(int index)
    {
        if (!isPlacingRoom)
        {
            Debug.Log("Not in placement mode");
            return;
        }

        // เช็คว่า index อยู่ในขอบเขต array
        if (index < 0 || index >= rooms.Count)
        {
            Debug.LogWarning($"TryPlaceRoom index out of range: {index}, rooms.Count = {rooms.Count}");
            return;
        }

        // ถ้าห้องนี้ถูกซื้อแล้วจะมี roomType != null
        if (rooms[index].roomType != null)
        {
            Debug.Log("This slot already has a room!");
            return;
        }

        // เซ็ตข้อมูลห้องใหม่
        rooms[index].roomType = roomToPlace;
        rooms[index].isRented = false;
        rooms[index].rentStartYear = 0;
        rooms[index].rentStartMonth = 0;
        rooms[index].rentStartDay = 0;

        // อัปเดต UI ห้องนั้น
        //UpdateRoomView(index);

        Debug.Log("Room placed at slot " + index);

        // ออกจากโหมดวางห้อง
        isPlacingRoom = false;
        roomToPlace = null;

        SaveData();
    }
    private void GenerateRoomSlots()
    {
        if (roomSlotPrefab == null)
        {
            Debug.LogError("RoomSlotPrefab is NULL!");
            return;
        }
        if (roomContainer == null)
        {
            Debug.LogError("RoomContainer is NULL!");
            return;
        }
        // roomViews = new RoomView[totalRoomSlots];
        rooms = new List<Room>();

        for (int i = 0; i < totalRoomSlots; i++)
        {
            GameObject slot = Instantiate(roomSlotPrefab, roomContainer);
            RoomSlotUI ui = slot.GetComponent<RoomSlotUI>();
            ui.slotIndex = i;
            ui.idleManager = this;

            RoomView view = slot.GetComponent<RoomView>();
            // roomViews[i] = view;

            //rooms[i] = new Room(); // ห้องใหม่ทั้งหมดเริ่มเป็นห้องว่าง
            rooms.Add(new Room());
        }
    }
    // public void BuyRoom(RoomTypeSO type)
    // {
    //     if (currentGold < type.Cost)
    //     {
    //         Debug.Log("Not enough gold.");
    //         return;
    //     }

    //     currentGold -= type.Cost;
    //     roomToPlace = type;
    //     isPlacingRoom = true;

    //     UpdateGoldUI();
    // }

}

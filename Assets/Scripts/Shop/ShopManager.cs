using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject shopPanel;      // Panel ร้านค้า (เปิด/ปิด)
    public Transform shopContent;     // Content ที่เอาไว้ใส่ item
    public ShopItemUI shopItemPrefab; // Prefab ปุ่มไอเทมในร้าน

    [Header("Data")]
    public RoomTypeSO[] roomTypes;    // ประเภทห้องทั้งหมดที่ขายในร้าน

    [Header("Refs")]
    public IdleManager idleManager;               // ลาก IdleManager มาจาก Scene
    public RoomPlacementManager placementManager; // ลาก RoomPlacementManager มาจาก Scene

    void Start()
    {
        GenerateShop();
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    // สร้างปุ่มในร้านค้าให้ครบทุกประเภทห้อง
    void GenerateShop()
    {
        if (shopContent == null || shopItemPrefab == null)
        {
            Debug.LogError("ShopManager: shopContent หรือ shopItemPrefab ยังไม่ถูกเซ็ต");
            return;
        }

        // ล้างลูกเก่าออกก่อน
        for (int i = shopContent.childCount - 1; i >= 0; i--)
        {
            Destroy(shopContent.GetChild(i).gameObject);
        }

        // สร้างปุ่มตาม roomTypes
        foreach (var type in roomTypes)
        {
            ShopItemUI item = Instantiate(shopItemPrefab, shopContent);
            item.data = type;

            if (item.icon != null)
                item.icon.sprite = type.roomSprite;

            if (item.nameText != null)
                item.nameText.text = type.roomName;

            if (item.priceText != null)
                item.priceText.text = type.Cost.ToString("0");

            RoomTypeSO capturedType = type;
            item.buyButton.onClick.AddListener(() => OnClickBuy(capturedType));
        }
    }

    public void OpenShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(true);
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    // เวลากดซื้อห้องจากร้านค้า
    void OnClickBuy(RoomTypeSO type)
    {
        if (idleManager == null || placementManager == null)
        {
            Debug.LogError("ShopManager: ยังไม่ได้เซ็ต idleManager หรือ placementManager ใน Inspector");
            return;
        }

        // กันกรณียังอยู่ในโหมดวางห้องของอันเก่า
        if (placementManager.IsPlacing)
        {
            placementManager.CancelPlacement();
        }

        // หักเงิน (ใช้ SpendGold ของ IdleManager)
        if (!idleManager.SpendGold(type.Cost))
        {
            Debug.Log("Not enough gold");
            return;
        }

        // เข้าสู่โหมดวางห้อง
        placementManager.BeginPlacement(type);

        // ปิดหน้าร้าน
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }
}

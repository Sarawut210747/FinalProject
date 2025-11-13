using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public GameObject shopPanel;
    public Transform shopContent;
    public ShopItemUI shopItemPrefab;

    public RoomTypeSO[] roomTypes;

    public IdleManager idleManager;   // ลากมาจาก scene

    void Start()
    {
        GenerateShop();
    }

    public void GenerateShop()
    {
        foreach (Transform t in shopContent)
            Destroy(t.gameObject);

        foreach (var type in roomTypes)
        {
            var item = Instantiate(shopItemPrefab, shopContent);

            item.data = type;
            item.icon.sprite = type.roomSprite;
            item.nameText.text = type.roomName;
            item.priceText.text = type.Cost.ToString();
            item.buyButton.onClick.AddListener(() =>
            {
                TryBuyRoom(type);
            });
        }
    }

    public void TryBuyRoom(RoomTypeSO type)
    {
        if (idleManager.currentGold < type.Cost)
        {
            Debug.Log("Not enough gold");
            return;
        }

        idleManager.SpendGold(type.Cost);

        // เข้าสู่โหมดวางห้อง
        shopPanel.SetActive(false);

        idleManager.BeginPlacementMode(type);
    }
}

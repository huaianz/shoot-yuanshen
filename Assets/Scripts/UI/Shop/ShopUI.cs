using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : SingleMonoBase<ShopUI>
{
    [Header("主面板")]
    public GameObject shopPanel;
    public TextMeshProUGUI shopNameText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI holdCountText;
    public Transform itemContainer;
    public GameObject shopItemPrefab;
    public Button closeBtn;
    [Header("详情面板")]
    public GameObject detailPanel;
    public Image detailIconImage;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailTypeText;
    public TextMeshProUGUI detailDescText;
    //购买按钮
    public Button buyBtn;
    [Header("消息提示")]
    public TextMeshProUGUI messageText;

    //对象池
    private Queue<GameObject> _itemPool = new Queue<GameObject>();
    //当前正在使用的商品格子
    private List<ShopItemUI> _activeItems = new List<ShopItemUI>();

    //当前选中的商品
    private ShopItem _selectedShopItem;
    private ShopItemUI _selectedItemUI;

    protected override void Awake()
    {
        base.Awake();
        shopPanel.SetActive(false);
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        if (buyBtn != null)
        {
            buyBtn.onClick.AddListener(OnPurchaseButtonClicked);
            buyBtn.interactable = false;
        }
        closeBtn.onClick.AddListener(CloseShop);

        EventHandler.CurrencyUpdateEvent += OnCurrencyUpdate;
        EventHandler.PurchaseSuccessEvent += OnPurchaseSuccess;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventHandler.CurrencyUpdateEvent -= OnCurrencyUpdate;
        EventHandler.PurchaseSuccessEvent -= OnPurchaseSuccess;
        //清理对象池
        ClearPool();
    }

    /// <summary>
    /// 打开商店
    /// </summary>
    /// <param name="shopData"></param>
    public void OpenShop(ShopData_SO shopData)
    {
        if (shopData == null)
        {
            return;
        }
        shopPanel.SetActive(true);
        shopNameText.text = shopData.shopName;
        RefreshCurrency();
        ShowMessage("");
        if (holdCountText != null) holdCountText.text = "";

        //隐藏详情
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
        _selectedShopItem = null;
        _selectedItemUI = null;

        //禁用购买按钮
        if (buyBtn != null)
        {
            buyBtn.interactable = false;
        }
        //清空当前显示的格子
        ClearActiveItems();
        foreach (var shopItem in shopData.shopItems)
        {
            GameObject go = GetFromPool();
            go.transform.SetParent(itemContainer, false);
            go.SetActive(true);

            var ui = go.GetComponent<ShopItemUI>();
            ui.Init(shopItem, this);
            _activeItems.Add(ui);
        }
    }

    /// <summary>
    /// 从对象池获取商品格子
    /// </summary>
    /// <returns></returns>
    private GameObject GetFromPool()
    {
        if (_itemPool.Count > 0)
        {
            return _itemPool.Dequeue();
        }
        return Instantiate(shopItemPrefab);
    }

    /// <summary>
    /// 清空当前激活的格子，收回至对象池
    /// </summary>
    private void ClearActiveItems()
    {
        foreach (var ui in _activeItems)
        {
            //重置格子状态
            ui.gameObject.SetActive(false);
            ui.transform.SetParent(null);

            //放回池子里
            _itemPool.Enqueue(ui.gameObject);
        }
        _activeItems.Clear();
    }


    public void SelectShopItem(ShopItem shopItem, ShopItemUI itemUI)
    {
        _selectedShopItem = shopItem;
        _selectedItemUI = itemUI;

        if (detailPanel == null)
        {
            return;
        }
        detailPanel.SetActive(true);

        //获取物品数据模版
        var weapon = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(shopItem.itemID);
        if (weapon != null)
        {
            detailNameText.text = weapon.weaponName;
            detailTypeText.text = weapon.weaponType;
            detailDescText.text = weapon.weaponDescription;
            if (detailIconImage != null)
            {
                detailIconImage.sprite = InventoryManager.INSTANCE.GetIcon(shopItem.itemID);
            }
        }
        else
        {
            var food = InventoryManager.INSTANCE.foodData?.GetFoodByID(shopItem.itemID);
            if (food != null)
            {
                detailNameText.text = food.foodName;
                detailTypeText.text = "食物";
                detailDescText.text = food.description;
                if (detailIconImage != null)
                {
                    detailIconImage.sprite = InventoryManager.INSTANCE.GetIcon(shopItem.itemID);
                }
            }
        }

        //更新持有数量
        UpdateHoldCount(shopItem.itemID);
        //启用购买按钮
        if (buyBtn != null)
        {
            buyBtn.interactable = true;
        }
    }

    private void OnPurchaseButtonClicked()
    {
        if (_selectedShopItem == null)
        {
            ShowMessage("请先选择一个商品");
            return;
        }
        // 调用 ShopManager 执行购买
        bool success = ShopManager.INSTANCE.BuyItem(_selectedShopItem);
        if (success)
        {
            RefreshCurrency();
            ShowMessage("购买成功！");
            // 刷新所有格子的库存显示
            foreach (var ui in _activeItems)
            {
                ui.UpdateStockDisplay();
            }
            // 刷新持有数量
            UpdateHoldCount(_selectedShopItem.itemID);
            //如果商品已售完，禁用购买按钮
            if (_selectedShopItem.stock == 0)
            {
                buyBtn.interactable = false;
                // 更新格子显示
                _selectedItemUI?.UpdateStockDisplay();
            }
        }
    }

    /// <summary>
    /// 更新当前选中商品持有数量
    /// </summary>
    /// <param name="itemID"></param>
    private void UpdateHoldCount(int itemID)
    {
        if (holdCountText == null) return;
        int count = GetItemHoldCount(itemID);
        holdCountText.text = $"当前持有: {count}";
    }

    /// <summary>
    /// 获取背包中某物品总数量
    /// </summary>
    /// <param name="itemID"></param>
    /// <returns></returns>
    private int GetItemHoldCount(int itemID)
    {
        int count = 0;
        var allItems = InventoryManager.INSTANCE.GetAllItems();
        foreach (var item in allItems)
        {
            if (item.itemID == itemID)
            {
                if (item is FoodItem food)
                    count += food.count;
                else
                    count++;
            }
        }
        return count;
    }
    /// <summary>
    /// 刷新货币
    /// </summary>
    public void RefreshCurrency()
    {
        coinText.text = $"{ShopManager.INSTANCE.GetCurrency("Coin")}";
    }


    public void CloseShop()
    {
        shopPanel.SetActive(false);
        ClearActiveItems();
        detailPanel.SetActive(false);
        EventHandler.CallShopClosedEvent();
    }

    private void OnCurrencyUpdate(string currencyType, int amount)
    {
        if (currencyType == "Coin")
        {
            RefreshCurrency();
        }
    }

    private void OnPurchaseSuccess(int itemID, int amount)
    {
        ShowMessage("购买成功！");
        foreach (var ui in _activeItems)
        {
            ui.UpdateStockDisplay();
        }
        if (_selectedShopItem != null)
        {
            UpdateHoldCount(_selectedShopItem.itemID);
        }
    }

    public void ShowMessage(string msg)
    {
        messageText.text = msg;
    }

    /// <summary>
    /// 清空整个对象池
    /// </summary>
    private void ClearPool()
    {
        while (_itemPool.Count > 0)
        {
            var go = _itemPool.Dequeue();
            if (go != null)
            {
                Destroy(go);
            }
        }
        foreach (var ui in _activeItems)
        {
            if (ui != null && ui.gameObject != null)
            {
                Destroy(ui.gameObject);
            }
        }
        _activeItems.Clear();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : SingleMonoBase<ShopManager>
{
    [Header("商店数据")]
    public List<ShopData_SO> allShops;
    //字典缓存
    private Dictionary<int, ShopData_SO> _shopDict = new Dictionary<int, ShopData_SO>();
    //货币缓存（货币类型/数量）
    private Dictionary<string, int> _playerCurrency = new Dictionary<string, int>();
    private Dictionary<int, int> _purchaseHistory = new Dictionary<int, int>();

    protected override void Awake()
    {
        base.Awake();
        BuildDictionary();
        InitCurrency();

        EventHandler.OpenShopEvent += OpenShop;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventHandler.OpenShopEvent -= OpenShop;
    }

    /// <summary>
    /// 构建商店ID与数据的字典
    /// </summary>
    private void BuildDictionary()
    {
        if (_shopDict == null)
            _shopDict = new Dictionary<int, ShopData_SO>();
        _shopDict.Clear();
        foreach (var shop in allShops)
        {
            if (shop == null)
            {
                continue;
            }
            if (!_shopDict.ContainsKey(shop.shopID))
            {
                _shopDict.Add(shop.shopID, shop);
            }
        }
    }

    /// <summary>
    /// 初始化玩家的货币初始值
    /// </summary>
    private void InitCurrency()
    {
        _playerCurrency["Coin"] = 1000;
        _playerCurrency["Gem"] = 0;
    }

    /// <summary>
    /// 通过商店ID获取对应商店数据
    /// </summary>
    /// <param name="shopID"></param>
    /// <returns></returns>
    public ShopData_SO GetShopData(int shopID)
    {
        _shopDict.TryGetValue(shopID, out var shop);
        return shop;
    }

    /// <summary>
    /// 打开指定ID的商店
    /// </summary>
    /// <param name="shopID"></param>
    public void OpenShop(int shopID)
    {
        var shop = GetShopData(shopID);
        if (shop == null)
        {
            return;
        }
        //打开商店界面
        ShopUI.INSTANCE?.OpenShop(shop);
    }

    /// <summary>
    /// 购买商品
    /// </summary>
    /// <param name="shopItem"></param>
    /// <returns></returns>
    public bool BuyItem(ShopItem shopItem)
    {
        if (shopItem == null)
        {
            return false;
        }
        if (!HasEnoughCurrent(shopItem.currentcyType, shopItem.price))
        {
            ShopUI.INSTANCE?.ShowMessage("货币不足");//UI提示
            return false;
        }

        //检查库存是否充足
        if (shopItem.stock != -1 && shopItem.stock <= 0)
        {
            return false;
        }

        //检查个人限购次数
        if (shopItem.purchaseLimit > 0)
        {
            int purchased = _purchaseHistory.TryGetValue(shopItem.itemID, out int count) ? count : 0;
            if (purchased >= shopItem.purchaseLimit)
            {
                ShopUI.INSTANCE?.ShowMessage("已达购买上限！");
                return false;
            }
        }

        //执行交易
        SpendCurrency(shopItem.currentcyType, shopItem.price);
        //将物品添加到背包
        AddItemToInventory(shopItem.itemID);

        //更新库存
        if (shopItem.stock != -1)
        {
            shopItem.stock--;
        }

        //更新个人限购次数
        if (!_purchaseHistory.ContainsKey(shopItem.itemID))
        {
            _purchaseHistory[shopItem.itemID] = 0;
        }
        _purchaseHistory[shopItem.itemID]++;

        EventHandler.CallPurchaseSuccessEvent(shopItem.itemID, 1);
        EventHandler.CallCurrencyUpdateEvent(shopItem.currentcyType, GetCurrency(shopItem.currentcyType));//货币变动事件

        ShopUI.INSTANCE?.ShowMessage("购买成功！");
        return true;
    }

    /// <summary>
    /// 检查玩家有没有足够的特定货币
    /// </summary>
    /// <param name="currencyType"></param>
    /// <param name="price"></param>
    /// <returns></returns>
    private bool HasEnoughCurrent(string currencyType, int price)
    {
        //检查玩家是否有足够的货币
        return _playerCurrency.TryGetValue(currencyType, out int amount) && amount >= price;
    }

    /// <summary>
    /// 扣除玩家指定类型的货币
    /// </summary>
    /// <param name="currencyType"></param>
    /// <param name="price"></param>
    private void SpendCurrency(string currencyType, int price)
    {
        if (_playerCurrency.ContainsKey(currencyType))
        {
            _playerCurrency[currencyType] -= price;
        }
    }

    /// <summary>
    /// 根据物品ID将物品加入背包
    /// </summary>
    /// <param name="itemID"></param>
    private void AddItemToInventory(int itemID)
    {
        // 先从武器数据中尝试查找该ID
        var weapon = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(itemID);
        if (weapon != null)
        {
            InventoryManager.INSTANCE.AddWeapon(itemID);
            return;
        }

        // 如果没找到武器，再尝试从食物数据中查找
        var food = InventoryManager.INSTANCE.foodData?.GetFoodByID(itemID);
        if (food != null) // 如果成功找到食物数据
        {
            InventoryManager.INSTANCE.AddFood(itemID, 1); // 调用背包管理器添加食物（数量1）
            return;
        }
    }

    /// <summary>
    /// 增加玩家指定类型的货币
    /// </summary>
    public void AddCurrency(string currencyType, int amount)
    {
        if (amount <= 0)
            return;

        _playerCurrency.TryGetValue(currencyType, out int current);
        _playerCurrency[currencyType] = current + amount;

        //通知所有界面刷新货币显示
        EventHandler.CallCurrencyUpdateEvent(currencyType, GetCurrency(currencyType));
    }

    // 获取玩家当前持有的某种货币数量，默认查询硬币
    public int GetCurrency(string currencyType = "Coin")
    {
        //能找到则返回余额，找不到则返回0
        return _playerCurrency.TryGetValue(currencyType, out int amount) ? amount : 0;
    }
}

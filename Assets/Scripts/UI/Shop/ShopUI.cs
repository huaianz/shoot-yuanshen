using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : SingleMonoBase<ShopUI>
{
    [Header("UI组件")]
    public GameObject shopPanel;
    public TextMeshProUGUI shopNameText;
    public TextMeshProUGUI coinText;
    public TextMeshProUGUI messageText;
    public Transform itemContainer;
    public GameObject shopItemPrefab;
    public Button closeBtn;

    private List<ShopItemUI> _itemUIs = new List<ShopItemUI>();

    protected override void Awake()
    {
        base.Awake();
        shopPanel.SetActive(false);
        closeBtn.onClick.AddListener(CloseShop);

        EventHandler.CurrencyUpdateEvent += OnCurrencyUpdate;
        EventHandler.PurchaseSuccessEvent += OnPurchaseSuccess;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventHandler.CurrencyUpdateEvent -= OnCurrencyUpdate;
        EventHandler.PurchaseSuccessEvent -= OnPurchaseSuccess;
    }

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

        ClearItems();
        foreach (var shopItem in shopData.shopItems)
        {
            GameObject go = Instantiate(shopItemPrefab, itemContainer);
            var ui = go.GetComponent<ShopItemUI>();
            ui.Init(shopItem);
            _itemUIs.Add(ui);
        }
    }
    public void RefreshCurrency()
    {
        coinText.text = $"{ShopManager.INSTANCE.GetCurrency("Coin")}";
    }

    public void ClearItems()
    {
        foreach (var ui in _itemUIs)
        {
            Destroy(ui.gameObject);
        }
        _itemUIs.Clear();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        ClearItems();
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
    }

    public void ShowMessage(string msg)
    {
        messageText.text = msg;
    }
}

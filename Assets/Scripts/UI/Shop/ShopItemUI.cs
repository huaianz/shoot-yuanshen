using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI priceText;
    public TextMeshProUGUI stockText;
    public Button buyBtn;
    private ShopItem _shopItem;

    public void Init(ShopItem shopItem)
    {
        _shopItem = shopItem;

        var weapon = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(shopItem.itemID);
        if (weapon != null)
        {
            itemNameText.text = weapon.weaponName;
            itemIcon.sprite = InventoryManager.INSTANCE.GetIcon(shopItem.itemID);
        }
        else
        {
            var food = InventoryManager.INSTANCE.foodData?.GetFoodByID(shopItem.itemID);
            if (food != null)
            {
                itemNameText.text = food.foodName;
                itemIcon.sprite = InventoryManager.INSTANCE.GetIcon(shopItem.itemID);
            }
        }

        priceText.text = $"{shopItem.price} 💰";
        UpdateStockDisplay();

        buyBtn.onClick.AddListener(() =>
        {
            bool success = ShopManager.INSTANCE.BuyItem(_shopItem);
            if (success)
            {
                UpdateStockDisplay();
                ShopUI.INSTANCE?.RefreshCurrency();
            }
        });
    }

    private void UpdateStockDisplay()
    {
        if (_shopItem.stock == -1)
        {
            stockText.text = "无限";
            stockText.color = Color.green;
        }
        else if (_shopItem.stock <= 0)
        {
            stockText.text = "已售罄";
            stockText.color = Color.red;
            buyBtn.interactable = false;
        }
        else
        {
            stockText.text = $"库存: {_shopItem.stock}";
            stockText.color = Color.white;
            buyBtn.interactable = true;
        }
    }
}

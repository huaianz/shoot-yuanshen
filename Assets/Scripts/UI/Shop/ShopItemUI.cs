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
    public TextMeshProUGUI stockText;//库存数
    private ShopItem _shopItem;
    private ShopUI _parentUI;

    public void Init(ShopItem shopItem, ShopUI parent)
    {
        _shopItem = shopItem;
        _parentUI = parent;

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

        //点击格子，选中商品
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                _parentUI.SelectShopItem(_shopItem, this);
            });
        }
    }

    public void UpdateStockDisplay()
    {
        if (_shopItem.stock == -1)
        {
            stockText.text = "无限";
            stockText.color = Color.green;
        }
        else if (_shopItem.stock <= 0)
        {
            stockText.text = "已售完";
            stockText.color = Color.red;
        }
        else
        {
            stockText.text = $"库存: {_shopItem.stock}";
            stockText.color = Color.white;
        }
    }
}

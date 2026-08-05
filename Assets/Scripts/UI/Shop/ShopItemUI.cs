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

    [Tooltip("选中时放大倍率")]
    public float selectedScale = 1.06f;
    private Image _backgroundImage;   // 格子根节点的背景 Image
    private Vector3 _originalScale;   // 原始大小
    private Color _originalColor;     // 原始背景颜色

    private void Awake()
    {
        // 缓存根节点上的背景 Image 和初始大小/颜色
        _backgroundImage = GetComponent<Image>();
        _originalScale = transform.localScale;
        if (_backgroundImage != null)
        {
            _originalColor = _backgroundImage.color;
        }
    }
    public void Init(ShopItem shopItem, ShopUI parent)
    {
        //复用前先回复原状
        SetSelected(false);

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

        priceText.text = $"{shopItem.price} ";
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

    /// <summary>
    /// 设置选中状态
    /// </summary>
    public void SetSelected(bool selected)
    {
        transform.localScale = selected ? Vector3.one * selectedScale : _originalScale;

        if (_backgroundImage != null)
        {
            _backgroundImage.color = selected ? Color.white : _originalColor;
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店里的一行商品
/// </summary>
public class CoopShopItemUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text priceText;
    public Button buyBtn;

    private int _itemId;
    private Action<int> _onBuy;

    public void Bind(int itemId, string title, string desc, int price, Action<int> onBuy)
    {
        _itemId = itemId;
        _onBuy = onBuy;

        if (nameText != null) nameText.text = title;
        if (descText != null) descText.text = desc;
        if (priceText != null) priceText.text = price + " 金币";

        if (buyBtn != null)
        {
            buyBtn.onClick.RemoveAllListeners();
            buyBtn.onClick.AddListener(() => _onBuy?.Invoke(_itemId));
        }
    }

    /// <summary>买不起时只把价格标红, 按钮仍然可点(这样服务器能回"金币不足")</summary>
    public void SetAffordable(bool affordable, int price)
    {
        if (priceText != null)
        {
            priceText.text = price + " 金币";
            priceText.color = affordable
                ? new Color(0.95f, 0.92f, 0.75f)
                : new Color(1f, 0.45f, 0.4f);
        }
    }
}
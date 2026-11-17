using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>购买界面里的一个商品</summary>
public class BuyItemUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public TMP_Text ownedMark;     // 文字标记(如"已拥有")
    public Button button;

    private int _itemId;
    private Action<int> _onClick;

    public void Bind(int itemId, string displayName, int price, Sprite sprite,
                     bool affordable, bool owned, Action<int> onClick)
    {
        _itemId = itemId;
        _onClick = onClick;

        if (nameText != null) nameText.text = displayName;

        if (priceText != null)
        {
            priceText.text = price.ToString();
            priceText.color = affordable
                ? new Color(0.95f, 0.92f, 0.75f)
                : new Color(1f, 0.45f, 0.4f);
        }

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        if (ownedMark != null)
        {
            ownedMark.gameObject.SetActive(owned);
            if (owned) ownedMark.text = "已拥有";
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_itemId));
        }
    }
}

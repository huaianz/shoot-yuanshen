using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店数据资产
/// </summary>
[CreateAssetMenu(fileName = "NewShop", menuName = "Game/ShopData")]
public class ShopData_SO : ScriptableObject
{
    public int shopID;
    public string shopName;
    public List<ShopItem> shopItems;
}

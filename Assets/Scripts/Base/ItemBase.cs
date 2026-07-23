using System;

/// <summary>
/// 背包物品基类
/// </summary>
[Serializable]
public abstract class ItemBase
{
    public string instanceID;//唯一标识符
    public int itemID;
    public bool isNew = true;
    public int ownerID = -1;//拥有者ID
}
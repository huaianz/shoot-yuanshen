using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingleMonoBase<GameManager>
{
    public PlayerModel[] playerModels;

    #region 排序缓存
    private List<ItemBase> _sortedCache = new List<ItemBase>();
    private bool _sortedDirty = true;
    #endregion

    private void Start()
    {

    }
    /// <summary>
    /// 批量删除物品
    /// </summary>
    /// <param name="uids"></param>
    public void DeletePackageItem(List<string> uids)
    {
        foreach (string uid in uids)
        {
            //通过uid批量删除物品
            DeletePackageItem(uid);
        }
        _sortedDirty = true;
    }

    /// <summary>
    /// 批量删除物品（根据 UID 列表）
    /// </summary>
    public void DeletePackageItems(List<string> uids)
    {
        if (uids == null || uids.Count == 0) return;

        foreach (string uid in uids)
        {
            DeletePackageItem(uid);
        }
        // 标记排序脏
        _sortedDirty = true;
        // 自动保存由 InventoryManager 的脏标记处理
    }
    /// <summary>
    /// 删除单个物品
    /// </summary>
    /// <param name="uid"></param>
    public void DeletePackageItem(string uid)
    {
        var item = InventoryManager.INSTANCE.GetItem(uid);
        if (item == null)
        {
            return;
        }
        if (item is WeaponItem weapon)
        {
            //如果武器被持有，则卸下再删除
            if (weapon.ownerID >= 0)
            {
                InventoryManager.INSTANCE.UnequipWeapon(weapon.ownerID);
            }
            InventoryManager.INSTANCE.RemoveItem(uid);
        }
        else if (item is FoodItem food)
        {
            //直接整组删除
            InventoryManager.INSTANCE.ConsumeFood(uid, food.count);
        }

        _sortedDirty = true;

    }

    /// <summary>
    /// 获取排序列表
    /// </summary>
    /// <returns></returns>
    public List<ItemBase> GetSortedItems()
    {
        if (_sortedDirty)
        {
            _sortedCache.Clear();
            //从主仓库获取所有物品
            _sortedCache.AddRange(InventoryManager.INSTANCE.GetAllItems());
            _sortedCache.Sort((a, b) =>
            {
                int weightA = GetItemWeight(a);
                int weightB = GetItemWeight(b);

                int weightCmp = weightB.CompareTo(weightA);
                if (weightCmp != 0)
                {
                    return weightCmp;
                }

                return b.itemID.CompareTo(a.itemID);
            });

            _sortedDirty = false;
        }

        return _sortedCache;
    }

    /// <summary>
    /// 计算权重
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private int GetItemWeight(ItemBase item)
    {
        if (item is WeaponItem weapon)
        {
            var t = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(weapon.itemID);
            return t?.Stars ?? 0;
        }

        else if (item is FoodItem food)
        {
            //DOTO:食物的权重是回血量
            var t = InventoryManager.INSTANCE.foodData?.GetFoodByID(food.itemID);
            return t?.healAmount ?? 0;
        }
        return 0;
    }

    /// <summary>
    /// 迭代器筛选
    /// </summary>
    /// 武器模版和食物模版是不同的类，用object统一返回
    /// <param name="type"></param>
    /// <returns></returns>
    public IEnumerable<object> GetItemsByType(int type)
    {
        //如果是武器
        if (type == GameConst.PackageTypeWeapon)
        {
            var weaponData = InventoryManager.INSTANCE.weaponData;

            if (weaponData != null)
            {
                foreach (var w in weaponData.weaponList)
                {
                    yield return w;
                }
            }
            else if (type == GameConst.PackageTypeFood)
            {
                var foodData = InventoryManager.INSTANCE.foodData;

                if (foodData != null)
                {
                    foreach (var f in foodData.foodList)
                    {
                        yield return f;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 判断是否是未查看的新武器（调用的是InventoryManager里的方法，只是函数名更短）
    /// </summary>
    /// 遵循依赖倒置原则
    /// <param name="itemID"></param>
    /// <returns></returns>
    public bool IsWeaponNew(int itemID)
    {
        return InventoryManager.INSTANCE.HasNewWeaponByItem(itemID);
    }


    /// <summary>
    /// 根据物品id获取物品（合并查询）
    /// </summary>
    /// <param name="itemID"></param>
    /// <returns></returns>
    public object GetItemById(int itemID)
    {
        var weapon = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(itemID);
        if (weapon != null)
            return weapon;
        return InventoryManager.INSTANCE.foodData?.GetFoodByID(itemID);
    }

    /// <summary>
    /// 标记排序脏
    /// </summary>
    public void MarkSortDirty() => _sortedDirty = true;


    public static class GameConst
    {
        public const int PackageTypeWeapon = 1;
        public const int PackageTypeFood = 2;
    }

    #region 按类型获取数据
    public List<ItemBase> GetWeaponItems()
    {
        return new List<ItemBase>(InventoryManager.INSTANCE.GetAllWeapons());
    }
    public List<ItemBase> GetFoodItems()
    {
        return new List<ItemBase>(InventoryManager.INSTANCE.GetAllFoods());
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : SingleMonoBase<InventoryManager>
{
    [Header("数据引用")]
    public WeaponData_SO weaponData;
    public FoodData_SO foodData;

    #region 主储存
    private Dictionary<string, ItemBase> _allItems = new Dictionary<string, ItemBase>();
    #endregion

    #region 哈希集合存储ID
    private HashSet<string> _weaponIds = new HashSet<string>();
    private HashSet<string> _foodIds = new HashSet<string>();
    private HashSet<string> _newItemIds = new HashSet<string>();
    #endregion

    #region 用来查找食物在哪个格子的字典（int是模版id，string是实例id）
    private Dictionary<int, string> _foodToInstanceId = new Dictionary<int, string>();
    #endregion

    #region 记录角色当前装备的武器（角色id,武器id)
    private Dictionary<int, string> _roleWeapon = new Dictionary<int, string>();
    #endregion

    #region 缓存数据
    private List<WeaponItem> _cachedWeaponList = new List<WeaponItem>();
    private List<FoodItem> _cachedFoodList = new List<FoodItem>();
    private List<ItemBase> _cachedAllItemList = new List<ItemBase>();
    private bool _allItemsCacheDirty = true;

    #endregion

    //新武器缓存（模版id,数量）
    private Dictionary<int, int> _newWeaponCount = new Dictionary<int, int>();
    #region 把加载的武器或者食物图片缓存
    private Dictionary<int, Sprite> _iconCache = new Dictionary<int, Sprite>();
    #endregion

    #region 存档状态

    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using MessagePack;

public class InventoryManager : SingleMonoBase<InventoryManager>
{
    [Header("数据引用")]
    public WeaponData_SO weaponData;
    public FoodData_SO foodData;
    public MaterialData_SO materialData;


    #region 主储存
    private Dictionary<string, ItemBase> _allItems = new Dictionary<string, ItemBase>();
    #endregion

    #region 哈希集合存储ID
    public HashSet<string> _weaponIds = new HashSet<string>();
    public HashSet<string> _foodIds = new HashSet<string>();
    public HashSet<string> _materialIds = new HashSet<string>();
    private HashSet<string> _newItemIds = new HashSet<string>();
    #endregion

    #region 用来查找食物在哪个格子的字典（int是模版id，string是实例id）
    private Dictionary<int, string> _foodToInstanceId = new Dictionary<int, string>();
    private Dictionary<int, string> _materialToInstanceId = new Dictionary<int, string>();
    #endregion

    #region 记录角色当前装备的武器（角色id,武器id)
    private Dictionary<int, string> _roleWeapon = new Dictionary<int, string>();
    #endregion

    #region 缓存数据
    private List<WeaponItem> _cachedWeaponList = new List<WeaponItem>();
    private List<FoodItem> _cachedFoodList = new List<FoodItem>();
    private List<MaterialItem> _cachedMaterialList = new List<MaterialItem>();
    private List<ItemBase> _cachedAllItems = new List<ItemBase>();
    private bool _allItemsCacheDirty = true;
    private Dictionary<int, string> _itemNameCache = new Dictionary<int, string>();
    #endregion

    //新武器缓存（模版id,数量）
    private Dictionary<int, int> _newWeaponCount = new Dictionary<int, int>();
    #region 把加载的武器或者食物图片缓存
    private Dictionary<int, Sprite> _iconCache = new Dictionary<int, Sprite>();
    #endregion

    #region 存档状态
    private bool _isDirty = false;//是否改动
    private bool _isSaving = false;//是否正在存档
    private float _saveTimer = 0f;//存档计时器
    private string _savePath;//存档路径
    #endregion


    override protected void Awake()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            Destroy(gameObject);
            return;
        }
        INSTANCE = this;
        #region 游戏开始时加载存档
        _savePath = Path.Combine(Application.persistentDataPath, "inventory.bin");
        LoadData();
        #endregion
    }

    private void Update()
    {
        #region 定时存档
        _saveTimer += Time.deltaTime;
        if (_saveTimer >= 5f && _isDirty && !_isSaving)
        {
            _saveTimer = 0f;
            SaveData();
        }
        #endregion
    }

    #region 切换后台或者关闭游戏强制存档
    private void OnApplicationPause(bool pause)
    {
        if (pause && _isDirty && !_isSaving)
        {
            SaveData();
        }
    }

    private void OnApplicationQuit()
    {
        if (_isDirty && !_isSaving)
        {
            SaveData();
        }
    }
    #endregion

    #region 核心方法
    /// <summary>
    /// 往背包里加武器
    /// </summary>
    /// <param name="itemID">武器资源ID</param>
    public void AddWeapon(int itemID)
    {
        var item = weaponData.GetWeaponByID(itemID);
        if (item == null)
        {
            return;
        }
        var newWeapon = new WeaponItem
        {
            instanceID = Guid.NewGuid().ToString("N"),
            itemID = itemID,
            isNew = true,
            ownerID = -1
        };

        _allItems[newWeapon.instanceID] = newWeapon;
        _weaponIds.Add(newWeapon.instanceID);
        _newItemIds.Add(newWeapon.instanceID);

        if (!_newWeaponCount.ContainsKey(itemID))
        {
            _newWeaponCount[itemID] = 0;
        }

        _newWeaponCount[itemID]++;

        _isDirty = true;
        _allItemsCacheDirty = true;
        EventHandler.CallInventoryChangedEvent();
    }

    /// <summary>
    /// 往背包里加食物
    /// </summary>
    /// <param name="itemID">食物模版id</param>
    /// <param name="amount">添加数量</param>
    public void AddFood(int itemID, int amount)
    {
        //先看看有没有这个id的食物模版
        var item = foodData.GetFoodByID(itemID);
        if (item == null)
        {
            return;
        }
        //看食物存储里有没有这个食物物品，有的话把这个食物的实例id取出来
        if (_foodToInstanceId.TryGetValue(itemID, out string existId))
        {
            //根据这个实例id核对是不是食物，是的话就将它命名为food并全部取出来
            if (_allItems.TryGetValue(existId, out var baseItem) && baseItem is FoodItem food)
            {
                int total = food.count + amount;
                food.count = Mathf.Min(total, item.maxStack);
                if (total > item.maxStack)
                {
                    //TODO:溢出部分正常情况下分到下一个格子，这里暂时丢弃
                }
                _isDirty = true;
                _allItemsCacheDirty = true;
                EventHandler.CallInventoryChangedEvent();
                return;
            }
            else
            {
                _foodToInstanceId.Remove(itemID);
            }
        }

        var newFood = new FoodItem
        {
            instanceID = Guid.NewGuid().ToString("N"),
            itemID = itemID,
            count = Mathf.Min(amount, item.maxStack),
            isNew = true,
            ownerID = -1,
        };

        _allItems[newFood.instanceID] = newFood;
        _foodIds.Add(newFood.instanceID);
        _newItemIds.Add(newFood.instanceID);
        _foodToInstanceId[itemID] = newFood.instanceID;

        _isDirty = true;
        _allItemsCacheDirty = true;
        EventHandler.CallInventoryChangedEvent();
    }

    /// <summary>
    /// 消耗食物
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="amount"></param>
    public void ConsumeFood(string instanceID, int amount)
    {
        if (!_allItems.TryGetValue(instanceID, out var baseitem))
        {
            return;
        }
        if (!(baseitem is FoodItem food))
        {
            return;
        }
        //修正食物持有者，因为食物没有持有者
        if (food.ownerID != -1)
        {
            food.ownerID = -1;
        }

        food.count -= amount;
        if (food.count <= 0)
        {
            _allItems.Remove(instanceID);
            _foodIds.Remove(instanceID);
            _newItemIds.Remove(instanceID);
            //DOTO：溢出部分也需要修改
            if (_foodToInstanceId.TryGetValue(food.itemID, out string mappedId) && mappedId == instanceID)
            {
                _foodToInstanceId.Remove(food.itemID);
            }
        }

        _isDirty = true;
        _allItemsCacheDirty = true;
        EventHandler.CallInventoryChangedEvent();
    }

    #region 素材方法
    /// <summary>
    /// 往背包里加素材
    /// </summary>
    /// <param name="itemID"></param>
    /// <param name="amount"></param>
    public void AddMaterial(int itemID, int amount)
    {
        var item = materialData.GetMaterialByID(itemID);
        if (item == null)
        {
            return;
        }

        // 已有同种素材就叠加数量
        if (_materialToInstanceId.TryGetValue(itemID, out string existId))
        {
            if (_allItems.TryGetValue(existId, out var baseItem) && baseItem is MaterialItem material)
            {
                material.count = Mathf.Min(material.count + amount, item.maxStack);
                _isDirty = true;
                _allItemsCacheDirty = true;
                return;
            }
            _materialToInstanceId.Remove(itemID);
        }

        var newMaterial = new MaterialItem
        {
            instanceID = Guid.NewGuid().ToString("N"),
            itemID = itemID,
            count = Mathf.Min(amount, item.maxStack),
            isNew = true,
            ownerID = -1,
        };

        _allItems[newMaterial.instanceID] = newMaterial;
        _materialIds.Add(newMaterial.instanceID);
        _newItemIds.Add(newMaterial.instanceID);
        _materialToInstanceId[itemID] = newMaterial.instanceID;

        _isDirty = true;
        _allItemsCacheDirty = true;
    }

    /// <summary>
    /// 消耗素材
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="amount"></param>
    public void ConsumeMaterial(string instanceID, int amount)
    {
        if (!_allItems.TryGetValue(instanceID, out var baseItem))
        {
            return;
        }
        if (!(baseItem is MaterialItem material))
        {
            return;
        }

        material.count -= amount;
        if (material.count <= 0)
        {
            _allItems.Remove(instanceID);
            _materialIds.Remove(instanceID);
            _newItemIds.Remove(instanceID);
            if (_materialToInstanceId.TryGetValue(material.itemID, out string mappedId) && mappedId == instanceID)
            {
                _materialToInstanceId.Remove(material.itemID);
            }
        }

        _isDirty = true;
        _allItemsCacheDirty = true;
        EventHandler.CallInventoryChangedEvent();
    }

    /// <summary>
    /// 获取背包里某素材的总数量
    /// </summary>
    public int GetMaterialCount(int itemID)
    {
        if (_materialToInstanceId.TryGetValue(itemID, out string existId) &&
            _allItems.TryGetValue(existId, out var item) && item is MaterialItem m)
        {
            return m.count;
        }
        return 0;
    }
    #endregion

    /// <summary>
    /// 强制删除任意物品
    /// </summary>
    /// <param name="instanceID"></param>
    public void RemoveItem(string instanceID)
    {
        if (!_allItems.TryGetValue(instanceID, out var item))
        {
            return;
        }
        if (item is WeaponItem weapon)
        {
            if (weapon.ownerID >= 0)
            {
                //先从角色身上卸载下来
                UnequipWeapon(weapon.ownerID);
            }
            //如果是新武器，就从新武器计数里减一
            if (weapon.isNew && _newWeaponCount.TryGetValue(weapon.itemID, out int cnt) && cnt > 0)
            {
                _newWeaponCount[weapon.itemID] -= 1;
            }
            _allItems.Remove(instanceID);
            _weaponIds.Remove(instanceID);
            if (weapon.isNew)
            {
                _newItemIds.Remove(instanceID);
            }
        }
        else if (item is FoodItem food)
        {
            _allItems.Remove(instanceID);
            _foodIds.Remove(instanceID);
            if (food.isNew)
            {
                _newItemIds.Remove(instanceID);
            }
            if (_foodToInstanceId.TryGetValue(food.itemID, out string mappedId) && mappedId == instanceID)
            {
                _foodToInstanceId.Remove(food.itemID);
            }
        }
        else if (item is MaterialItem material)
        {
            _allItems.Remove(instanceID);
            _materialIds.Remove(instanceID);
            if (material.isNew)
            {
                _newItemIds.Remove(instanceID);
            }
            if (_materialToInstanceId.TryGetValue(material.itemID, out string mappedMaterialId) && mappedMaterialId == instanceID)
            {
                _materialToInstanceId.Remove(material.itemID);
            }
        }
        else
        {
            return;
        }

        _isDirty = true;
        _allItemsCacheDirty = true;
        EventHandler.CallInventoryChangedEvent();
    }

    public string GetItemName(int itemID)
    {
        if (_itemNameCache.TryGetValue(itemID, out string cachedName))
        {
            return cachedName;
        }
        string name = null;
        var weapon = weaponData?.GetWeaponByID(itemID);
        if (weapon != null)
        {
            name = weapon.weaponName;
        }
        else
        {
            var food = foodData?.GetFoodByID(itemID);
            if (food != null)
            {
                name = food.foodName;
            }
            else
            {
                var material = materialData?.GetMaterialByID(itemID);
                if (material != null)
                {
                    name = material.materialName;
                }
            }
        }
        _itemNameCache[itemID] = name;
        return name;
    }
    #endregion

    #region 装备系统
    /// <summary>
    /// 角色更换武器
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="targetRoleID"></param>
    public void EquipWeapon(string instanceID, int targetRoleID)
    {
        if (!_allItems.TryGetValue(instanceID, out var baseItem))
        {
            return;
        }
        if (!(baseItem is WeaponItem weapon))
        {
            return;
        }

        int currentOwner = weapon.ownerID;
        //如果当前持有者不是-1且不是目标角色，就把之前的持有者标记擦除
        if (currentOwner != -1 && currentOwner != targetRoleID)
        {
            if (_roleWeapon.TryGetValue(currentOwner, out string oldId) && oldId == instanceID)
            {
                _roleWeapon.Remove(currentOwner);
            }
        }

        //如果目标角色已经有武器，就把之前的武器擦除
        if (_roleWeapon.TryGetValue(targetRoleID, out string oldWeaponId))
        {
            if (_allItems.TryGetValue(oldWeaponId, out var oldItem))
            {
                oldItem.ownerID = -1;
            }
            _roleWeapon.Remove(targetRoleID);
        }

        weapon.ownerID = targetRoleID;
        _roleWeapon[targetRoleID] = instanceID;

        if (weapon.isNew)
        {
            weapon.isNew = false;
            _newItemIds.Remove(instanceID);
            if (_newWeaponCount.TryGetValue(weapon.itemID, out int cnt) && cnt > 0)
            {
                _newWeaponCount[weapon.itemID] = cnt - 1;
            }
            _allItemsCacheDirty = true;
        }
        _isDirty = true;
    }

    /// <summary>
    /// 按角色ID来卸载武器
    /// </summary>
    /// <param name="roleID"></param>
    public void UnequipWeapon(int roleID)
    {
        if (!_roleWeapon.TryGetValue(roleID, out string instanceID))
        {
            return;
        }
        if (_allItems.TryGetValue(instanceID, out var item))
        {
            item.ownerID = -1;
            _roleWeapon.Remove(roleID);
            _isDirty = true;
        }
        else
        {
            _roleWeapon.Remove(roleID);
        }
    }

    /// <summary>
    /// 按实例ID来卸载武器
    /// </summary>
    /// <param name="instanceID"></param>
    public void UnequipWeaponByInstance(string instanceID)
    {
        if (!_allItems.TryGetValue(instanceID, out var item))
        {
            return;
        }
        if (!(item is WeaponItem))
        {
            return;
        }
        if (item.ownerID < 0)
        {
            return;
        }
        if (_roleWeapon.TryGetValue(item.ownerID, out string equippedId) && equippedId == instanceID)
        {
            _roleWeapon.Remove(item.ownerID);
        }
        item.ownerID = -1;
        _isDirty = true;
    }
    #endregion

    #region 查询接口
    /// <summary>
    /// 根据角色id来查询武器
    /// </summary>
    /// <param name="roleID"></param>
    /// <returns></returns>
    public string GetRoleWeaponId(int roleID)
    {
        _roleWeapon.TryGetValue(roleID, out string id);
        return id;
    }

    /// <summary>
    /// 根据角色id拿到武器，如果主仓库有这个物品，将它转换成武器物品
    /// </summary>
    /// <param name="roleID"></param>
    /// <returns></returns>
    public WeaponItem GetRoleWeaponData(int roleID)
    {
        string id = GetRoleWeaponId(roleID);
        //为空则报空
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        return _allItems.TryGetValue(id, out var item) ? item as WeaponItem : null;
    }

    /// <summary>
    /// 根据角色ID获取佩戴武器的攻击力
    /// </summary>
    /// <param name="roleID"></param>
    /// <returns></returns>
    public int GetRoleWeaponATK(int roleID)
    {
        var weapon = GetRoleWeaponData(roleID);
        if (weapon == null)
        {
            return 0;
        }
        var item = weaponData.GetWeaponByID(weapon.itemID);
        //不为空则获取攻击力并返回
        return item?.weaponATK ?? 0;
    }

    /// <summary>
    /// 获取新物品数量
    /// </summary>
    /// <returns></returns>
    public int GetNewItemCount() => _newItemIds.Count;

    /// <summary>
    /// 根据武器模版ID查询是否是新武器
    /// </summary>
    /// <param name="itemID"></param>
    /// <returns></returns>
    public bool HasNewWeaponByItem(int itemID)
    {
        return _newWeaponCount.TryGetValue(itemID, out int count) && count > 0;
    }

    /// <summary>
    /// 刷新武器展示柜
    /// </summary>
    /// <returns></returns>
    public List<WeaponItem> GetAllWeapons()
    {
        //先清空展示柜
        _cachedWeaponList.Clear();
        //然后遍历武器标签册里的每一个ID，去主仓库里拿出武器来展示
        foreach (var id in _weaponIds)
        {
            if (_allItems.TryGetValue(id, out var item) && item is WeaponItem w)
            {
                _cachedWeaponList.Add(w);
            }
        }
        return _cachedWeaponList;
    }

    /// <summary>
    /// 刷新食物展示柜
    /// </summary>
    /// <returns></returns>
    public List<FoodItem> GetAllFoods()
    {
        _cachedFoodList.Clear();
        foreach (var id in _foodIds)
        {
            if (_allItems.TryGetValue(id, out var item) && item is FoodItem f)
            {
                _cachedFoodList.Add(f);
            }
        }
        return _cachedFoodList;
    }

    /// <summary>
    /// 获取所有素材
    /// </summary>
    public List<MaterialItem> GetAllMaterials()
    {
        _cachedMaterialList.Clear();
        foreach (var id in _materialIds)
        {
            if (_allItems.TryGetValue(id, out var item) && item is MaterialItem m)
            {
                _cachedMaterialList.Add(m);
            }
        }
        return _cachedMaterialList;
    }

    /// <summary>
    /// 把整个背包导出成 JSON(上传到服务器用)
    /// </summary>
    public string ExportToCloudJson()
    {
        var data = new CloudInventoryData();

        foreach (var id in _weaponIds)
        {
            if (_allItems.TryGetValue(id, out var it) && it is WeaponItem w)
                data.items.Add(new CloudItemData { type = "Weapon", itemID = w.itemID, count = 1 });
        }
        foreach (var id in _foodIds)
        {
            if (_allItems.TryGetValue(id, out var it) && it is FoodItem f)
                data.items.Add(new CloudItemData { type = "Food", itemID = f.itemID, count = f.count });
        }
        foreach (var id in _materialIds)
        {
            if (_allItems.TryGetValue(id, out var it) && it is MaterialItem m)
                data.items.Add(new CloudItemData { type = "Material", itemID = m.itemID, count = m.count });
        }

        return JsonUtility.ToJson(data);
    }

    /// <summary>
    /// 从服务器 JSON 恢复背包。
    /// 规则: 服务器是空背包就不覆盖本地(避免第一次登录清空你的东西);
    /// 服务器有内容就用服务器覆盖本地。
    /// </summary>
    public void ImportFromCloudJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        CloudInventoryData data;
        try
        {
            data = JsonUtility.FromJson<CloudInventoryData>(json);
        }
        catch
        {
            return;
        }
        if (data == null || data.items == null || data.items.Count == 0) return;

        // 清空本地(和读档逻辑一致)
        _allItems.Clear();
        _weaponIds.Clear();
        _foodIds.Clear();
        _materialIds.Clear();
        _newItemIds.Clear();
        _foodToInstanceId.Clear();
        _materialToInstanceId.Clear();
        _roleWeapon.Clear();
        _newWeaponCount.Clear();
        _allItemsCacheDirty = true;

        // 逐条重建
        foreach (var item in data.items)
        {
            switch (item.type)
            {
                case "Weapon":
                    AddWeapon(item.itemID);
                    break;
                case "Food":
                    AddFood(item.itemID, item.count);
                    break;
                case "Material":
                    AddMaterial(item.itemID, item.count);
                    break;
            }
        }
    }

    /// <summary>
    /// 获取所有物品
    /// </summary>
    /// <returns></returns>
    public List<ItemBase> GetAllItems()
    {
        if (_allItemsCacheDirty)
        {
            _cachedAllItems.Clear();
            foreach (var id in _weaponIds)
            {
                _cachedAllItems.Add(_allItems[id]);
            }
            foreach (var id in _foodIds)
            {
                _cachedAllItems.Add(_allItems[id]);
            }
            foreach (var id in _materialIds)
            {
                _cachedAllItems.Add(_allItems[id]);
            }

            _allItemsCacheDirty = false;
        }
        return _cachedAllItems;
    }

    /// <summary>
    /// 根据实例ID获取物品
    /// </summary>
    /// <param name="instanceID"></param>
    /// <returns></returns>
    public ItemBase GetItem(string instanceID)
    {
        _allItems.TryGetValue(instanceID, out var item);
        return item;
    }

    /// <summary>
    /// 手动标记物品不为新（已查看）
    /// </summary>
    /// <param name="instanceID"></param>
    public void MarkAsViewed(string instanceID)
    {
        if (_allItems.TryGetValue(instanceID, out var item) && item.isNew)
        {
            item.isNew = false;
            _newItemIds.Remove(instanceID);
            if (item is WeaponItem)
            {
                int tid = item.itemID;
                if (_newWeaponCount.TryGetValue(tid, out int cnt) && cnt > 0)
                {
                    _newWeaponCount[tid] = cnt - 1;
                }
            }
            _isDirty = true;
            _allItemsCacheDirty = true;
        }
    }
    #endregion

    #region 图标加载
    /// <summary>
    /// 获取图标
    /// </summary>
    /// <param name="itemID"></param>
    /// <returns></returns>
    public Sprite GetIcon(int itemID)
    {
        //先从缓存里查找有没有对应图片
        if (_iconCache.TryGetValue(itemID, out Sprite cached))
        {
            return cached;
        }
        Sprite loaded = null;
        var weapon = weaponData?.GetWeaponByID(itemID);
        //武器存在且有图标路径，则获取图片
        if (weapon != null && !string.IsNullOrEmpty(weapon.iconPath))
        {
            loaded = Resources.Load<Sprite>(weapon.iconPath);
        }

        if (loaded == null)
        {
            var food = foodData?.GetFoodByID(itemID);
            if (food != null && !string.IsNullOrEmpty(food.iconPath))
            {
                loaded = Resources.Load<Sprite>(food.iconPath);
            }
        }
        if (loaded == null)
        {
            var material = materialData?.GetMaterialByID(itemID);
            if (material != null && !string.IsNullOrEmpty(material.iconPath))
            {
                loaded = Resources.Load<Sprite>(material.iconPath);
            }
        }

        //对应路径能找到图片的话存入缓存
        if (loaded != null)
        {
            _iconCache[itemID] = loaded;
        }
        else
        {
            Debug.LogWarning($"找不到物品{itemID}的图标");
        }

        return loaded;
    }
    #endregion

    #region 异步存档
    /// <summary>
    /// 异步存盘
    /// </summary>
    // private async void SaveData()
    // {
    //     if (!_isDirty || _isSaving)
    //     {
    //         return;
    //     }
    //     _isDirty = false;
    //     _isSaving = true;

    //     var list = _allItems.Values.ToList();
    //     byte[] data = MessagePackSerializer.Serialize(list);

    //     await Task.Run(() =>
    //     {
    //         using (var fs = new FileStream(_savePath, FileMode.Create))
    //         {
    //             fs.Write(data, 0, data.Length);
    //         }
    //     });

    //     _isSaving = false;
    // }


    private async void SaveData()
    {
        if (!_isDirty || _isSaving) return;
        _isDirty = false;
        _isSaving = true;

        // 构建可序列化的数据
        SaveData1 saveData = new SaveData1();
        foreach (var kvp in _allItems)
        {
            var item = kvp.Value;
            SerializableItem serializable = new SerializableItem
            {
                instanceID = item.instanceID,
                templateID = item.itemID,
                isNew = item.isNew,
                ownerID = item.ownerID,
            };

            if (item is WeaponItem)
            {
                serializable.type = "Weapon";
                serializable.count = 0;
            }
            else if (item is FoodItem food)
            {
                serializable.type = "Food";
                serializable.count = food.count;
            }
            else if (item is MaterialItem material)
            {
                serializable.type = "Material";
                serializable.count = material.count;
            }
            saveData.items.Add(serializable);
        }

        // 序列化为 JSON
        string json = JsonUtility.ToJson(saveData, true);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

        await Task.Run(() =>
        {
            using (var fs = new FileStream(_savePath, FileMode.Create))
            {
                fs.Write(data, 0, data.Length);
            }
        });

        _isSaving = false;
    }

    private void LoadData()
    {
        if (!File.Exists(_savePath)) return;

        try
        {
            byte[] rawData = File.ReadAllBytes(_savePath);
            string json = System.Text.Encoding.UTF8.GetString(rawData);
            SaveData1 saveData = JsonUtility.FromJson<SaveData1>(json);

            if (saveData == null || saveData.items == null)
            {
                Debug.LogWarning("读档数据为空");
                return;
            }

            // 清空现有数据
            _allItems.Clear();
            _weaponIds.Clear();
            _foodIds.Clear();
            _newItemIds.Clear();
            _foodToInstanceId.Clear();
            _materialIds.Clear();
            _materialToInstanceId.Clear();
            _roleWeapon.Clear();
            _newWeaponCount.Clear();
            _allItemsCacheDirty = true;

            foreach (var serializable in saveData.items)
            {
                ItemBase item;
                if (serializable.type == "Weapon")
                {
                    item = new WeaponItem
                    {
                        instanceID = serializable.instanceID,
                        itemID = serializable.templateID,
                        isNew = serializable.isNew,
                        ownerID = serializable.ownerID
                    };
                }
                else if (serializable.type == "Food")
                {
                    item = new FoodItem
                    {
                        instanceID = serializable.instanceID,
                        itemID = serializable.templateID,
                        isNew = serializable.isNew,
                        ownerID = -1,  // 食物强制 -1
                        count = serializable.count
                    };
                }
                else if (serializable.type == "Material")
                {
                    item = new MaterialItem
                    {
                        instanceID = serializable.instanceID,
                        itemID = serializable.templateID,
                        isNew = serializable.isNew,
                        ownerID = -1,
                        count = serializable.count
                    };
                }
                else
                {
                    continue;
                }

                _allItems[item.instanceID] = item;

                if (item is WeaponItem)
                {
                    _weaponIds.Add(item.instanceID);
                    if (item.ownerID >= 0)
                        _roleWeapon[item.ownerID] = item.instanceID;
                    if (item.isNew)
                    {
                        int tid = item.itemID;
                        if (!_newWeaponCount.ContainsKey(tid))
                            _newWeaponCount[tid] = 0;
                        _newWeaponCount[tid]++;
                    }
                }
                else if (item is FoodItem food)
                {
                    _foodIds.Add(item.instanceID);
                    if (!_foodToInstanceId.ContainsKey(item.itemID))
                        _foodToInstanceId[item.itemID] = item.instanceID;
                }
                else if (item is MaterialItem material)
                {
                    _materialIds.Add(item.instanceID);
                    if (!_materialToInstanceId.ContainsKey(item.itemID))
                        _materialToInstanceId[item.itemID] = item.instanceID;
                }
                if (item.isNew)
                    _newItemIds.Add(item.instanceID);
            }

            Debug.Log($"读档成功！共加载 {_allItems.Count} 个物品");
        }
        catch (Exception e)
        {
            Debug.LogError($"读档失败：{e.Message}");
        }
    }

    /// <summary>
    /// 读档
    /// </summary>
    // private void LoadData()
    // {
    //     if (!File.Exists(_savePath))
    //     {
    //         return;
    //     }

    //     try
    //     {
    //         byte[] rawData = File.ReadAllBytes(_savePath);
    //         var list = MessagePackSerializer.Deserialize<List<ItemBase>>(rawData);

    //         _allItems.Clear();
    //         _weaponIds.Clear();
    //         _foodIds.Clear();
    //         _newItemIds.Clear();
    //         _foodToInstanceId.Clear();
    //         _roleWeapon.Clear();
    //         _newWeaponCount.Clear();
    //         _allItemsCacheDirty = true;

    //         foreach (var item in list)
    //         {
    //             //食物修正持有者
    //             if (item is FoodItem && item.ownerID != -1)
    //             {
    //                 item.ownerID = -1;
    //             }
    //             _allItems[item.instanceID] = item;
    //             if (item is WeaponItem)
    //             {
    //                 _weaponIds.Add(item.instanceID);
    //                 if (item.ownerID >= 0)
    //                 {
    //                     _roleWeapon[item.ownerID] = item.instanceID;
    //                 }
    //                 if (item.isNew)
    //                 {
    //                     int tid = item.itemID;
    //                     if (!_newWeaponCount.ContainsKey(tid))
    //                     {
    //                         _newWeaponCount[tid] = _newWeaponCount[tid]++;
    //                     }
    //                 }
    //             }
    //             else if (item is FoodItem food)
    //             {
    //                 _foodIds.Add(item.instanceID);
    //                 if (!_foodToInstanceId.ContainsKey(item.itemID))
    //                 {
    //                     _foodToInstanceId[item.itemID] = item.instanceID;
    //                 }
    //             }

    //             if (item.isNew)
    //             {
    //                 _newItemIds.Add(item.instanceID);
    //             }
    //         }
    //         Debug.Log($"读档成功！共加载 {_allItems.Count} 个物品");
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.LogError($"读档失败：{e.Message}");
    //     }
    // }

    [System.Serializable]
    public class SerializableItem
    {
        public string type;          // "Weapon" 或 "Food"
        public string instanceID;
        public int templateID;
        public bool isNew;
        public int ownerID;
        public int count;            // 仅食物使用，武器为0
    }


    [System.Serializable]
    public class SaveData1
    {
        public List<SerializableItem> items = new List<SerializableItem>();
    }
    #endregion
}

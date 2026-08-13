using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;


public class GameManager : SingleMonoBase<GameManager>
{
    public PlayerModel[] playerModels;

    [Header("角色数据")]
    public PlayerData_SO playerData_SO;
    [Header("死亡处理")]
    [Tooltip("战斗场景名")]
    public string combatSceneName = "BattleMap";
    //复活点: 目前是游戏开始时的玩家位置
    private Vector3 _respawnPosition;
    private Quaternion _respawnRotation;
    private bool _respawnReady;

    private Dictionary<int, RoleRuntimeData> _roleDataDict = new Dictionary<int, RoleRuntimeData>();
    private int _currentActiveRoleID = -1;
    //角色切换时通知UI的事件
    public System.Action<int> OnActiveRoleChanged;
    //缓存列表
    private List<RoleRuntimeData> _cachedRoleList = new List<RoleRuntimeData>();

    private Dictionary<int, Sprite> _avatarCache = new Dictionary<int, Sprite>();
    #region 排序缓存
    private List<ItemBase> _sortedCache = new List<ItemBase>();
    private bool _sortedDirty = true;
    #endregion

    private void Start()
    {
        //初始化角色系统
        InitRoles();
        //记录安全区复活点
        if (PlayerController.INSTANCE != null && PlayerController.INSTANCE.currentPlayerModel != null)
        {
            _respawnPosition = PlayerController.INSTANCE.currentPlayerModel.transform.position;
            _respawnRotation = PlayerController.INSTANCE.currentPlayerModel.transform.rotation;
            _respawnReady = true;
        }

        // 预热三个自动创建的 UI(懒加载单例, 需要第一次调用才会创建)
        _ = LowHealthUI.Instance;                 // 残血红闪
        _ = QuestTrackerUI.Instance;              // 委托追踪
        _ = RegionBannerUI.Instance;              // 地区提示
        _ = LoginUI.Instance;  // 登录界面
        _ = CloudSaveManager.Instance;
        RegionBannerUI.ShowRegion("安全区");      // 开局先显示一次地区

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
        public const int PackageTypeMaterial = 3;
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
    public List<ItemBase> GetMaterialItems()
    {
        return new List<ItemBase>(InventoryManager.INSTANCE.GetAllMaterials());
    }
    #endregion

    #region 角色管理
    /// <summary>
    /// 初始化所有角色
    /// </summary>
    public void InitRoles()
    {
        _roleDataDict.Clear();
        if (playerData_SO == null)
        {
            return;
        }
        foreach (var character in playerData_SO.characterList)
        {
            var runtime = new RoleRuntimeData(character);
            if (character.weaponID > 0)
            {
                runtime.equippedWeaponId = character.weaponID.ToString();
            }
            _roleDataDict[character.characterID] = runtime;
        }

        //默认第一个角色上阵
        if (_roleDataDict.Count > 0)
        {
            var first = _roleDataDict.Values.GetEnumerator();
            first.MoveNext();
            SetActiveRole(first.Current.roleID);
        }

    }

    /// <summary>
    /// 设置当前上阵角色
    /// </summary>
    /// <param name="roleID"></param>
    public void SetActiveRole(int roleID)
    {
        if (_currentActiveRoleID == roleID)
        {
            return;
        }
        _currentActiveRoleID = roleID;
        MarkRoleStatsDirty(roleID);
        OnActiveRoleChanged?.Invoke(roleID);
    }

    public int GetActiveRoleID()
    {
        return _currentActiveRoleID;
    }


    /// <summary>
    /// 获取指定角色的运行时的数据
    /// </summary>
    /// <param name="roleID"></param>
    /// <returns></returns>
    public RoleRuntimeData GetRoleData(int roleID)
    {
        _roleDataDict.TryGetValue(roleID, out var data);
        return data;
    }

    /// <summary>
    /// 获取所有角色运行时的数据
    /// </summary>
    /// <returns></returns>
    public List<RoleRuntimeData> GetAllRoles()
    {
        _cachedRoleList.Clear();
        _cachedRoleList.AddRange(_roleDataDict.Values);
        return _cachedRoleList;
    }

    /// <summary>
    /// 标记角色数据为脏数据
    /// </summary>
    /// <param name="roleID"></param>
    public void MarkRoleStatsDirty(int roleID)
    {
        if (_roleDataDict.TryGetValue(roleID, out var data))
        {
            data.isDirty = true;
        }
    }
    /// <summary>
    /// 刷新指定角色数据
    /// </summary>
    /// <param name="roleID"></param>
    public void RefreshRoleStats(int roleID)
    {
        var data = GetRoleData(roleID);
        if (data == null || !data.isDirty)
        {
            return;
        }
        string weaponId = InventoryManager.INSTANCE.GetRoleWeaponId(roleID);
        WeaponItem weapon = null;
        if (!string.IsNullOrEmpty(weaponId))
        {
            weapon = InventoryManager.INSTANCE.GetItem(weaponId) as WeaponItem;
        }
        var (attack, defense, moveSpeed, maxHealth, maxArmor) = RoleStatsCalculator.CalculateFinalStats(data.baseData, weapon);
        data.finalAttack = attack;
        data.finalDefense = defense;
        data.finalMoveSpeed = moveSpeed;
        data.finalMaxHealth = maxHealth;
        data.finalMaxArmor = maxArmor;
        data.equippedWeaponId = weaponId;
        data.isDirty = false;
    }


    public Sprite GetAvatar(int characterID)
    {
        if (_avatarCache.TryGetValue(characterID, out Sprite cached))
        {
            return cached;
        }
        var role = GetRoleData(characterID);
        if (role == null || string.IsNullOrEmpty(role.baseData.avatarPath))
        {
            return null;
        }
        Sprite loaded = Resources.Load<Sprite>(role.baseData.avatarPath);
        if (loaded != null)
        {
            _avatarCache[characterID] = loaded;
        }
        return loaded;
    }
    #endregion
    /// <summary>
    /// 对当前上阵角色造成伤害
    /// </summary>
    /// <param name="damage"></param>
    public void ApplyDamageToActiveRole(float damage)
    {
        if (_currentActiveRoleID < 0)
        {
            return;
        }
        var data = GetRoleData(_currentActiveRoleID);
        if (data == null || data.currentHealth <= 0)
        {
            return;
        }
        data.currentHealth = Mathf.Max(0f, data.currentHealth - damage);

        EventHandler.CallPlayerHealthChangedEvent(data.roleID, data.currentHealth, data.finalMaxHealth);
        //死亡: 回安全区
        if (data.currentHealth <= 0f)
        {
            EventHandler.CallPlayerDiedEvent();
            StartCoroutine(HandlePlayerDeath(data));
        }

    }

    /// <summary>
    /// 死亡处理: 等1.5秒 -> 卸载战斗场景 -> 传回安全区 -> 回满血 -> 提示
    /// </summary>
    private IEnumerator HandlePlayerDeath(RoleRuntimeData data)
    {
        yield return new WaitForSeconds(1.5f);

        //卸载战斗场景
        if (!string.IsNullOrEmpty(combatSceneName))
        {
            Scene combat = SceneManager.GetSceneByName(combatSceneName);
            if (combat.isLoaded)
            {
                SceneManager.UnloadSceneAsync(combat);
            }
        }

        //传回安全区复活点
        PlayerController player = PlayerController.INSTANCE;
        if (player != null)
        {
            Vector3 target = _respawnReady ? _respawnPosition : player.transform.position;
            Quaternion targetRot = _respawnReady ? _respawnRotation : player.transform.rotation;

            player.transform.position = target;
            player.transform.rotation = targetRot;

            if (player.currentPlayerModel != null)
            {
                CharacterController cc = player.currentPlayerModel.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.currentPlayerModel.transform.position = target;
                player.currentPlayerModel.transform.rotation = targetRot;
                NavMeshAgent agent = player.currentPlayerModel.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                if (cc != null) cc.enabled = true;
            }

            if (GameManager.INSTANCE.playerModels != null)
            {
                foreach (PlayerModel m in GameManager.INSTANCE.playerModels)
                {
                    if (m == null || m == player.currentPlayerModel) continue;
                    CharacterController cc2 = m.GetComponent<CharacterController>();
                    if (cc2 != null) cc2.enabled = false;
                    m.transform.position = target;
                    m.transform.rotation = targetRot;
                    NavMeshAgent ag = m.GetComponent<NavMeshAgent>();
                    if (ag != null)
                    {
                        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                        {
                            m.transform.position = hit.position;
                        }
                        ag.enabled = true;
                        ag.Warp(m.transform.position);
                        ag.isStopped = false;
                    }
                    if (cc2 != null) cc2.enabled = true;
                }
            }
        }

        //回满血
        data.currentHealth = data.finalMaxHealth;
        EventHandler.CallPlayerHealthChangedEvent(data.roleID, data.currentHealth, data.finalMaxHealth);
        // 回城时显示地区提示
        RegionBannerUI.ShowRegion("安全区");
        //提示
        ToastUI.ShowMessage("你阵亡了，已返回安全区休整", new Color(1f, 0.5f, 0.3f));
    }
}

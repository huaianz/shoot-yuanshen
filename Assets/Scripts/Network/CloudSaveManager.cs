using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 云存档管理器: 登录后下载, 变化打脏标记, 5秒节流上传。
/// 下载的数据会等到游戏场景(管理器)就绪后再应用。
/// </summary>
public class CloudSaveManager : MonoBehaviour
{
    private static CloudSaveManager _instance;
    public static CloudSaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CloudSaveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CloudSaveManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CloudSaveManager>();
                }
            }
            return _instance;
        }
    }

    [Tooltip("上传节流间隔(秒)")]
    public float saveInterval = 5f;

    private float _timer;
    private bool _dirty;

    // 待应用的数据(登录时下载, 等游戏场景就绪)
    private int? _pendingCoin;
    private string _pendingInventory;
    private string _pendingRoleData;   // 新增: 待应用的角色等级经验

    private void OnEnable()
    {
        EventHandler.CurrencyUpdateEvent += OnCurrencyChanged;
        EventHandler.InventoryChangedEvent += OnInventoryChanged;
        EventHandler.ExpChangedEvent += OnExpChanged;      // 新增: 经验变化 -> 打脏
        EventHandler.RoleLevelUpEvent += OnLevelUp;        // 新增: 升级 -> 打脏
        GameClient.Instance.OnLoginResult += OnLoginResult;
        GameClient.Instance.OnPlayerDataResult += OnPlayerDataResult;
    }

    private void OnDisable()
    {
        EventHandler.CurrencyUpdateEvent -= OnCurrencyChanged;
        EventHandler.InventoryChangedEvent -= OnInventoryChanged;
        EventHandler.ExpChangedEvent -= OnExpChanged;
        EventHandler.RoleLevelUpEvent -= OnLevelUp;
        GameClient.Instance.OnLoginResult -= OnLoginResult;
        GameClient.Instance.OnPlayerDataResult -= OnPlayerDataResult;
    }

    private void Update()
    {
        // 1. 有下载数据待应用, 且管理器就绪 -> 应用
        TryApplyPending();

        // 2. 脏标记 + 节流上传
        if (!_dirty) return;
        if (!GameClient.Instance.IsLoggedIn) return;

        _timer += Time.deltaTime;
        if (_timer >= saveInterval)
        {
            _timer = 0f;
            Upload();
        }
    }

    private void OnCurrencyChanged(string currencyType, int amount)
    {
        if (currencyType == "Coin") _dirty = true;
    }

    private void OnInventoryChanged()
    {
        _dirty = true;
    }

    private void OnExpChanged(int roleID, int curExp, int expToNext) => _dirty = true;

    private void OnLevelUp(int roleID, int newLevel) => _dirty = true;

    private void OnLoginResult(LoginResult r)
    {
        if (!r.success) return;
        _dirty = false;
        _timer = 0f;
        GameClient.Instance.GetPlayerData();
    }

    private void OnPlayerDataResult(PlayerDataResult r)
    {
        if (!r.success) return;
        // 先缓存, 等游戏场景加载后再套用
        _pendingCoin = r.coin;
        _pendingInventory = r.inventoryJson;
        _pendingRoleData = r.roleDataJson;   // 新增
        TryApplyPending();
    }

    /// <summary>
    /// 应用下载的数据(三个管理器都存在时才执行, 防止游戏还没初始化)
    /// </summary>
    private void TryApplyPending()
    {
        if (!_pendingCoin.HasValue) return;
        if (ShopManager.INSTANCE == null || InventoryManager.INSTANCE == null || GameManager.INSTANCE == null) return;

        ShopManager.INSTANCE.SetCurrency("Coin", _pendingCoin.Value);
        InventoryManager.INSTANCE.ImportFromCloudJson(_pendingInventory);
        ImportRoleDataJson(_pendingRoleData);   // 新增: 应用角色等级经验

        _dirty = false;
        _timer = 0f;
        // 演示: 导入后补武器, 并重新标记脏, 确保5秒后会上传到服务器
        // InventoryManager.INSTANCE.EnsureAllWeaponsInBag();
        // _dirty = true;
        // ToastUI.ShowMessage($"云存档同步完成（金币 {_pendingCoin.Value}）", new Color(0.4f, 1f, 0.5f));

        _pendingCoin = null;
        _pendingInventory = null;
        _pendingRoleData = null;   // 新增
    }

    /// <summary>
    /// 把下载的角色数据写进运行时, 并刷新属性/血条/经验条
    /// </summary>
    private void ImportRoleDataJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        RoleSaveList list = JsonUtility.FromJson<RoleSaveList>(json);
        if (list == null || list.roles == null) return;

        foreach (var entry in list.roles)
        {
            var role = GameManager.INSTANCE.GetRoleData(entry.roleID);
            if (role == null) continue;

            role.roleLevel = Mathf.Max(1, entry.level);   // 覆盖为存档等级
            role.roleExp = Mathf.Max(0, entry.exp);       // 覆盖为存档经验

            // 等级变化 -> 重算属性, 并刷新血条上限
            GameManager.INSTANCE.MarkRoleStatsDirty(role.roleID);
            GameManager.INSTANCE.RefreshRoleStats(role.roleID);
            EventHandler.CallPlayerHealthChangedEvent(role.roleID, role.currentHealth, role.finalMaxHealth);
        }

        // 刷新当前角色的经验条
        var active = GameManager.INSTANCE.GetRoleData(GameManager.INSTANCE.GetActiveRoleID());
        if (active != null)
        {
            EventHandler.CallExpChangedEvent(active.roleID, active.roleExp, GameManager.INSTANCE.GetExpToNextLevel(active.roleLevel));
        }
    }

    private void Upload()
    {
        if (ShopManager.INSTANCE == null || InventoryManager.INSTANCE == null) return;

        int coin = ShopManager.INSTANCE.GetCurrency("Coin");
        string inventoryJson = InventoryManager.INSTANCE.ExportToCloudJson();
        string roleDataJson = ExportRoleDataJson();   // 新增
        GameClient.Instance.SavePlayerData(coin, inventoryJson, roleDataJson);
    }

    /// <summary>外部主动触发保存(返回主菜单/退出游戏前调用, 让脏数据马上上传)</summary>
    public void UploadNow()
    {
        _timer = saveInterval;   // 下次 Update 立即达到节流时间 -> 触发上传
    }


    /// <summary>
    /// 把所有角色的等级/经验序列化成 JSON
    /// </summary>
    private string ExportRoleDataJson()
    {
        var list = new RoleSaveList();
        foreach (var role in GameManager.INSTANCE.GetAllRoles())
        {
            list.roles.Add(new RoleSaveEntry { roleID = role.roleID, level = role.roleLevel, exp = role.roleExp });
        }
        return JsonUtility.ToJson(list);
    }
}

/// <summary>角色存档条目</summary>
[System.Serializable]
public class RoleSaveEntry
{
    public int roleID;
    public int level;
    public int exp;
}

/// <summary>角色存档列表(JsonUtility 不支持直接序列化 List, 需要包装类)</summary>
[System.Serializable]
public class RoleSaveList
{
    public List<RoleSaveEntry> roles = new List<RoleSaveEntry>();
}
using UnityEngine;

/// <summary>
/// 云存档管理器: 登录后自动下载金币+背包, 变化打脏标记, 每5秒节流上传。
/// 性能: 只有变化才传(脏标记), 合并成5秒一次, 事件驱动零轮询。
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

    private void OnEnable()
    {
        EventHandler.CurrencyUpdateEvent += OnCurrencyChanged;
        EventHandler.InventoryChangedEvent += OnInventoryChanged;   // 新增: 背包变化
        GameClient.Instance.OnLoginResult += OnLoginResult;
        GameClient.Instance.OnPlayerDataResult += OnPlayerDataResult;
    }

    private void OnDisable()
    {
        EventHandler.CurrencyUpdateEvent -= OnCurrencyChanged;
        EventHandler.InventoryChangedEvent -= OnInventoryChanged;
        GameClient.Instance.OnLoginResult -= OnLoginResult;
        GameClient.Instance.OnPlayerDataResult -= OnPlayerDataResult;
    }

    private void Update()
    {
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

    /// <summary>
    /// 背包变化 -> 打脏标记
    /// </summary>
    private void OnInventoryChanged()
    {
        _dirty = true;
    }

    private void OnLoginResult(LoginResult r)
    {
        if (!r.success) return;
        _dirty = false;
        _timer = 0f;
        GameClient.Instance.GetPlayerData();
    }

    /// <summary>
    /// 收到服务器数据 -> 覆盖金币 + 恢复背包
    /// </summary>
    private void OnPlayerDataResult(PlayerDataResult r)
    {
        if (!r.success) return;

        if (ShopManager.INSTANCE != null)
        {
            ShopManager.INSTANCE.SetCurrency("Coin", r.coin);
        }
        if (InventoryManager.INSTANCE != null)
        {
            // 服务器有背包就恢复; 空背包保留本地(导入方法内部判断)
            InventoryManager.INSTANCE.ImportFromCloudJson(r.inventoryJson);
        }

        _dirty = false;
        _timer = 0f;
    }

    /// <summary>
    /// 上传金币+背包到服务器
    /// </summary>
    private void Upload()
    {
        if (ShopManager.INSTANCE == null || InventoryManager.INSTANCE == null) return;

        int coin = ShopManager.INSTANCE.GetCurrency("Coin");
        string inventoryJson = InventoryManager.INSTANCE.ExportToCloudJson();
        GameClient.Instance.SavePlayerData(coin, inventoryJson);
    }
}
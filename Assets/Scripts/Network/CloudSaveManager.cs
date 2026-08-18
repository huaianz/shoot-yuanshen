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

    private void OnEnable()
    {
        EventHandler.CurrencyUpdateEvent += OnCurrencyChanged;
        EventHandler.InventoryChangedEvent += OnInventoryChanged;
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
        TryApplyPending();
    }

    /// <summary>
    /// 应用下载的数据(ShopManager/InventoryManager 存在时才执行)
    /// </summary>
    private void TryApplyPending()
    {
        if (!_pendingCoin.HasValue) return;
        if (ShopManager.INSTANCE == null || InventoryManager.INSTANCE == null) return;

        ShopManager.INSTANCE.SetCurrency("Coin", _pendingCoin.Value);
        InventoryManager.INSTANCE.ImportFromCloudJson(_pendingInventory);

        _dirty = false;
        _timer = 0f;
        // 演示: 导入后补武器, 并重新标记脏, 确保5秒后会上传到服务器
        // InventoryManager.INSTANCE.EnsureAllWeaponsInBag();
        // _dirty = true;
        // ToastUI.ShowMessage($"云存档同步完成（金币 {_pendingCoin.Value}）", new Color(0.4f, 1f, 0.5f));

        _pendingCoin = null;
        _pendingInventory = null;
    }

    private void Upload()
    {
        if (ShopManager.INSTANCE == null || InventoryManager.INSTANCE == null) return;

        int coin = ShopManager.INSTANCE.GetCurrency("Coin");
        string inventoryJson = InventoryManager.INSTANCE.ExportToCloudJson();
        GameClient.Instance.SavePlayerData(coin, inventoryJson);
    }
}
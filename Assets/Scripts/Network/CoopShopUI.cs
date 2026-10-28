using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局内商店: 按配置生成商品行, 购买由服务器裁决
/// </summary>
public class CoopShopUI : MonoBehaviour
{
    [Serializable]
    public class ShopEntry
    {
        public int itemId;
        public string title;
        public string desc;
        public int price;
    }

    [Header("面板")]
    public GameObject panelRoot;
    public TMP_Text coinText;
    public TMP_Text hpText;

    [Header("列表")]
    public Transform itemContainer;
    public CoopShopItemUI itemPrefab;

    [Header("商品配置")]
    public ShopEntry[] entries;

    [Header("关闭")]
    public Button closeBtn;
    public KeyCode toggleKey = KeyCode.B;

    private readonly List<CoopShopItemUI> _rows = new List<CoopShopItemUI>();
    private bool _blocking;   // 是否已经进入 UI 锁定状态(防止计数泄漏)

    private void Awake()
    {
        if (closeBtn != null) closeBtn.onClick.AddListener(Close);
        BuildRows();
    }

    private void OnEnable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnBuyResult += HandleBuyResult;
    }

    private void OnDisable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnBuyResult -= HandleBuyResult;

        // 面板可能被别的逻辑直接关掉, 这里兜底把 UI 锁定退回来
        ReleaseUIBlock();
    }

    private void Update()
    {
        if (panelRoot == null) return;

        if (!panelRoot.activeSelf)
        {
            // 面板被外部隐藏时, 把 UI 锁定退回来(否则光标会一直显示)
            if (_blocking) ReleaseUIBlock();
        }
        else
        {
            // 掉线或离开房间时自动收起
            if (!CanUseShop())
            {
                Close();
            }
            else
            {
                RefreshText();
            }
        }

        if (Input.GetKeyDown(toggleKey)) Toggle();
    }

    /// <summary>只生成一次, 之后复用</summary>
    private void BuildRows()
    {
        if (itemPrefab == null || itemContainer == null) return;
        if (entries == null || entries.Length == 0) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var row = Instantiate(itemPrefab, itemContainer);
            row.gameObject.SetActive(true);
            row.Bind(entries[i].itemId, entries[i].title, entries[i].desc, entries[i].price, OnClickBuy);
            _rows.Add(row);
        }
    }

    private void OnClickBuy(int itemId)
    {
        if (BattleClient.Instance == null) return;
        BattleClient.Instance.SendBuy(itemId);
    }

    public void Open()
    {
        string reason = ShopBlockReason();
        if (reason != null)
        {
            ToastUI.ShowMessage(reason, new Color(1f, 0.65f, 0.35f));
            return;
        }

        if (panelRoot != null) panelRoot.SetActive(true);

        if (!_blocking)
        {
            UIManager.EnterUIBlock();
            _blocking = true;
        }

        RefreshText();

        if (CountAliveEnemies() > 0)
            ToastUI.ShowMessage("战斗中无法购买，清空本波后再来", new Color(1f, 0.6f, 0.35f));
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        ReleaseUIBlock();
    }

    public void Toggle()
    {
        if (panelRoot == null) return;

        if (panelRoot.activeSelf)
        {
            Close();
            return;
        }

        string reason = ShopBlockReason();
        if (reason != null)
        {
            ToastUI.ShowMessage(reason, new Color(1f, 0.65f, 0.35f));
            return;
        }

        // 别的界面开着时不要叠加弹出
        if (UIManager.IsAnyUIOpen && !_blocking) return;

        Open();
    }

    /// <summary>只有真正进入联机房间、并且战斗服连上了, 才能用补给站</summary>
    private bool CanUseShop()
    {
        return ShopBlockReason() == null;
    }

    /// <summary>返回不能用补给站的原因, null 表示可以用</summary>
    private string ShopBlockReason()
    {
        if (!RoomSession.InRoom) return "请先进入联机房间";

        var bc = BattleClient.Instance;
        if (bc == null || !bc.IsConnected || bc.MyPlayerId <= 0)
            return "正在连接战斗服，请稍候";

        return null;
    }

    /// <summary>客户端只用来做提示, 是否允许购买仍由服务器判断</summary>
    private int CountAliveEnemies()
    {
        var bc = BattleClient.Instance;
        if (bc == null || bc.LatestSnapshot == null || bc.LatestSnapshot.enemies == null) return 0;

        int n = 0;
        var list = bc.LatestSnapshot.enemies;
        for (int i = 0; i < list.Count; i++)
            if (list[i].hp > 0) n++;
        return n;
    }

    private void RefreshText()
    {
        var bc = BattleClient.Instance;
        if (bc == null) return;

        if (coinText != null) coinText.text = "金币  " + bc.MatchCoins;
        if (hpText != null) hpText.text = "生命  " + bc.MyServerHp;

        if (entries == null) return;
        for (int i = 0; i < _rows.Count && i < entries.Length; i++)
            _rows[i].SetAffordable(bc.MatchCoins >= entries[i].price, entries[i].price);
    }

    private void ReleaseUIBlock()
    {
        if (!_blocking) return;

        _blocking = false;

        // 场景切换时 UIManager 可能已经重置过计数, 这时再减会变成负数
        if (UIManager.IsAnyUIOpen) UIManager.ExitUIBlock();
    }

    private void HandleBuyResult(BuyResultDto dto)
    {
        if (dto.success)
        {
            ToastUI.ShowMessage(
                $"{dto.msg}   -{dto.price} 金币",
                new Color(0.5f, 1f, 0.6f));
        }
        else
        {
            ToastUI.ShowMessage(dto.msg, new Color(1f, 0.5f, 0.4f));
        }

        RefreshText();
    }
}

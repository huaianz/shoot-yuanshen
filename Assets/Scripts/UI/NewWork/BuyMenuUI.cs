using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>战术模式购买界面: 三个标签 + 商品列表 + 右侧详情 + 购买按钮</summary>
public class BuyMenuUI : MonoBehaviour
{
    [Serializable]
    public class BuyEntry
    {
        public int itemId;
        public string displayName;
        public int price;
        public Sprite icon;
        public string category;   // weapon / armor / supply
    }

    [Header("面板")]
    public GameObject panelRoot;
    public TMP_Text moneyText;
    public TMP_Text timerText;
    public Transform itemContainer;
    public BuyItemUI itemPrefab;
    public Button closeBtn;

    [Header("标签")]
    public Button weaponTabBtn;
    public Button armorTabBtn;
    public Button supplyTabBtn;

    [Header("右侧详情")]
    public GameObject detailPanel;
    public TMP_Text detailNameText;
    public TMP_Text detailPriceText;
    public TMP_Text detailDescText;
    public Button detailBuyBtn;

    [Header("商品配置")]
    public BuyEntry[] entries;

    public KeyCode toggleKey = KeyCode.B;

    private readonly List<BuyItemUI> _rows = new List<BuyItemUI>();
    private readonly Dictionary<int, string> _rowCategories = new Dictionary<int, string>();
    private string _tab = "weapon";
    private int _selected = -1;
    private string _selectedCategory;
    private int _timeLeftMs;
    private bool _inBuyPhase;
    private bool _blocking;
    private bool _subscribed;

    private void Awake()
    {
        if (closeBtn != null) closeBtn.onClick.AddListener(HidePanel);
        if (weaponTabBtn != null) weaponTabBtn.onClick.AddListener(() => SetTab("weapon"));
        if (armorTabBtn != null) armorTabBtn.onClick.AddListener(() => SetTab("armor"));
        if (supplyTabBtn != null) supplyTabBtn.onClick.AddListener(() => SetTab("supply"));
        if (detailBuyBtn != null) detailBuyBtn.onClick.AddListener(BuySelected);
    }

    private void OnEnable() { TrySubscribe(); }
    private void Start() { TrySubscribe(); }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null) return;
        BattleClient.Instance.OnRoundState += HandleRound;
        BattleClient.Instance.OnBuyItemResult += HandleBuyResult;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (_subscribed && BattleClient.Instance != null)
        {
            BattleClient.Instance.OnRoundState -= HandleRound;
            BattleClient.Instance.OnBuyItemResult -= HandleBuyResult;
        }
        _subscribed = false;
        ReleaseUIBlock();
    }

    private void Update()
    {
        TrySubscribe();

        if (!_inBuyPhase) return;

        if (Input.GetKeyDown(toggleKey)) TogglePanel();

        if (panelRoot != null && panelRoot.activeSelf)
            Refresh();
    }

    private void HandleRound(RoundStateDto dto)
    {
        if (dto == null) return;
        _timeLeftMs = dto.timeLeftMs;

        if (dto.phase == 1)
        {
            if (!_inBuyPhase) BeginBuyPhase();
        }
        else if (_inBuyPhase)
        {
            EndBuyPhase();
        }
    }

    private void BeginBuyPhase()
    {
        _inBuyPhase = true;
        _selected = -1;
        _selectedCategory = null;

        if (!_blocking)
        {
            UIManager.EnterUIBlock();
            _blocking = true;
        }

        if (panelRoot != null) panelRoot.SetActive(true);
        SetTab(_tab);
        ShowDetail(null);
    }

    private void EndBuyPhase()
    {
        _inBuyPhase = false;

        HidePanel();
        ReleaseUIBlock();
    }

    private void TogglePanel()
    {
        if (panelRoot == null) return;

        bool show = !panelRoot.activeSelf;
        panelRoot.SetActive(show);

        if (show) SetTab(_tab);
    }

    /// <summary>只隐藏界面, 不解除输入锁定(准备阶段仍不能移动)</summary>
    private void HidePanel()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void ReleaseUIBlock()
    {
        if (!_blocking) return;

        _blocking = false;
        if (UIManager.IsAnyUIOpen) UIManager.ExitUIBlock();
    }

    private void SetTab(string category)
    {
        bool changed = _tab != category;
        if (changed)
        {
            _selected = -1;
            _selectedCategory = null;
        }
        _tab = category;
        RebuildGrid();
        if (changed)
            ShowDetail(null);
    }

    private void RebuildGrid()
    {
        if (itemContainer == null || itemPrefab == null) return;

        _rowCategories.Clear();
        int used = 0;

        if (_tab == "weapon")
        {
            WeaponData_SO weaponData = InventoryManager.INSTANCE != null
                ? InventoryManager.INSTANCE.weaponData
                : null;
            if (weaponData != null && weaponData.weaponList != null)
            {
                foreach (Weapon weapon in weaponData.weaponList)
                {
                    if (weapon == null) continue;
                    BuyEntry entry = new BuyEntry
                    {
                        itemId = weapon.weaponID,
                        displayName = weapon.weaponName,
                        price = weapon.matchPrice,
                        icon = LoadWeaponIcon(weapon),
                        category = "weapon"
                    };
                    BindEntry(entry, ref used);
                }
            }
        }
        else if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                BuyEntry entry = entries[i];
                if (entry.category != _tab) continue;
                BindEntry(entry, ref used);
            }
        }

        for (int i = used; i < _rows.Count; i++)
            _rows[i].gameObject.SetActive(false);
    }

    private void BindEntry(BuyEntry entry, ref int used)
    {
        if (used >= _rows.Count)
            _rows.Add(Instantiate(itemPrefab, itemContainer));

        BuyItemUI ui = _rows[used];
        used++;
        ui.gameObject.SetActive(true);
        _rowCategories[entry.itemId] = entry.category;

        bool configured = IsConfigured(entry);
        ui.Bind(entry.itemId, entry.displayName, entry.price, entry.icon,
            configured && BattleClient.Instance.MatchCoins >= entry.price,
            IsOwned(entry),
            OnItemClicked);
    }

    private void OnItemClicked(int itemId)
    {
        _selected = itemId;
        _selectedCategory = _rowCategories.TryGetValue(itemId, out string category)
            ? category
            : _tab;

        BuyEntry entry = Find(itemId);
        ShowDetail(entry);
    }

    private void ShowDetail(BuyEntry entry)
    {
        bool show = entry != null;

        if (detailPanel != null) detailPanel.SetActive(show);
        if (!show) return;

        if (detailNameText != null) detailNameText.text = entry.displayName;
        if (detailPriceText != null) detailPriceText.text = entry.price + " 金币";
        if (detailDescText != null) detailDescText.text = Describe(entry);

        if (detailBuyBtn != null)
            detailBuyBtn.interactable = IsConfigured(entry)
                && BattleClient.Instance.MatchCoins >= entry.price;
    }

    private void BuySelected()
    {
        if (_selected < 0) return;
        BattleClient.Instance.SendBuyItem(_selected, _selectedCategory ?? _tab);
    }

    private void HandleBuyResult(BuyItemResultDto dto)
    {
        if (dto == null) return;

        ToastUI.ShowMessage(dto.msg,
            dto.success ? new Color(0.6f, 1f, 0.7f) : new Color(1f, 0.6f, 0.4f));

        RebuildGrid();
        if (_selected >= 0) ShowDetail(Find(_selected));
    }

    private void Refresh()
    {
        if (BattleClient.Instance == null) return;

        if (moneyText != null) moneyText.text = BattleClient.Instance.MatchCoins.ToString();

        if (timerText != null)
        {
            int sec = Mathf.CeilToInt(_timeLeftMs / 1000f);
            timerText.text = $"{sec / 60:00}:{sec % 60:00}";
        }

        if (_selected >= 0 && detailBuyBtn != null)
        {
            var e = Find(_selected);
            if (e != null)
                detailBuyBtn.interactable = IsConfigured(e)
                    && BattleClient.Instance.MatchCoins >= e.price;
        }
    }

    private bool IsOwned(BuyEntry entry)
    {
        if (BattleClient.Instance == null) return false;

        if (entry.category == "armor")
        {
            int armor = BattleClient.Instance.MyArmor;
            if (entry.itemId == 1) return armor >= 25;
            if (entry.itemId == 2) return armor >= 50;
        }
        if (entry.category == "weapon")
            return BattleClient.Instance.MyMatchWeaponId == entry.itemId;

        return false;
    }

    private BuyEntry Find(int itemId)
    {
        string category = string.IsNullOrEmpty(_selectedCategory) ? _tab : _selectedCategory;
        if (entries != null)
        {
            foreach (var entry in entries)
                if (entry.itemId == itemId && entry.category == category)
                    return entry;
        }

        if (category == "weapon")
        {
            Weapon weapon = InventoryManager.INSTANCE != null
                ? InventoryManager.INSTANCE.weaponData?.GetWeaponByID(itemId)
                : null;
            if (weapon != null)
            {
                return new BuyEntry
                {
                    itemId = weapon.weaponID,
                    displayName = weapon.weaponName,
                    price = weapon.matchPrice,
                    icon = LoadWeaponIcon(weapon),
                    category = "weapon"
                };
            }
        }
        return null;
    }

    private static string Describe(BuyEntry entry)
    {
        switch (entry.category)
        {
            case "armor" when entry.itemId == 1:
                return "半甲 25，对战时先扣护甲再扣生命";
            case "armor" when entry.itemId == 2:
                return "全甲 50，对战时先扣护甲再扣生命";
            case "supply" when entry.itemId == 3:
                return "立刻回复 50 点生命";
        }

        Weapon template = InventoryManager.INSTANCE != null
            ? InventoryManager.INSTANCE.weaponData?.GetWeaponByID(entry.itemId)
            : null;
        if (template == null)
            return "";

        if (template.matchDamage <= 0 || template.matchPrice <= 0
            || template.matchFireIntervalMs <= 0)
            return "该武器尚未填写完整对局数据";

        float shotsPerSecond = 1000f / template.matchFireIntervalMs;
        return $"对局武器模板：{template.weaponName}\n" +
               $"对局伤害：{template.matchDamage}\n" +
               $"射速：{shotsPerSecond:F1} 发/秒";
    }

    private static bool IsConfigured(BuyEntry entry)
    {
        if (entry.category != "weapon")
            return true;

        Weapon weapon = InventoryManager.INSTANCE != null
            ? InventoryManager.INSTANCE.weaponData?.GetWeaponByID(entry.itemId)
            : null;
        return weapon != null
            && weapon.matchDamage > 0
            && weapon.matchPrice > 0
            && weapon.matchFireIntervalMs > 0;
    }

    private static Sprite LoadWeaponIcon(Weapon weapon)
    {
        return weapon != null && !string.IsNullOrEmpty(weapon.iconPath)
            ? Resources.Load<Sprite>(weapon.iconPath)
            : null;
    }
}

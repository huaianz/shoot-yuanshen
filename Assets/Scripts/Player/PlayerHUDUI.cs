using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUDUI : MonoBehaviour
{
    [Tooltip("血条填充图片")]
    public Image healthSlider;
    [Tooltip("血量文本,显示 当前/总血量")]
    public TextMeshProUGUI healthText;

    [Header("战术模式护甲")]
    [Tooltip("护甲图标/护甲组根节点，只在战术对局显示")]
    public GameObject armorRoot;
    [Tooltip("护甲填充图片，可留空")]
    public Image armorFill;
    [Tooltip("护甲数值文本")]
    public TextMeshProUGUI armorText;

    [Header("战术数值上限")]
    public float tacticalMaxHealth = 100f;
    public float tacticalMaxArmor = 50f;

    //右下角弹药显示
    private TextMeshProUGUI _ammoText;
    private bool _battleSubscribed;
    private float _lastHealth = -1f;
    private float _lastMaxHealth = -1f;
    private float _lastArmor = -1f;
    private float _lastMaxArmor = -1f;

    private void Awake()
    {
        EnsureAmmoText();
    }

    private void OnEnable()
    {
        // 订阅血量变化事件
        EventHandler.PlayerHealthChangedEvent += OnPlayerHealthChanged;
        // 订阅弹药变化事件
        EventHandler.AmmoChangedEvent += OnAmmoChanged;
        TrySubscribeBattle();
        RefreshDisplay();
        RefreshAmmoFromCurrentWeapon();
    }

    private void OnDisable()
    {
        EventHandler.PlayerHealthChangedEvent -= OnPlayerHealthChanged;
        EventHandler.AmmoChangedEvent -= OnAmmoChanged;
        UnsubscribeBattle();
    }

    private void Start()
    {
        TrySubscribeBattle();
        // 切换角色事件在 Start 订阅
        if (GameManager.INSTANCE != null)
        {
            GameManager.INSTANCE.OnActiveRoleChanged += OnActiveRoleChanged;
        }
        RefreshDisplay();
        RefreshAmmoFromCurrentWeapon();
    }

    private void Update()
    {
        if (!_battleSubscribed)
            TrySubscribeBattle();
    }

    private void OnDestroy()
    {
        if (GameManager.INSTANCE != null)
        {
            GameManager.INSTANCE.OnActiveRoleChanged -= OnActiveRoleChanged;
        }
    }


    private void OnPlayerHealthChanged(int roleID, float currentHealth, float maxHealth)
    {
        // 战术模式血量由服务器下发，忽略本地跑图角色的血量。
        if (IsTacticalMode())
            return;

        // 只关心当前上阵角色
        if (roleID != GameManager.INSTANCE.GetActiveRoleID())
        {
            return;
        }
        UpdateDisplay(currentHealth, maxHealth);
    }

    private void OnActiveRoleChanged(int roleID)
    {
        RefreshDisplay();
        //换角色后武器可能变了
        RefreshAmmoFromCurrentWeapon();
    }

    /// <summary>
    /// 弹药事件回调: 只显示当前上阵角色的弹药
    /// </summary>
    private void OnAmmoChanged(int roleID, int currentAmmo, int magazineSize, bool isReloading)
    {
        if (roleID != GameManager.INSTANCE.GetActiveRoleID()) return;
        UpdateAmmoDisplay(currentAmmo, magazineSize, isReloading);
    }

    /// <summary>
    /// 获取当前操控角色的武器
    /// </summary>
    private PlayerWeapon GetCurrentWeapon()
    {
        if (PlayerController.INSTANCE == null) return null;
        PlayerModel model = PlayerController.INSTANCE.currentPlayerModel;
        return model != null ? model.weapon : null;
    }

    private void RefreshAmmoFromCurrentWeapon()
    {
        PlayerWeapon weapon = GetCurrentWeapon();
        if (weapon == null)
        {
            UpdateAmmoDisplay(0, 0, false);
            return;
        }
        UpdateAmmoDisplay(weapon.currentAmmo, weapon.magazineSize, weapon.isReloading);
    }

    /// <summary>
    /// 弹药文本只在内容变化时才刷新
    /// </summary>
    private void UpdateAmmoDisplay(int currentAmmo, int magazineSize, bool isReloading)
    {
        if (_ammoText == null) return;
        string text;
        Color color = Color.white;
        if (isReloading)
        {
            text = "装填中...";
            color = new Color(1f, 0.8f, 0.3f);
        }
        else
        {
            text = $"{currentAmmo} / {magazineSize}";
        }
        if (_ammoText.text != text)
        {
            _ammoText.text = text;
            _ammoText.color = color;
        }
    }

    private void RefreshDisplay()
    {
        if (GameManager.INSTANCE == null)
        {
            return;
        }

        if (IsTacticalMode() && BattleClient.Instance != null)
        {
            SetArmorRootActive(true);
            UpdateDisplay(BattleClient.Instance.MyServerHp, tacticalMaxHealth);
            UpdateArmorDisplay(BattleClient.Instance.MyArmor, tacticalMaxArmor);
            return;
        }

        SetArmorRootActive(false);
        var data = GameManager.INSTANCE.GetRoleData(GameManager.INSTANCE.GetActiveRoleID());
        if (data == null)
        {
            return;
        }
        UpdateDisplay(data.currentHealth, data.finalMaxHealth);
    }

    /// <summary>
    ///更新血条。只在事件触发时调用,不在 Update 里轮询
    /// </summary>
    private void UpdateDisplay(float currentHealth, float maxHealth)
    {
        if (Mathf.Approximately(currentHealth, _lastHealth)
            && Mathf.Approximately(maxHealth, _lastMaxHealth))
            return;

        _lastHealth = currentHealth;
        _lastMaxHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0f;
        }

        if (healthText != null)
        {

            //Mathf.CeilToInt 取整数
            healthText.SetText("{0}/{1}", Mathf.CeilToInt(currentHealth), Mathf.CeilToInt(maxHealth));
        }
    }

    private void UpdateArmorDisplay(float currentArmor, float maxArmor)
    {
        if (Mathf.Approximately(currentArmor, _lastArmor)
            && Mathf.Approximately(maxArmor, _lastMaxArmor))
            return;

        _lastArmor = currentArmor;
        _lastMaxArmor = maxArmor;

        if (armorFill != null)
            armorFill.fillAmount = maxArmor > 0f ? currentArmor / maxArmor : 0f;

        if (armorText != null)
            armorText.SetText("{0}", Mathf.CeilToInt(currentArmor));
    }

    private bool IsTacticalMode()
    {
        return RoomSession.CurrentMode == "pvp"
            && RoomSession.InRoom
            && BattleClient.Instance != null;
    }

    private void SetArmorRootActive(bool active)
    {
        if (armorRoot != null && armorRoot.activeSelf != active)
            armorRoot.SetActive(active);
    }

    private void TrySubscribeBattle()
    {
        if (_battleSubscribed || BattleClient.Instance == null)
            return;

        BattleClient.Instance.OnSnapshot += HandleBattleSnapshot;
        BattleClient.Instance.OnDamageResult += HandleBattleDamage;
        BattleClient.Instance.OnBuyItemResult += HandleBattleBuy;
        BattleClient.Instance.OnRoundState += HandleBattleRound;
        BattleClient.Instance.OnRoomChanged += HandleRoomChanged;
        _battleSubscribed = true;
    }

    private void UnsubscribeBattle()
    {
        if (!_battleSubscribed)
            return;

        BattleClient battleClient = BattleClient.Instance;
        if (battleClient != null)
        {
            battleClient.OnSnapshot -= HandleBattleSnapshot;
            battleClient.OnDamageResult -= HandleBattleDamage;
            battleClient.OnBuyItemResult -= HandleBattleBuy;
            battleClient.OnRoundState -= HandleBattleRound;
            battleClient.OnRoomChanged -= HandleRoomChanged;
        }
        _battleSubscribed = false;
    }

    private void HandleBattleSnapshot(SnapshotDto dto) { RefreshDisplay(); }
    private void HandleBattleDamage(DamageResultDto dto) { RefreshDisplay(); }
    private void HandleBattleBuy(BuyItemResultDto dto) { RefreshDisplay(); }
    private void HandleBattleRound(RoundStateDto dto) { RefreshDisplay(); }
    private void HandleRoomChanged() { RefreshDisplay(); }

    /// <summary>
    /// 创建右下角弹药文本(只创建一次)
    /// </summary>
    private void EnsureAmmoText()
    {
        if (_ammoText != null) return;

        GameObject go = new GameObject("AmmoText", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);  // 锚定右下角
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-30f, 30f);
        rt.sizeDelta = new Vector2(220f, 40f);

        _ammoText = go.AddComponent<TextMeshProUGUI>();
        _ammoText.font = UITextHelper.GetFont();
        _ammoText.fontSize = 28;
        _ammoText.alignment = TextAlignmentOptions.Right;
        _ammoText.color = Color.white;
        _ammoText.outlineWidth = 0.3f;
        _ammoText.outlineColor = new Color(0f, 0f, 0f, 0.9f);
        _ammoText.text = "";
    }
}

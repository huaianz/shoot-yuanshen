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
    //右下角弹药显示
    private TextMeshProUGUI _ammoText;

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
        RefreshDisplay();
        RefreshAmmoFromCurrentWeapon();
    }

    private void OnDisable()
    {
        EventHandler.PlayerHealthChangedEvent -= OnPlayerHealthChanged;
        EventHandler.AmmoChangedEvent -= OnAmmoChanged;
    }

    private void Start()
    {
        // 切换角色事件在 Start 订阅
        if (GameManager.INSTANCE != null)
        {
            GameManager.INSTANCE.OnActiveRoleChanged += OnActiveRoleChanged;
        }
        RefreshDisplay();
        RefreshAmmoFromCurrentWeapon();
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

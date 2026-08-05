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

    private void OnEnable()
    {
        // 订阅血量变化事件
        EventHandler.PlayerHealthChangedEvent += OnPlayerHealthChanged;
        RefreshDisplay();
    }

    private void OnDisable()
    {
        EventHandler.PlayerHealthChangedEvent -= OnPlayerHealthChanged;
    }

    private void Start()
    {
        // 切换角色事件在 Start 订阅
        if (GameManager.INSTANCE != null)
        {
            GameManager.INSTANCE.OnActiveRoleChanged += OnActiveRoleChanged;
        }
        RefreshDisplay();
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
}

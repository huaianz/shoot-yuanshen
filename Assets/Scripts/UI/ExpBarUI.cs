using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD 经验条: 监听经验事件刷新填充和文本, 事件驱动, 没有每帧轮询
/// </summary>
public class ExpBarUI : MonoBehaviour
{
    [Header("引用")]
    public Image fillImage;           // 填充图片(Image Type = Filled)
    public TextMeshProUGUI expText;   // 经验文本, 可以不填

    private void OnEnable()
    {
        EventHandler.ExpChangedEvent += OnExpChanged;
        if (GameManager.INSTANCE != null)
        {
            GameManager.INSTANCE.OnActiveRoleChanged += OnActiveRoleChanged;
        }
        Refresh();   // 打开时先显示当前角色的经验
    }

    private void OnDisable()
    {
        EventHandler.ExpChangedEvent -= OnExpChanged;
        if (GameManager.INSTANCE != null)
        {
            GameManager.INSTANCE.OnActiveRoleChanged -= OnActiveRoleChanged;
        }
    }

    // 切换角色 -> 经验条刷新成新角色的经验
    private void OnActiveRoleChanged(int roleID)
    {
        Refresh();
    }

    // 经验变化回调: 只显示当前上阵角色的经验
    private void OnExpChanged(int roleID, int curExp, int expToNext)
    {
        if (roleID != GameManager.INSTANCE.GetActiveRoleID()) return;
        UpdateDisplay(curExp, expToNext);
    }

    private void Refresh()
    {
        if (GameManager.INSTANCE == null) return;
        int roleID = GameManager.INSTANCE.GetActiveRoleID();
        if (roleID < 0) return;
        var role = GameManager.INSTANCE.GetRoleData(roleID);
        if (role == null) return;
        UpdateDisplay(role.roleExp, GameManager.INSTANCE.GetExpToNextLevel(role.roleLevel));
    }

    private void UpdateDisplay(int curExp, int expToNext)
    {
        if (expToNext <= 0) return;
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01((float)curExp / expToNext);
        }
        if (expText != null)
        {
            expText.text = $"EXP {curExp}/{expToNext}";
        }
    }
}
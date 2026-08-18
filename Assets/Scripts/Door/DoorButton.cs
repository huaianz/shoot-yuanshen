using UnityEngine;

/// <summary>
/// 机关按钮: 玩家踩进触发器 -> 打开指定门 + 弹提示
/// </summary>
public class DoorButton : MonoBehaviour
{
    [Tooltip("这个按钮控制的门")]
    public DoorController door;
    [Tooltip("触发后提示文案")]
    public string prompt = "门已打开！";

    private bool _used; // 一次性机关

    private void OnTriggerEnter(Collider other)
    {
        if (_used) return;
        // 向上找父物体判断是不是玩家(角色身上不是所有碰撞体都带 Player 标签)
        if (other.GetComponentInParent<PlayerController>() == null
            && other.GetComponentInParent<PlayerModel>() == null) return;

        _used = true;
        if (door != null) door.Open();
        ToastUI.ShowMessage(prompt, new Color(0.4f, 1f, 0.5f));
    }
}

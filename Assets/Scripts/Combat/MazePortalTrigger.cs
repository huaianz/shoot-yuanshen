using UnityEngine;

/// <summary>
/// 迷宫入口触发器: 玩家踩入 -> 生成随机迷宫
/// </summary>
public class MazePortalTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null
            && other.GetComponentInParent<PlayerModel>() == null) return;

        MazeManager.Instance.TryEnterMaze(transform.position);
    }
}

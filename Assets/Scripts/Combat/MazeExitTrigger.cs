using UnityEngine;

/// <summary>
/// 迷宫出口触发器: 玩家到达 -> 通关奖励 + 清理迷宫
/// </summary>
public class MazeExitTrigger : MonoBehaviour
{
    private MazeManager _manager;
    private bool _used;

    public void Init(MazeManager manager)
    {
        _manager = manager;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_used) return;
        if (other.GetComponentInParent<PlayerController>() == null
            && other.GetComponentInParent<PlayerModel>() == null) return;

        _used = true;
        _manager?.FinishMaze();
    }
}

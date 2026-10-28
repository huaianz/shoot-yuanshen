using UnityEngine;

/// <summary>
/// 合作房间触发器: 玩家进入关门 -> 服务器广播本波清空 -> 开门 -> 下一波刷新重新关门
/// </summary>
public class CoopRoomTrigger : MonoBehaviour
{
    [Header("门")]
    public DoorController door;

    [Header("提示")]
    public string enterPrompt = "门已关闭！清理房间内所有敌人";
    public string clearPrompt = "敌人已清除，门已打开";
    public string nextWavePrompt = "下一波敌人来袭，门再次关闭";

    private bool _entered;       // 本机玩家是否已经进过这个房间
    private bool _waveCleared;   // 当前这一波是否已被清空

    private void OnEnable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnWaveState += HandleWaveState;
    }

    private void OnDisable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnWaveState -= HandleWaveState;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_entered) return;

        // 只认本机玩家: 远端玩家的胶囊上没有 PlayerController
        if (other.GetComponentInParent<PlayerController>() == null) return;

        _entered = true;
        SetDoor(false);
        ToastUI.ShowMessage(enterPrompt, new Color(1f, 0.6f, 0.2f));
    }

    private void HandleWaveState(WaveStateDto dto)
    {
        if (!RoomSession.InRoom) return;
        if (dto.roomId != RoomSession.CurrentRoomId) return;

        if (dto.cleared)
        {
            if (_waveCleared) return;   // 同一次清空只处理一遍
            _waveCleared = true;
            SetDoor(true);
            ToastUI.ShowMessage(clearPrompt, new Color(0.4f, 1f, 0.5f));
        }
        else if (_waveCleared && dto.alive > 0)
        {
            // 上一波清完之后又刷出新的一波
            _waveCleared = false;
            SetDoor(false);
            ToastUI.ShowMessage(nextWavePrompt, new Color(1f, 0.6f, 0.2f));
        }
    }

    private void SetDoor(bool open)
    {
        if (door == null) return;
        if (open) door.Open();
        else door.Close();
    }
}
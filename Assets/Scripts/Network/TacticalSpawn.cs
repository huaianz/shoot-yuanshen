using UnityEngine;

/// <summary>战术模式: 每回合开始时把自己传送到对应队伍出生点</summary>
public class TacticalSpawn : MonoBehaviour
{
    public Transform attackSpawn;
    public Transform defenseSpawn;

    private int _lastRound = -1;

    private void OnEnable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnRoundState += HandleRound;
    }

    private void OnDisable()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnRoundState -= HandleRound;
    }

    private void HandleRound(RoundStateDto dto)
    {
        if (dto == null) return;
        if (dto.phase != 1) return;          // 进入购买阶段 = 新回合开始
        if (dto.round == _lastRound) return;

        _lastRound = dto.round;

        var pc = PlayerController.INSTANCE;
        if (pc == null || pc.currentPlayerModel == null) return;

        Transform spawn = BattleClient.Instance.MyTeam == 0 ? attackSpawn : defenseSpawn;
        if (spawn == null) return;

        var model = pc.currentPlayerModel;
        var cc = model.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;

        pc.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        model.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

        if (cc != null) cc.enabled = true;

        BattleClient.Instance.MarkTeleport();
        Debug.Log($"[TacticalSpawn] 传送到{(BattleClient.Instance.MyTeam == 0 ? "进攻" : "防守")}出生点");
    }
}

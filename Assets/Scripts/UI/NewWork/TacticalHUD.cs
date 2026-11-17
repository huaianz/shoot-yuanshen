using TMPro;
using UnityEngine;

/// <summary>战术模式 HUD: 队伍 / 比分 / 阶段 / 倒计时</summary>
public class TacticalHUD : MonoBehaviour
{
    public TMP_Text teamText;
    public TMP_Text scoreText;
    public TMP_Text phaseText;
    public TMP_Text timerText;
    public GameObject panelRoot;

    [Header("炸弹状态")]
    public GameObject bombStatus;
    public TMP_Text fuseText;

    [Header("死亡界面")]
    public GameObject deadOverlay;
    public TMP_Text deadText;

    private bool _subscribed;
    private SpikeStateDto _spike;

    private void OnEnable()
    {
        TrySubscribe();
        RefreshVisibility();
    }

    private void Start() { TrySubscribe(); }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null) return;
        BattleClient.Instance.OnRoundState += Refresh;
        BattleClient.Instance.OnSpikeState += HandleSpike;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (_subscribed && BattleClient.Instance != null)
            BattleClient.Instance.OnRoundState -= Refresh;
            BattleClient.Instance.OnSpikeState -= HandleSpike;
        _subscribed = false;
    }

    private void Update()
    {
        TrySubscribe();
        RefreshVisibility();

        // 阵亡界面
        if (deadOverlay != null && BattleClient.Instance != null)
        {
            bool dead = RoomSession.CurrentMode == "pvp"
                && RoomSession.InRoom
                && BattleClient.Instance.MyServerHp <= 0;

            if (deadOverlay.activeSelf != dead) deadOverlay.SetActive(dead);
        }

        // 炸弹图标 + 引信
        if (bombStatus != null)
        {
            bool planted = _spike != null && _spike.planted;
            if (bombStatus.activeSelf != planted) bombStatus.SetActive(planted);

            if (planted && fuseText != null)
                fuseText.text = Mathf.CeilToInt(_spike.fuseLeftMs / 1000f) + "s";
        }
    }

    private void HandleSpike(SpikeStateDto dto)
    {
        _spike = dto;
    }

    private void RefreshVisibility()
    {
        bool pvp = RoomSession.CurrentMode == "pvp" && RoomSession.InRoom;

        var round = BattleClient.Instance != null ? BattleClient.Instance.LastRound : null;

        // 只有正式战斗阶段显示。购买、选角、回合结束和新回合准备都隐藏。
        bool playing = pvp && round != null
            && !round.matchOver
            && round.phase == 2;

        if (panelRoot != null && panelRoot.activeSelf != playing)
            panelRoot.SetActive(playing);
    }

    private void Refresh(RoundStateDto dto)
    {
        if (dto == null) return;

        if (teamText != null)
            teamText.text = BattleClient.Instance.MyTeam == 0 ? "进攻方" : "防守方";

        if (scoreText != null)
            scoreText.text = $"{dto.attackScore} : {dto.defenseScore}";

        if (phaseText != null)
        {
            switch (dto.phase)
            {
                case 0: phaseText.text = "准备"; break;
                case 1: phaseText.text = $"第 {dto.round} 回合 · 购买阶段"; break;
                case 2: phaseText.text = $"第 {dto.round} 回合 · 战斗中"; break;
                case 4: phaseText.text = "选择角色"; break;
                default:
                    phaseText.text = dto.matchOver ? "比赛结束" : "回合结束";
                    break;
            }
        }

        if (timerText != null)
        {
            int sec = Mathf.CeilToInt(dto.timeLeftMs / 1000f);
            timerText.text = $"{sec / 60:00}:{sec % 60:00}";
        }
    }
}

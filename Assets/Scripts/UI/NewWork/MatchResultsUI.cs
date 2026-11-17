using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>局末: 胜负横幅 → 战绩结算</summary>
public class MatchResultsUI : MonoBehaviour
{
    [Header("胜利/失败横幅")]
    public GameObject bannerRoot;
    public TMP_Text bannerText;
    public TMP_Text bannerSubText;     // 比分, 例如 "5 : 3"
    public float bannerSeconds = 3f;

    [Header("结算面板")]
    public GameObject panelRoot;
    public TMP_Text scoreText;
    public Transform playerContainer;
    public MatchResultRowUI rowPrefab;
    public Button returnBtn;
    public Button leaveBtn;

    private readonly List<MatchResultRowUI> _rows = new List<MatchResultRowUI>();
    private bool _blocking;
    private bool _subscribed;
    private Coroutine _routine;

    private void Awake()
    {
        if (returnBtn != null) returnBtn.onClick.AddListener(ReturnToLobby);
        if (leaveBtn != null) leaveBtn.onClick.AddListener(LeaveRoom);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start() { TrySubscribe(); }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null) return;
        BattleClient.Instance.OnMatchResult += HandleResult;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (_subscribed && BattleClient.Instance != null)
            BattleClient.Instance.OnMatchResult -= HandleResult;
        _subscribed = false;

        ReleaseUIBlock();
    }

    private void Update()
    {
        TrySubscribe();
    }

    private void HandleResult(MatchResultDto dto)
    {
        if (dto == null) return;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(dto));
    }

    private IEnumerator ShowRoutine(MatchResultDto dto)
    {
        bool win = dto.winnerTeam == BattleClient.Instance.MyTeam;

        // 先显示胜负横幅
        if (panelRoot != null) panelRoot.SetActive(false);
        if (bannerRoot != null) bannerRoot.SetActive(true);

        if (bannerText != null)
        {
            bannerText.text = win ? "胜利" : "失败";
            bannerText.color = win ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.45f, 0.4f);
        }

        if (bannerSubText != null)
            bannerSubText.text = $"{dto.attackScore} : {dto.defenseScore}";

        BlockUI();

        yield return new WaitForSecondsRealtime(bannerSeconds);

        // 再显示战绩
        if (bannerRoot != null) bannerRoot.SetActive(false);
        if (panelRoot != null) panelRoot.SetActive(true);

        if (scoreText != null)
            scoreText.text = $"{dto.attackScore} : {dto.defenseScore}";

        Rebuild(dto);
        _routine = null;
    }

    private void Rebuild(MatchResultDto dto)
    {
        if (playerContainer == null || rowPrefab == null) return;

        var sorted = new List<MatchResultPlayerDto>(dto.players);
        sorted.Sort((a, b) => b.kills.CompareTo(a.kills));

        while (_rows.Count < sorted.Count)
        {
            var row = Instantiate(rowPrefab, playerContainer);
            row.gameObject.SetActive(true);
            _rows.Add(row);
        }

        for (int i = 0; i < _rows.Count; i++)
        {
            bool used = i < sorted.Count;
            _rows[i].gameObject.SetActive(used);
            if (!used) continue;

            _rows[i].Bind(sorted[i], sorted[i].id == BattleClient.Instance.MyPlayerId);
        }
    }

    private void ReturnToLobby()
    {
        HideAll();
        BattleClient.Instance.SendReturnToLobby();
    }

    private void LeaveRoom()
    {
        HideAll();
        RoomSession.Leave();
        ToastUI.ShowMessage("已退出房间", new Color(0.8f, 0.95f, 1f));
    }

    private void HideAll()
    {
        if (bannerRoot != null) bannerRoot.SetActive(false);
        if (panelRoot != null) panelRoot.SetActive(false);
        ReleaseUIBlock();
    }

    private void BlockUI()
    {
        if (_blocking) return;

        UIManager.EnterUIBlock();
        _blocking = true;
    }

    private void ReleaseUIBlock()
    {
        if (!_blocking) return;

        _blocking = false;
        if (UIManager.IsAnyUIOpen) UIManager.ExitUIBlock();
    }
}

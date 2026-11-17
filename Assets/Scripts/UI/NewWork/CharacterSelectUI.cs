using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>战术模式角色选择(1 分钟, 超时随机)</summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("面板")]
    public GameObject panelRoot;
    public Transform cardContainer;
    public CharacterCardUI cardPrefab;
    public TMP_Text timerText;
    public TMP_Text statusText;
    public Button confirmBtn;
    public TMP_Text confirmText;
    public CharacterPreview preview;
    public TMP_Text previewNameText;

    [Header("角色配置")]
    public string[] characterNames = { "荧", "芙宁娜" };
    public Sprite[] characterSprites;

    private readonly List<CharacterCardUI> _cards = new List<CharacterCardUI>();
    private int _selected = -1;
    private bool _confirmed;
    private bool _blocking;
    private bool _subscribed;
    private bool _wasSelecting;
    private CharacterSelectStateDto _last;

    private void Awake()
    {
        if (confirmBtn != null) confirmBtn.onClick.AddListener(Confirm);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start() { TrySubscribe(); }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null) return;

        BattleClient.Instance.OnCharacterSelectState += HandleState;
        BattleClient.Instance.OnRoundState += HandleRound;
        _subscribed = true;
    }

    private void OnDisable()
    {
        if (_subscribed && BattleClient.Instance != null)
        {
            BattleClient.Instance.OnCharacterSelectState -= HandleState;
            BattleClient.Instance.OnRoundState -= HandleRound;
        }
        _subscribed = false;

        ReleaseUIBlock();
    }

    private void Update()
    {
        TrySubscribe();
    }

    private void HandleState(CharacterSelectStateDto dto)
    {
        if (dto == null) return;
        _last = dto;
        _wasSelecting = true;

        Show();

        if (timerText != null)
        {
            int sec = Mathf.CeilToInt(dto.timeLeftMs / 1000f);
            timerText.text = $"{sec / 60:00}:{sec % 60:00}";
        }

        int confirmed = 0;
        CharacterSelectPlayerDto me = null;
        foreach (var p in dto.players)
        {
            if (p.confirmed) confirmed++;
            if (p.id == BattleClient.Instance.MyPlayerId) me = p;
        }

        if (me != null)
        {
            _confirmed = me.confirmed;
            if (me.characterId >= 0) _selected = me.characterId;
        }

        RebuildCards();
        RefreshPreview();

        if (statusText != null)
            statusText.text = $"{confirmed}/{dto.players.Count} 已确认";

        if (confirmBtn != null)
            confirmBtn.interactable = !_confirmed;

        if (confirmText != null)
            confirmText.text = _confirmed ? "已确认" : "确认";
    }

    private void HandleRound(RoundStateDto dto)
    {
        // 4 = 角色选择阶段
        if (dto != null && dto.phase != 4)
        {
            if (_wasSelecting)
            {
                _wasSelecting = false;
                ApplySelectedCharacterForMatch();
                // 选角结束, 播一段加载再进回合
                if (LoadingVideoUI.Instance != null)
                {
                    LoadingVideoUI.Instance.Show();
                    StartCoroutine(HideLoadingAfter(4f));
                }
            }

            Hide();
        }
    }

    private IEnumerator HideLoadingAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (LoadingVideoUI.Instance != null)
            LoadingVideoUI.Instance.Hide();
    }

    private void RebuildCards()
    {
        if (cardContainer == null || cardPrefab == null) return;

        int count = characterNames != null && characterNames.Length > 0 ? characterNames.Length : 2;

        while (_cards.Count < count)
        {
            var card = Instantiate(cardPrefab, cardContainer);
            card.gameObject.SetActive(true);
            _cards.Add(card);
        }

        for (int i = 0; i < _cards.Count; i++)
        {
            bool used = i < count;
            _cards[i].gameObject.SetActive(used);
            if (!used) continue;

            Sprite sprite = characterSprites != null && i < characterSprites.Length ? characterSprites[i] : null;
            _cards[i].Bind(i, characterNames != null && i < characterNames.Length ? characterNames[i] : "角色 " + i,
                sprite, i == _selected, OnSelect);
        }
    }

    private void OnSelect(int index)
    {
        if (_confirmed) return;

        _selected = index;
        BattleClient.Instance.SendSelectCharacter(index);
        ApplyLocalCharacter(index);
        RebuildCards();
        RefreshPreview();
    }

    /// <summary>本地也切到同一个角色, 否则只有别人能看到你的选择</summary>
    private void ApplyLocalCharacter(int index)
    {
        var gm = GameManager.INSTANCE;
        if (gm == null) return;

        var roles = gm.GetAllRoles();
        if (roles == null || index < 0 || index >= roles.Count) return;

        gm.SetActiveRole(roles[index].roleID);
    }

    /// <summary>
    /// 选角阶段结束、正式进入对局时，切换到所选角色对应的 PlayerModel。
    /// </summary>
    private void ApplySelectedCharacterForMatch()
    {
        GameManager gm = GameManager.INSTANCE;
        if (gm == null || _selected < 0)
            return;

        List<RoleRuntimeData> roles = gm.GetAllRoles();
        if (roles == null || _selected >= roles.Count)
            return;

        int roleID = roles[_selected].roleID;
        int modelIndex = gm.GetModelIndexForRoleID(roleID);
        if (modelIndex < 0)
        {
            Debug.LogWarning($"[CharacterSelect] 角色 {roleID} 没有对应的场景模型");
            return;
        }

        gm.SetActiveRole(roleID);
        if (PlayerController.INSTANCE != null)
            PlayerController.INSTANCE.SwitchPlayerModel(modelIndex);

        Debug.Log($"[CharacterSelect] 对局角色={roles[_selected].baseData.characterName}, " +
                  $"roleID={roleID}, modelIndex={modelIndex}");
    }

    private void Confirm()
    {
        if (_confirmed) return;

        if (_selected < 0)
        {
            ToastUI.ShowMessage("请先选择一个角色", new Color(1f, 0.75f, 0.4f));
            return;
        }

        BattleClient.Instance.SendConfirmCharacter();
        _confirmed = true;
        if (confirmBtn != null) confirmBtn.interactable = false;
        if (confirmText != null) confirmText.text = "已确认";
    }

    private void Show()
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (!_blocking)
        {
            UIManager.EnterUIBlock();
            _blocking = true;
        }
    }

    private void Hide()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (preview != null) preview.Clear();
        if (previewNameText != null) previewNameText.text = string.Empty;
        ReleaseUIBlock();
    }

    private void RefreshPreview()
    {
        if (_selected < 0)
        {
            if (preview != null) preview.Clear();
            if (previewNameText != null) previewNameText.text = "请选择角色";
            return;
        }

        if (preview != null) preview.Show(_selected);
        if (previewNameText != null && characterNames != null && _selected < characterNames.Length)
            previewNameText.text = characterNames[_selected];
    }

    private void ReleaseUIBlock()
    {
        if (!_blocking) return;

        _blocking = false;
        if (UIManager.IsAnyUIOpen) UIManager.ExitUIBlock();
    }
}

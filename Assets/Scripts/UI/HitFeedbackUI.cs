using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 命中/受击/波次反馈
/// </summary>
public class HitFeedbackUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Image hurtVignette;
    public Image hitMarker;
    public Image killMarker;

    [Header("时长(秒)")]
    public float hitDuration = 0.15f;
    public float hurtDuration = 0.25f;

    [Header("命中标记缩放冲击")]
    public float markerPunchScale = 1.25f;

    private float _hitTimer;
    private float _hurtTimer;
    private Vector3 _hitBaseScale = Vector3.one;
    private Vector3 _killBaseScale = Vector3.one;

    private void Awake()
    {
        if (hitMarker != null)
        {
            _hitBaseScale = hitMarker.rectTransform.localScale;
            SetAlpha(hitMarker, 0f);
        }
        if (killMarker != null)
        {
            _killBaseScale = killMarker.rectTransform.localScale;
            SetAlpha(killMarker, 0f);
        }
        if (hurtVignette != null) SetAlpha(hurtVignette, 0f);
    }

    private void Start()
    {
        if (BattleClient.Instance == null) return;
        BattleClient.Instance.OnDamageResult += HandleDamage;
        BattleClient.Instance.OnEnemyHit += HandleEnemyHit;
        BattleClient.Instance.OnWaveState += HandleWave;
        BattleClient.Instance.OnReward += HandleReward;
    }

    private void OnDestroy()
    {
        if (BattleClient.Instance == null) return;
        BattleClient.Instance.OnDamageResult -= HandleDamage;
        BattleClient.Instance.OnEnemyHit -= HandleEnemyHit;
        BattleClient.Instance.OnWaveState -= HandleWave;
        BattleClient.Instance.OnReward -= HandleReward;
    }

    /// <summary>玩家之间的伤害</summary>
    private void HandleDamage(DamageResultDto dto)
    {
        int myId = BattleClient.Instance.MyPlayerId;

        if (dto.attackerKind == 0 && dto.shooterId == myId)
        {
            ShowHit(false);
            DamageNumberUI.Instance?.Show(
                new Vector3(dto.hitX, dto.hitY + 0.2f, dto.hitZ), dto.damage, false);
        }
        else if (dto.targetId == myId)
        {
            _hurtTimer = hurtDuration;
            if (hurtVignette != null)
            {
                hurtVignette.gameObject.SetActive(true);
                SetAlpha(hurtVignette, 1f);
            }

            ToastUI.ShowMessage(
                $"你被击中  -{dto.damage}" + (dto.isKill ? "（阵亡，3 秒后复活）" : ""),
                new Color(1f, 0.4f, 0.3f));
        }
    }

    /// <summary>
    /// 我打中敌人
    /// </summary>
    private void HandleEnemyHit(EnemyHitDto dto)
    {
        if (dto.shooterId != BattleClient.Instance.MyPlayerId) return;

        ShowHit(dto.isKill);
        DamageNumberUI.Instance?.Show(
            new Vector3(dto.hitX, dto.hitY + 0.2f, dto.hitZ), dto.damage, dto.isKill);
    }

    private void HandleWave(WaveStateDto dto)
    {
        if (dto.cleared)
        {
            ToastUI.ShowMessage(
                $"第 {dto.wave} 波已清空  {dto.nextWaveInMs / 1000} 秒后刷新",
                new Color(1f, 0.85f, 0.45f));
        }
        else if (dto.alive > 0 && dto.alive == dto.total)
        {
            string tip = dto.wave % 3 == 0 ? "精英波来袭" : "波次来袭";
            ToastUI.ShowMessage(
                $"第 {dto.wave} 波 {tip}  {dto.alive} 只",
                new Color(1f, 0.65f, 0.3f));
        }
        else if (dto.alive <= 2 && dto.alive > 0)
        {
            ToastUI.ShowMessage($"只剩 {dto.alive} 只了", new Color(0.85f, 0.9f, 1f));
        }
    }

    private void HandleReward(RewardDto dto)
    {
        if (dto.playerId != BattleClient.Instance.MyPlayerId) return;

        string tag = string.IsNullOrEmpty(dto.reason)
            ? (dto.elite ? "精英击杀" : "击杀")
            : dto.reason;

        ToastUI.ShowMessage(
            $"{tag}  +{dto.coins} 金币  (共 {dto.total})",
            new Color(1f, 0.9f, 0.5f));
    }


    private void ShowHit(bool isKill)
    {
        _hitTimer = hitDuration;
        if (hitMarker != null) SetAlpha(hitMarker, 1f);

        if (isKill && killMarker != null)
        {
            killMarker.gameObject.SetActive(true);
            SetAlpha(killMarker, 1f);
        }
    }

    private void Update()
    {
        if (hitMarker != null && _hitTimer > 0f)
        {
            _hitTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_hitTimer / hitDuration);
            SetAlpha(hitMarker, t);
            hitMarker.rectTransform.localScale =
                Vector3.Lerp(_hitBaseScale, _hitBaseScale * markerPunchScale, t);
        }

        if (killMarker != null && killMarker.gameObject.activeSelf)
        {
            float t = Mathf.Clamp01(_hitTimer / hitDuration);
            SetAlpha(killMarker, t);
            killMarker.rectTransform.localScale =
                Vector3.Lerp(_killBaseScale, _killBaseScale * markerPunchScale, t);
            if (_hitTimer <= 0f) killMarker.gameObject.SetActive(false);
        }

        if (hurtVignette != null && _hurtTimer > 0f)
        {
            _hurtTimer -= Time.deltaTime;
            SetAlpha(hurtVignette, Mathf.Clamp01(_hurtTimer / hurtDuration));
            if (_hurtTimer <= 0f) hurtVignette.gameObject.SetActive(false);
        }
    }

    private static void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}

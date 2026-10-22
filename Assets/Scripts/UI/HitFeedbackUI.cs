using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 命中/受击反馈
/// </summary>
public class HitFeedbackUI : MonoBehaviour
{
    [Header("UI 引用")]
    public Image hurtVignette;   // 全屏红色渐晕, 平时 alpha=0
    public Image hitMarker;      // 命中标记, 平时 alpha=0
    public Image killMarker;     // 击杀标记(没有可留空)

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
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnDamageResult += HandleDamage;
    }

    private void OnDestroy()
    {
        if (BattleClient.Instance != null)
            BattleClient.Instance.OnDamageResult -= HandleDamage;
    }

    private void HandleDamage(DamageResultDto dto)
    {
        int myId = BattleClient.Instance.MyPlayerId;

        if (dto.shooterId == myId)
        {
            // 我打中别人
            _hitTimer = hitDuration;
            if (hitMarker != null) SetAlpha(hitMarker, 1f);

            if (dto.isKill && killMarker != null)
            {
                killMarker.gameObject.SetActive(true);
                SetAlpha(killMarker, 1f);
            }

            // 伤害数字: 复用你现有的 DamageNumberUI
            if (DamageNumberUI.Instance != null)
                DamageNumberUI.Instance.Show(
                    new Vector3(dto.hitX, dto.hitY + 0.2f, dto.hitZ), dto.damage, false);
        }
        else if (dto.targetId == myId)
        {
            // 我被击中
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

    private void Update()
    {
        // 命中标记: 淡出 + 轻微缩回
        if (hitMarker != null && _hitTimer > 0f)
        {
            _hitTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_hitTimer / hitDuration);
            SetAlpha(hitMarker, t);
            hitMarker.rectTransform.localScale =
                Vector3.Lerp(_hitBaseScale, _hitBaseScale * markerPunchScale, t);
        }

        // 击杀标记: 跟命中标记同生命周期
        if (killMarker != null && killMarker.gameObject.activeSelf)
        {
            float t = Mathf.Clamp01(_hitTimer / hitDuration);
            SetAlpha(killMarker, t);
            killMarker.rectTransform.localScale =
                Vector3.Lerp(_killBaseScale, _killBaseScale * markerPunchScale, t);
            if (_hitTimer <= 0f) killMarker.gameObject.SetActive(false);
        }

        // 受击红屏: 淡出
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
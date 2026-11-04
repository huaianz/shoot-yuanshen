using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 把快照缓冲里插值出来的"其他玩家"显示成带动作的角色
/// </summary>
public class RemotePlayerView : MonoBehaviour
{
    [Tooltip("远端玩家的显示预制体(带 Animator 的角色)")]
    public GameObject remotePlayerPrefab;

    [Tooltip("位置平滑速度")]
    public float smoothSpeed = 20f;

    [Tooltip("显示偏移: 角色陷进地面就把 Y 调大(例如 0.9)")]
    public Vector3 positionOffset = Vector3.zero;

    [Header("调试")]
    [Tooltip("勾选后每 2 秒打印一次动画状态, 排错用")]
    public bool debugAnimator = false;

    [Header("动画参数名(要和 Animator 里一致)")]
    public string moveBlendParam = "MoveBlend";
    public string aimingXParam = "AimingX";
    public string aimingYParam = "AimingY";
    public string crouchParam = "Crouch";
    public string climbParam = "Climb";
    public string slideParam = "Slide";
    public string dodgeParam = "Dodge";

    [Header("动画过渡")]
    [Tooltip("动作混合的平滑速度, 越大越跟手")]
    public float animLerp = 10f;

    [Tooltip("按多少米每秒算作\"跑满\"")]
    public float fullSpeed = 6f;

    [Header("状态名(要和 Animator 里的 State 名一致)")]
    public string idleStateName = "Idle";
    public string moveStateName = "Move";
    public string aimStateName = "Aiming";
    public string hoverStateName = "Hover";
    public float stateTransition = 0.15f;

    private class View
    {
        public Transform Root;
        public Animator Animator;
        public Transform Hand;
        public GameObject Weapon;
        public int WeaponId = int.MinValue;
        public float MoveBlend;
        public Vector3 LastTarget;
        public int LastAnimId = -1;
        public float LogTimer;
        public float Age;

        public bool HasMove, HasAimX, HasAimY, HasCrouch, HasClimb, HasSlide, HasDodge;
    }

    private readonly Dictionary<int, View> _views = new Dictionary<int, View>();
    private readonly List<int> _alive = new List<int>();

    private int _moveBlendHash, _aimXHash, _aimYHash, _crouchHash, _climbHash, _slideHash, _dodgeHash;

    private void Awake()
    {
        _moveBlendHash = Animator.StringToHash(moveBlendParam);
        _aimXHash = Animator.StringToHash(aimingXParam);
        _aimYHash = Animator.StringToHash(aimingYParam);
        _crouchHash = Animator.StringToHash(crouchParam);
        _climbHash = Animator.StringToHash(climbParam);
        _slideHash = Animator.StringToHash(slideParam);
        _dodgeHash = Animator.StringToHash(dodgeParam);
    }

    private void Start()
    {
        if (BattleClient.Instance == null)
        {
            Debug.LogError("[RemotePlayerView] 找不到 BattleClient.Instance");
            return;
        }
        BattleClient.Instance.OnPlayerJoinLeave += HandleJoinLeave;
        BattleClient.Instance.OnRoomChanged += HandleRoomChanged;
    }

    private void OnDestroy()
    {
        if (BattleClient.Instance != null)
        {
            BattleClient.Instance.OnPlayerJoinLeave -= HandleJoinLeave;
            BattleClient.Instance.OnRoomChanged -= HandleRoomChanged;
        }
        ClearAll();
    }

    private void Update()
    {
        var bc = BattleClient.Instance;
        if (bc == null || remotePlayerPrefab == null) return;

        if (!bc.Buffer.TrySample(out var players)) return;
        Apply(players);
    }

    private void Apply(List<PlayerStateDto> players)
    {
        _alive.Clear();
        int myId = BattleClient.Instance.MyPlayerId;

        foreach (var p in players)
        {
            if (p.id == myId) continue;
            _alive.Add(p.id);

            if (!_views.TryGetValue(p.id, out var v) || v.Root == null)
            {
                v = CreateView(p.id);
                if (v == null) continue;
            }

            Vector3 target = new Vector3(p.x, p.y, p.z) + positionOffset;

            v.Root.position = Vector3.Lerp(v.Root.position, target, smoothSpeed * Time.deltaTime);
            v.Root.rotation = Quaternion.Slerp(v.Root.rotation,
                Quaternion.Euler(0f, p.yaw, 0f), smoothSpeed * Time.deltaTime);

            UpdateAnimator(v, p, target);
        }

        // 快照里没有的玩家 -> 销毁
        List<int> toRemove = null;
        foreach (var kv in _views)
            if (!_alive.Contains(kv.Key))
                (toRemove ??= new List<int>()).Add(kv.Key);

        if (toRemove != null)
            foreach (int id in toRemove) RemoveView(id);
    }

    private View CreateView(int id)
    {
        var go = Instantiate(remotePlayerPrefab, transform);
        go.name = "RemotePlayer_" + id;

        var anim = go.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }
        else
        {
            Debug.LogWarning($"[RemotePlayerView] 预制体上没有 Animator: {remotePlayerPrefab.name}");
        }

        var v = new View
        {
            Root = go.transform,
            Animator = anim,
            Hand = FindDeep(go.transform, "DEF-hand.R"),
            LastTarget = go.transform.position
        };

        if (anim != null)
        {
            v.HasMove = HasParam(anim, _moveBlendHash);
            v.HasAimX = HasParam(anim, _aimXHash);
            v.HasAimY = HasParam(anim, _aimYHash);
            v.HasCrouch = HasParam(anim, _crouchHash);
            v.HasClimb = HasParam(anim, _climbHash);
            v.HasSlide = HasParam(anim, _slideHash);
            v.HasDodge = HasParam(anim, _dodgeHash);

            Debug.Log($"[RemoteAnim初始化] id={id} enabled={anim.enabled} " +
                      $"controller={(anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "null")} " +
                      $"avatar={(anim.avatar != null ? anim.avatar.name : "null")} " +
                      $"params={anim.parameterCount} speed={anim.speed:F2}");
        }

        _views[id] = v;
        return v;
    }

    /// <summary>按服务器下发的武器 id 生成对应枪械模型</summary>
    private void UpdateWeapon(View v, int weaponId)
    {
        if (v.WeaponId == weaponId) return;
        v.WeaponId = weaponId;

        if (v.Weapon != null)
        {
            Destroy(v.Weapon);
            v.Weapon = null;
        }

        if (weaponId < 0) return;

        if (v.Hand == null)
        {
            Debug.LogWarning("[RemotePlayerView] 远端模型里找不到右手骨骼 DEF-hand.R, 无法挂枪");
            return;
        }

        var data = InventoryManager.INSTANCE != null ? InventoryManager.INSTANCE.weaponData : null;
        var template = data != null ? data.GetWeaponByID(weaponId) : null;

        if (template == null || template.weaponModel == null) return;

        v.Weapon = Instantiate(template.weaponModel, v.Hand);
        v.Weapon.transform.localPosition = template.weaponPosition;
        v.Weapon.transform.localRotation = Quaternion.Euler(template.weaponRotation);
        v.Weapon.transform.localScale = template.weaponScale;
    }

    private static Transform FindDeep(Transform root, string boneName)
    {
        if (root == null) return null;
        if (root.name == boneName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindDeep(root.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }

    private void UpdateAnimator(View v, PlayerStateDto p, Vector3 target)
    {
        var anim = v.Animator;
        if (anim == null) return;

        UpdateWeapon(v, p.weaponId);

        // 用插值后的位移估算速度, 驱动移动混合树
        Vector3 delta = target - v.LastTarget;
        v.LastTarget = target;

        float speed = delta.magnitude / Mathf.Max(0.0001f, Time.deltaTime);
        float targetBlend = Mathf.Clamp01(speed / Mathf.Max(0.1f, fullSpeed));
        v.MoveBlend = Mathf.Lerp(v.MoveBlend, targetBlend, animLerp * Time.deltaTime);

        if (v.HasMove) anim.SetFloat(_moveBlendHash, v.MoveBlend);

        // 控制器的默认状态是空的, 必须主动切到具体状态才会播动画
        if (p.animId != v.LastAnimId)
        {
            string stateName = null;

            switch (p.animId)
            {
                case 0: stateName = idleStateName; break;
                case 1: stateName = moveStateName; break;
                case 2: stateName = aimStateName; break;
                case 7: stateName = hoverStateName; break;
                // 3/4/5/6 交给 Bool/Trigger 让控制器自己过渡
            }

            if (!string.IsNullOrEmpty(stateName))
                anim.CrossFadeInFixedTime(stateName, stateTransition, 0);
        }

        bool crouch = p.animId == 3;
        bool climb = p.animId == 4;
        bool slide = p.animId == 5;

        if (v.HasCrouch) anim.SetBool(_crouchHash, crouch);
        if (v.HasClimb) anim.SetBool(_climbHash, climb);
        if (v.HasSlide) anim.SetBool(_slideHash, slide);

        // 瞄准时上半身偏移, 目前服务器没有下发瞄准角度, 先归零(正前方)
        if (v.HasAimX) anim.SetFloat(_aimXHash, 0f);
        if (v.HasAimY) anim.SetFloat(_aimYHash, 0f);

        // 翻滚是 Trigger: 只在 animId 刚变成 6 的那一帧触发
        if (p.animId == 6 && v.LastAnimId != 6 && v.HasDodge)
            anim.SetTrigger(_dodgeHash);

        v.LastAnimId = p.animId;

        v.Age += Time.deltaTime;

        // 创建后前 10 秒自动打印, 方便定位"动画不播"的问题
        if (debugAnimator || v.Age < 10f)
        {
            v.LogTimer += Time.deltaTime;
            if (v.LogTimer >= 1f)
            {
                v.LogTimer = 0f;

                var clipInfos = anim.GetCurrentAnimatorClipInfo(0);
                string clip = clipInfos.Length > 0 && clipInfos[0].clip != null
                    ? clipInfos[0].clip.name
                    : "none";

                var state = anim.GetCurrentAnimatorStateInfo(0);

                Debug.Log($"[RemoteAnim] id={p.id} age={v.Age:F1} animId={p.animId} blend={v.MoveBlend:F2} " +
                          $"clip={clip} norm={state.normalizedTime:F2} " +
                          $"inTransition={anim.IsInTransition(0)} enabled={anim.enabled} speed={anim.speed:F2}");
            }
        }
    }

    private static bool HasParam(Animator anim, int hash)
    {
        var ps = anim.parameters;
        for (int i = 0; i < ps.Length; i++)
            if (ps[i].nameHash == hash) return true;
        return false;
    }

    private void HandleJoinLeave(int playerId, bool joined)
    {
        if (!joined) RemoveView(playerId);
    }

    private void HandleRoomChanged()
    {
        ClearAll();
    }

    private void RemoveView(int id)
    {
        if (_views.TryGetValue(id, out var v))
        {
            if (v.Root != null) Destroy(v.Root.gameObject);
            _views.Remove(id);
        }
    }

    private void ClearAll()
    {
        foreach (var kv in _views)
            if (kv.Value.Root != null) Destroy(kv.Value.Root.gameObject);
        _views.Clear();
    }
}

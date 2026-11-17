using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>拾取 / 下包 / 拆包 / 丢弃</summary>
public class SpikeInteraction : MonoBehaviour
{
    private const string BuiltInBeaconPath = "Prefabs/Network/Spike/SpikeBeacon";

    [Header("包点")]
    public Transform plantSite;
    public float plantRadius = 3f;
    public float defuseRadius = 3f;
    public float pickupRadius = 1.5f;

    [Header("包的实物")]
    public GameObject spikePrefab;

    [Header("提示 UI")]
    public TMP_Text promptText;
    public Image progressFill;

    public KeyCode key = KeyCode.E;
    public KeyCode dropKey = KeyCode.G;
    public float sendInterval = 0.1f;

    private SpikeStateDto _spike;
    private GameObject _spikeObj;
    private float _sendTimer;
    private float _localPlantMs;
    private float _localDefuseMs;
    private bool _subscribed;
    private GameObject _interactRoot;
    private GameObject _builtInBeaconPrefab;
    private SpikeActionAnimation _actionAnimation;
    private SpikeBeaconVisual _spikeVisual;
    private Collider _spikeContactCollider;

    private void OnEnable() { TrySubscribe(); }
    private void Start() { TrySubscribe(); }

    private void TrySubscribe()
    {
        if (_subscribed || BattleClient.Instance == null) return;
        BattleClient.Instance.OnSpikeState += dto => _spike = dto;
        _subscribed = true;
    }

    private void OnDisable()
    {
        _actionAnimation?.End();
        if (_spikeObj != null) Destroy(_spikeObj);
        _spikeObj = null;
    }

    private void Update()
    {
        TrySubscribe();
        _sendTimer += Time.deltaTime;

        var bc = BattleClient.Instance;
        var pc = PlayerController.INSTANCE;
        var round = bc != null ? bc.LastRound : null;

        if (bc == null || pc == null || pc.currentPlayerModel == null)
        {
            SetPrompt(null, 0f);
            SetActionAnimation(false, pc);
            return;
        }

        bool combat = round != null && round.phase == 2;
        Vector3 me = pc.currentPlayerModel.transform.position;
        Vector3 spikePos = _spike != null
            ? new Vector3(_spike.x, _spike.y, _spike.z)
            : Vector3.zero;

        int state = _spike != null ? _spike.spikeState : -1;
        bool carriedByMe = state == 1 && _spike != null && _spike.carrierId == bc.MyPlayerId;

        // 地上未拾取的包和已经下包的包都需要保留世界模型。
        bool planted = state == 2;
        bool showSpike = combat && (state == 0 || planted);
        UpdateSpikeObject(showSpike ? spikePos : Vector3.zero, showSpike, planted);

        string prompt = null;
        float progress = 0f;
        bool actionActive = false;

        // G 丢弃
        if (combat && bc.MyTeam == 0 && Input.GetKeyDown(dropKey))
            bc.SendDropSpike();

        // 拾取
        if (combat && bc.MyTeam == 0 && state == 0
            && Vector3.Distance(me, spikePos) <= pickupRadius)
        {
            prompt = "按 E 拾取包";
            if (Input.GetKeyDown(key)) bc.SendPickupSpike();
        }

        // 下包(必须自己携带)
        else if (combat && bc.MyTeam == 0 && carriedByMe && plantSite != null
                 && Vector3.Distance(me, plantSite.position) <= plantRadius)
        {
            bool holding = Input.GetKey(key);
            actionActive = holding;
            _localPlantMs = holding ? Mathf.Min(3000f, _localPlantMs + Time.deltaTime * 1000f) : 0f;
            float server = _spike.plantProgressMs / 3000f;

            prompt = "按住 E 下包";
            progress = Mathf.Max(server, _localPlantMs / 3000f);

            if (holding && _sendTimer >= sendInterval)
            {
                _sendTimer = 0f;
                bc.SendPlantSpike();
            }
        }

        // 拆包: 玩家碰撞体必须实际接触爆能器碰撞体。
        else if (combat && bc.MyTeam == 1 && state == 2)
        {
            bool touching = IsTouchingSpike(pc.currentPlayerModel);
            if (touching)
            {
                bool holding = Input.GetKey(key);
                actionActive = holding;
                _localDefuseMs = holding
                    ? Mathf.Min(6000f, _localDefuseMs + Time.deltaTime * 1000f)
                    : 0f;
                float server = _spike.defuseProgressMs / 6000f;

                prompt = $"按住 E 拆包  {_spike.fuseLeftMs / 1000f:F0}s";
                progress = Mathf.Max(server, _localDefuseMs / 6000f);

                if (holding && _sendTimer >= sendInterval)
                {
                    _sendTimer = 0f;
                    bc.SendDefuseSpike();
                }
            }
            else
            {
                _localDefuseMs = 0f;
                if (Vector3.Distance(me, spikePos) <= defuseRadius)
                    prompt = "靠近并接触爆能器后才能拆包";
            }
        }

        if (state != 0) _localPlantMs = 0f;
        if (state != 2) _localDefuseMs = 0f;

        SetPrompt(prompt, progress);
        SetActionAnimation(actionActive, pc);
    }

    private void UpdateSpikeObject(Vector3 pos, bool visible, bool planted)
    {
        if (_builtInBeaconPrefab == null)
            _builtInBeaconPrefab = Resources.Load<GameObject>(BuiltInBeaconPath);

        GameObject prefab = _builtInBeaconPrefab != null ? _builtInBeaconPrefab : spikePrefab;
        if (prefab == null)
        {
            if (_spikeObj != null) _spikeObj.SetActive(visible);
            return;
        }

        if (_spikeObj == null)
        {
            _spikeObj = Instantiate(prefab);
            _spikeObj.name = "SpikeBeacon";
            _spikeVisual = _spikeObj.GetComponent<SpikeBeaconVisual>();
            if (_spikeVisual == null)
                _spikeVisual = _spikeObj.AddComponent<SpikeBeaconVisual>();
            _spikeContactCollider = _spikeVisual.ContactCollider;
        }

        _spikeObj.SetActive(visible);
        if (visible) _spikeObj.transform.position = pos + Vector3.up * 0.025f;
        _spikeVisual?.SetPlanted(planted);
    }

    private bool IsTouchingSpike(PlayerModel player)
    {
        if (_spikeObj == null || !_spikeObj.activeInHierarchy
            || player == null || player.cc == null)
            return false;

        if (_spikeContactCollider == null && _spikeVisual != null)
            _spikeContactCollider = _spikeVisual.ContactCollider;
        if (_spikeContactCollider == null)
            return false;

        Bounds playerBounds = player.cc.bounds;
        Bounds spikeBounds = _spikeContactCollider.bounds;
        Vector3 pointOnSpike = spikeBounds.ClosestPoint(playerBounds.center);
        Vector3 pointOnPlayer = playerBounds.ClosestPoint(pointOnSpike);
        return (pointOnPlayer - pointOnSpike).sqrMagnitude <= 0.025f * 0.025f;
    }

    private void SetActionAnimation(bool active, PlayerController controller)
    {
        if (active)
        {
            if (_actionAnimation == null)
                _actionAnimation = gameObject.AddComponent<SpikeActionAnimation>();
            _actionAnimation.Begin(controller);
        }
        else
        {
            _actionAnimation?.End();
        }
    }

    private void SetPrompt(string prompt, float progress)
    {
        ResolveInteractRoot();

        // InteractArea 默认是隐藏的, 由这里统一开关
        if (_interactRoot != null)
            _interactRoot.SetActive(prompt != null);

        if (promptText != null)
        {
            promptText.enabled = true;
            promptText.text = prompt ?? "";
        }

        if (progressFill != null)
        {
            progressFill.enabled = true;
            progressFill.fillAmount = Mathf.Clamp01(progress);
        }
    }

    /// <summary>自动向上找到名为 InteractArea 的父物体</summary>
    private void ResolveInteractRoot()
    {
        if (_interactRoot != null) return;

        Transform t = promptText != null ? promptText.transform
            : (progressFill != null ? progressFill.transform : null);
        if (t == null) return;

        Transform p = t;
        while (p != null)
        {
            if (p.name == "InteractArea") { _interactRoot = p.gameObject; return; }
            p = p.parent;
        }

        _interactRoot = t.parent != null ? t.parent.gameObject : null;
    }
}
